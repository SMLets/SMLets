param ([string]$scsmserver = "localhost", [switch]$verbose )
if ( ! (get-command candle.exe -ea silentlycontinue))
{
    $env:path += ';c:\Program Files (x86)\WiX Toolset v3.10\bin\'
}

$outputPrefix = "SMLets"

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
	Write-Host "new MSE Created: $MSINAME"
}

Write-Progress -Activity "Building MSI" -status "Creating '$outputPrefix' x64 version"
$env:SMLETS64=1
new-smletsmsi -name $outputPrefix -arch x64

Write-Progress -Activity "Building MSI" -status "Creating '$outputPrefix' x86 version"
$env:SMLETS64=0
new-smletsmsi -name $outputPrefix -arch x86

if ( test-path env:SMLETS64 ) { rm env:SMLETS64 }



