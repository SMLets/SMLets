
[CmdletBinding()]
param (
    [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)] $DeletedObject,
    [Parameter()][Switch]$PassThru,
    [Parameter()][Switch]$whatif
    )
Process
{
    $DeletedObject | set-scsmobject -ph @{ ObjectStatus = "Active" } -whatif:$whatif -passthru:$passthru
}
