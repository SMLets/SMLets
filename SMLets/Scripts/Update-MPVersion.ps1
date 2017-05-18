[CmdletBinding(SupportsShouldProcess=$true)]
param ( 
    [Parameter(Position=0,Mandatory=$true)]$ManagementPackFile ,
    [Parameter()][Switch]$MajorIncrement,
    [Parameter()][Switch]$MinorIncrement,
    [Parameter()][Switch]$BuildIncrement,
    [Parameter()][Switch]$RevisionIncrement,
    [Parameter()][Switch]$PassThru
    )
$File = get-childitem $ManagementPackFile -ea SilentlyContinue
if ( ! $File -and ($File.Extension -ne ".xml"))
{
    throw "$ManagementPackFile not found or is not a .xml"
}
$xml = [xml](get-content $File)
[version]$Version = $xml.ManagementPack.Manifest.Identity.Version
if ( ! ($MajorIncrement -or $MinorIncrement -or $BuildIncrement -or $RevisionIncrement) )
{
    $Version = "{0}.{1}.{2}.{3}" -f $version.Major,$version.Minor,($version.Build+1),$version.Revision
}
if ( $MajorIncrement )
{
    $Version = "{0}.{1}.{2}.{3}" -f ($version.Major+1),$version.Minor,$version.Build,$version.Revision
}
if ( $MinorIncrement )
{
    $Version = "{0}.{1}.{2}.{3}" -f $version.Major,($version.Minor+1),$version.Build,$version.Revision
}
if ( $BuildIncrement )
{
    $Version = "{0}.{1}.{2}.{3}" -f $version.Major,$version.Minor,($version.Build+1),$version.Revision
}
if ( $RevisionIncrement )
{
    $Version = "{0}.{1}.{2}.{3}" -f $version.Major,$version.Minor,$version.Build,($version.Revision+1)
}
Write-Verbose "Setting Version to $Version"
if ( $PSCmdlet.ShouldProcess($File.fullname))
{
    $xml.ManagementPack.Manifest.Identity.Version = $Version.ToString()
    $xml.Save($File.FullName)
    if ( $PassThru )
    {
        get-content $file
    }
}

