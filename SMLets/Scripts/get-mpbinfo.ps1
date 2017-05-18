#requires -version 2.0
# required because of the way we use new-object
# Get-MPBInfo.ps1
# Retrieve management pack and resource information from a .MPB file
param ( $file, $computername = "localhost", [switch]$mpinfo, [switch]$dumpStream )
$PACKDLL = "Microsoft.EnterpriseManagement.Packaging"
$FSTYPE  = "Microsoft.EnterpriseManagement.Configuration.IO.ManagementPackFileStore"
$BUNDLET = "Microsoft.EnterpriseManagement.Packaging.ManagementPackBundleFactory"
$pkgasm = [reflection.assembly]::LoadWithPartialName($PACKDLL)
if ( ! $pkgasm ) { throw "Can't load packaging dll" }
# get a bundlefactory type
$BFACTORYT = $pkgasm.GetType($BUNDLET)
# create a bundle reader
# create a filestore, which is used by the bundlereader
# and then read the bundle
$br = $BFACTORYT::CreateBundleReader()

$files = resolve-path $file

foreach($mpfile in $files)
{
    $path = $mpfile.path
    if ( ! $path ) { throw "Could not find '$file'" }
    $fs = new-object $FSTYPE
    $mpb = $br.Read($path,$fs)
    $p = [io.path]::GetFileName($path)
    # for each managementpack, get the resources and create a custom object
    $mpb.ManagementPacks|%{
        $theMP = $_
        $ManagementPack = $_.name
        $Version = $_.Version
        $KeyToken = $_.KeyToken
        # keep track of whether the MP is sealed or no
        if ( $_.Sealed ) { $Sealed = "Sealed" } else { $Sealed = "Not Sealed" }
        if ( $mpb.GetStreams($_).count -gt 0 -and ! $mpinfo )
        {
            $mpb.GetStreams($_) |%{ 
                $hadStreams = $true
                $streams = $_
                # retrieve the keys and create a custom object which we'll use 
                # in formatting
                $streams.keys | %{
                    $ResourceName = $_
                    $Length = $streams.Item($ResourceName).Length
                    # this emits a custom object which can then be used with
                    # PowerShell formatting
                    # Get-MPBInfo <file>|ft -group ManagementPack Length,ResourceName
                    if ( $sealed -eq "sealed" ) { $s = $true } else { $s = $false }
                    new-object -type psobject -prop @{
                        MP             = $theMP
                        ManagementPack = "$ManagementPack"
                        Length         = $Length
                        Sealed         = $s
                        ResourceName   = $ResourceName
                        MPBFile        = $p
                        }
                    }
                }
        }
        else
        {
            if ( $sealed -eq "sealed" ) { $s = $true } else { $s = $false }
            new-object -type psobject -prop @{
                MP             = $_
                ManagementPack = "$ManagementPack"
                Sealed         = $s
                MPBFile        = $p
                }
        }
    }
    $fs.dispose()
}
