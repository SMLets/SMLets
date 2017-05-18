#requires -version 2.0

[CmdletBinding(SupportsShouldProcess=$true)]
param ( 
    [parameter(Position=0)][string]$computerName = "localhost"
    )

BEGIN
{
    $articles = get-scsmclass knowledge.ar|get-scsmobject
    [reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")|out-null
    $DEFAULT  = ([type]"Microsoft.EnterpriseManagement.Common.ObjectQueryOptions")::Default
    $EMG      = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup($computername)
    $SMP      = ([type]"Microsoft.EnterpriseManagement.Configuration.SystemManagementPack")::System
    $sl       = $emg.managementpacks.GetManagementPack($SMP)
    $KBL      = $emg.ManagementPacks.GetManagementPack("System.Knowledge.Library",$sl.keytoken,$sl.version)
    $kbclass  = $emg.EntityTypes.GetClass("System.Knowledge.Article", $KBL)
    $IMGMT    = $emg.EntityObjects.GetType()
    $EMOT     = [type]"Microsoft.EnterpriseManagement.Common.EnterpriseManagementObject"



    [type[]]$TYPES = [Microsoft.EnterpriseManagement.Configuration.ManagementPackClass],
                         [Microsoft.EnterpriseManagement.Common.ObjectQueryOptions]
    # Retrieve the method
    $ObjectReader = $IMGMT.GetMethod("GetObjectReader",$TYPES)
    # Create a generic method
    $GenericMethod = $ObjectReader.MakeGenericMethod($EMOT)
    # Invoke the method with our arguments
    [array]$arguments = $kbclass,$DEFAULT
    $articles = $GenericMethod.invoke($emg.EntityObjects,$arguments) 
    $propertyNames = "Title","Abstract","Keywords","ArticleId","CreatedBy","Name"


    $KBRTF = "KBARTICLE{0:000000}"
    $ArticleCount = 1
    foreach($article in $articles )
    {
        $pso = new-object PSObject $article
        $article.Values | %{ $pso | add-member NoteProperty $_.Type $_.Value -force }
        $pso.psobject.TypeNames[0] = "EnterpriseManagementObject#System.Knowledge.Article"
        # code to retrieve the property values that we actually want
        $Properties = $article.values | 
            ?{$properties -contains $_.type }|
            %{ $o = new-object psobject } {$o|add-member NoteProperty $_.type.name $_.value } { $o }
            
        $title = $properties.Title
        if ( $PSCmdlet.ShouldProcess($title) )
        {
            $KBStream = $article.values|?{$_.type.name -eq "EndUserContent"}
            if ( $KBStream.Value )
            {
                $filename = "$PWD\$KBRTF.RTF" -f $ArticleCount
                $output = new-object io.filestream "$filename","create"
                while ( ($b = $KBStream.Value.ReadByte()) -ne -1 )
                {
                    $output.WriteByte($b)
                }
                $output.close()
                $output.dispose()
                $pso | add-member NoteProperty RtfFile $filename 
            }
        }
        $pso
        $ArticleCount++
    }
}


<#
.SYNOPSIS
    Export a Knowledge Article
.DESCRIPTION
    The cmdlet retrieves all knowledge articles currently available 
    and creates the RTF file representing the End User Content and
    emits objects that represent the metadata for the knowledge
    article. This output should be used to create a CSV file which
    can then be used by Import-KnowledgeArticle
.PARAMETER ComputerName
    The computer name of the Service Manager 2010 server
.EXAMPLE
Export-KnowledgeArticle | Export-CSV kbarticles.csv

.INPUTS
    None
.OUTPUTS
    A custom object which contains the following:
    Title     - The title of the article
    Abstract  - The abstract of the article
    Keywords  - The keywords for the article
    ArticleId - The keywords for the article
    CreatedBy  - The keywords for the article
    Name  - The keywords for the article
    RtfFile  - The keywords for the article

        Verified - A boolean indicating whether the MP was verified
        Name     - The name of the management pack
        FullName - The path to the management pack
        Error    - Errors produced while verifying the management pack
.LINK
    Export-ManagementPack
    Get-ManagementPack
    Import-ManagementPack
    New-ManagementPack
    New-SealedManagementPack
    Remove-ManagementPack

#>
