param ([string]$scsmserver = "localhost", [switch]$verbose )
if ( ! (get-command candle.exe -ea silentlycontinue))
{
    $env:path += ';c:\Program Files (x86)\WiX Toolset v3.10\bin\'
}
# we need to load the module assembly (but not the module) so we can determine
# whether we're bulding for SP1 or 2012
# we've got to load Microsoft.EnterpriseManagement.Core so we
# can build an EnterpriseManagementGroup
[reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")| out-null
$emg = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $scsmserver
[byte[]]$bytes = Get-Content ../SMLets.Module.dll -encoding byte -readcount 0
$asm = [reflection.assembly]::Load($bytes)
if ( (new-object smlets.smletsversioninfo $emg).TargetProduct -match "SP1" )
{
    $outputPrefix = "SMLets.SP1"
}
else
{
    $outputPrefix = "SMLets"
}

function new-smletsmsi
{
    param ( $NAME, $ARCH )
    if ( $ARCH -eq "x86" )
    {
        $MSINAME = "${NAME}.X86.msi"
        $WIXNAME = "${NAME}.X86.wixobj"
        $PDBNAME = "${NAME}.X86.wixpdb"
    }
    else
    {
        $MSINAME = "${NAME}.msi"
        $WIXNAME = "${NAME}.wixobj"
        $PDBNAME = "${NAME}.wixpdb"
    }

    if ( test-path $MSINAME ) { rm $MSINAME }
    # the WSX file doesn't change
    $c = candle -arch $ARCH smlets.wsx -out $WIXNAME
    if ( $verbose ) { $c | write-verbose -verbose }
    $l = light -ext WixUIExtension $WIXNAME -out $MSINAME
    if ( $verbose ) { $l | write-verbose -verbose }
    # cleanup
    if ( test-path fogfile.txt ) { rm fogfile.txt }
    if ( test-path $WIXNAME ) { rm $WIXNAME }
    if ( test-path $PDBNAME ) { rm $PDBNAME }
}

Write-Progress -Activity "Building MSI" -status "Creating '$outputPrefix' x64 version"
$env:SMLETS64=1
new-smletsmsi -name $outputPrefix -arch x64

Write-Progress -Activity "Building MSI" -status "Creating '$outputPrefix' x86 version"
$env:SMLETS64=0
new-smletsmsi -name $outputPrefix -arch x86

if ( test-path env:SMLETS64 ) { rm env:SMLETS64 }



