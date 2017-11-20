
param ( 
    [Parameter(Mandatory=$true,Position=0)][Microsoft.EnterpriseManagement.Configuration.ManagementPackProperty]$property,
    [Parameter(Mandatory=$true,Position=1)][string]$op,
    [Parameter(Mandatory=$true,Position=2)][string]$value,
    [switch]$stringOnly
    )
# Create the expression XML...something like:
#<Criteria xmlns="http://Microsoft.EnterpriseManagement.Core.Criteria/">
#  <Reference Id="ObjectInteractionClient" Version="1.0.0.0" Alias="Test" />
#  <Expression>
#    <SimpleExpression>
#      <ValueExpressionLeft>
#        <Property>$Target/Property[Type='Test!ObjectInteractionClient.Bleh']/BlehId$</Property>
#      </ValueExpressionLeft>
#      <Operator>Equal</Operator>
#      <ValueExpressionRight>
#        <Value>JakubTestIdCompositeTests</Value>
#      </ValueExpressionRight>
#    </SimpleExpression>
#  </Expression>
#</Criteria>            

function New-Criteria ( $stringBuilder, $MP, [switch]$UseNamespaces, $Namespace )
{
    $ref1 = "myMP"
    $references.Add($ref1, $MP)
    $criteriaWriter = new-object io.StringWriter $stringBuilder 
    $criteriaXmlWriter = new-object System.Xml.XmlTextWriter $criteriaWriter
    $criteriaXmlWriter.Namespaces = $true
    $criteriaXmlWriter.WriteStartElement("Criteria", $NameSpace)
    $criteriaXmlWriter.WriteStartElement("Reference", $NameSpace)
    $criteriaXmlWriter.WriteAttributeString("Id", $MP.Name)
    $criteriaXmlWriter.WriteAttributeString("Version", $MP.Version.ToString())
    $criteriaXmlWriter.WriteAttributeString("PublicKeyToken", $MP.KeyToken)
    $criteriaXmlWriter.WriteAttributeString("Alias", $ref1)
    $criteriaXmlWriter.WriteEndElement()
    $criteriaXmlWriter
}

function Add-Expression ( $XMLWRITER, $NS, $PP, $OP, $VALUE )
{
    write-host -for red $PP
    $XMLWRITER.WriteStartElement("Expression", $NS)
    $XMLWRITER.WriteStartElement("SimpleExpression", $NS)
    $XMLWRITER.WriteStartElement("ValueExpressionLeft", $NS)
    $XMLWRITER.WriteElementString("Property", $NS, $PP)
    $XMLWRITER.WriteEndElement() # ValueExpressionLeft
    $XMLWRITER.WriteElementString("Operator", $NS, $OP)
    $XMLWRITER.WriteStartElement("ValueExpressionRight", $NS)
    $XMLWRITER.WriteElementString("Value", $NS, $VALUE)
    $XMLWRITER.WriteEndElement() # ValueExpressionRight
    $XMLWRITER.WriteEndElement() # SimpleExpression
    $XMLWRITER.WriteEndElement() # Expression
}

$genericTypeName = "System.Collections.Generic.Dictionary``2"
$genericType = [type]$genericTypeName
[type[]] $typedParameters = "string","Microsoft.EnterpriseManagement.Configuration.ManagementPack"
$closedType = $genericType.MakeGenericType($typedParameters)
$global:references = [activator]::CreateInstance($closedType, $null)

$MPCLASS = [microsoft.enterprisemanagement.configuration.managementpackclass]
$SPE = "Microsoft.EnterpriseManagement.Common.SeedPathElement"
$PPE = "Microsoft.EnterpriseManagement.Common.PropertyPathElement"
$mpp = [Microsoft.EnterpriseManagement.Configuration.ManagementPackProperty]$p
$global:seed = new-object $SPE $mpp.parentelement
$seed.ChildElement = new-object $PPE $property


$global:criteriaBuilder = new-object text.stringbuilder
$ExpressionNamespace = "http://Microsoft.EnterpriseManagement.Core.Criteria/"
# This returns an XML writer with the header stuff already done
$propertyMP = $property.GetManagementPack()
$criteriaXmlWriter = New-Criteria -string $criteriaBuilder -mp $propertyMP -usen $true -namespace $ExpressionNamespace

$propertyPath = $seed.ToString($references) -replace "Context","Target"
Add-Expression $criteriaXmlWriter $ExpressionNamespace $PropertyPath $op $value

$criteriaXmlWriter.WriteEndElement() # Criteria
$criteriaXmlWriter.Flush()

$cString = $criteriaBuilder.ToString()
if ( $stringOnly ) { $cstring } 
else
{
$CRITERIATYPE = "Microsoft.EnterpriseManagement.Common.EnterpriseManagementObjectCriteria"
new-object $CRITERIATYPE $cString,$property.ParentElement,$property.ManagementGroup
}
