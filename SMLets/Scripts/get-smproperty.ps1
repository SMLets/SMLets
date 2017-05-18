[CmdletBinding(DefaultParameterSetName="name")]
param ( 
    [Parameter(Position=0,ParameterSetName="name",Mandatory=$true)][string]$classname,
    [Parameter(Position=0,ParameterSetName="class",Mandatory=$true,ValueFromPipeline=$true)]
    [Microsoft.EnterpriseManagement.Configuration.ManagementPackClass]$class = $null,
    [Parameter()][string]$property = "*"
    )
PROCESS
{
if ( ! $class )
{
    $class = get-scsmclass $classname
    if ( $class -is [array] )
    {
        Write-Host -for RED "Too many classes, try again"
        $class | Write-host -for yellow
        exit
    }
    if ( ! $class )
    {
        Write-Host -for RED "$classname not found, try again"
        exit
    }
}
if ( $class.Abstract )
{
    $class.PropertyCollection|sort name -uniq
    $class.GetBaseTypes()|%{$_.propertycollection}|sort name -uniq
}
else
{
#$EMOT = "Microsoft.EnterpriseManagement.Common.CreatableEnterpriseManagementObject"
#$emo = new-object $EMOT $class.ManagementGroup,$class
# $emo.getproperties()|%{$_}|
$class.GetProperties("recursive")|add-member -pass NoteProperty Class $class | 
    where-object { $_.name -like "$property" } |
    sort name -uniq
}
}
