param (
    [Parameter(Position=0,Mandatory=$true)]
    [Microsoft.EnterpriseManagement.Configuration.ManagementPackClass]$class,
    [Parameter(Position=1,Mandatory=$true)][scriptblock]$sb,
    [Parameter()][switch]$xmlonly
    )

$mpName = $class.GetManagementPack().Name
$mpVers = $class.GetManagementPack().Version
$mpKey  = $class.GetManagementPack().KeyToken
$clName = $class.Name

if ( $mpKey ) { $TokenString = "PublicKeyToken='${mpKey}'" }
else { $TokenString = "" }

$mpId = $class.GetManagementPack().Id
$clId = $class.Id

[string]$CriteriaString = @"
<Criteria xmlns="http://Microsoft.EnterpriseManagement.Core.Criteria/">
  <Reference Id="${mpName}" Version="${mpVers}" ${TokenString} Alias="myMP" />
"@

[array]$conditions = (($sb.tostring() -replace " -and ","`r").split("`r"))
if ( $conditions.count -gt 1 )
{
    $CriteriaString += "`r`n<Expression><And>"
}
foreach ( $condition in $conditions )
{
    $left,$operator,$right = $condition.trim().split()
    $right = ([string]$right).trim().trim("""")
    $CEMO = new-object Microsoft.EnterpriseManagement.Common.CreatableEnterpriseManagementObject $class.ManagementGroup,$class
    $propertyCollection = $CEMO.GetProperties()|%{$_.name} 
    # tidy up
    # old way - not the right way because CEMOs have all the properties and classes miss base class info
    #$propertyCollection = $class.propertycollection|%{$_.name} 
    if ( $propertyCollection -notcontains "Id" )
    {
    $propertyCollection += "Id"
    }
    remove-item Variable:CEMO
    [string]$propertyName = $propertyCollection -eq $left
    $left = $propertyName
    if ( ! $propertyName )
    {
        throw "$left is not a property of $clName"
    }

    switch ( $operator )
    {
        '-eq' { $opName = "Equal"; break }
        '-like' { $opName = "Like"; break }
        '-notlike' { $opName = "NotLike"; break }
        '-isnull' { $opName = "IsNull"; break }
        '-isnotnull' { $opName = "IsNotNull"; break }
        default { throw "Only Equals supported"; break }
    }

    $leftValue = '$Target/Property[Type=''myMP!{0}'']/{1}$' -f ${clname},${left} 

    if ( $opName -match "Null" )
    {
    $EXPRESSION = @"
      <Expression>
        <UnaryExpression>
          <ValueExpression>
            <Property>${leftValue}</Property>
          </ValueExpression>
          <Operator>${opName}</Operator>
        </UnaryExpression>
      </Expression>
"@
    }
    else
    {
    $EXPRESSION = @"
  <Expression>
    <SimpleExpression>
      <ValueExpressionLeft>
        <Property>${leftValue}</Property>
      </ValueExpressionLeft>
      <Operator>$opName</Operator>
      <ValueExpressionRight>
        <Value>$right</Value>
      </ValueExpressionRight>
    </SimpleExpression>
  </Expression>
"@
}

  $CriteriaString += "`r`n${EXPRESSION}"
}
if ( $conditions.count -gt 1 )
{
    $CriteriaString += "`r`n</And></Expression>"
}
$CriteriaString += "`r`n</Criteria>"
$CRITERIATYPE = "Microsoft.EnterpriseManagement.Common.EnterpriseManagementObjectCriteria"
if ( $xmlonly )
{
$CriteriaString
}
else
{
new-object $CRITERIATYPE $CriteriaString,$class,$class.ManagementGroup
}
