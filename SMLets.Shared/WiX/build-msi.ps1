param ([string]$version,[string]$targetDir, [switch]$verbose )
$ErrorActionPreference = "Stop"
if ( ! (get-command candle.exe -ea silentlycontinue))
{
	if(!(Test-Path 'c:\Program Files (x86)\WiX Toolset v3.10\bin\'))
	{
		throw New-Object Error("WiX Toolset required")
	}
    $env:path += ';c:\Program Files (x86)\WiX Toolset v3.10\bin\'
}

$outputPrefix = "SMLets.$version"
write-host $solutionDir

function new-smletsmsi
{
    param ( $NAME, $ARCH )
    $MSINAME = "${NAME}.msi"
    $WIXNAME = "${NAME}.wixobj"
    $PDBNAME = "${NAME}.wixpdb"

    if ( test-path $MSINAME ) { rm $MSINAME }
    $wsxPath = "SMLets.wsx"
    # the WSX file doesn't change
    $c = candle -arch x64 -out $WIXNAME "`"$wsxPath`"" 
    if ( $verbose ) { $c | write-verbose -verbose }
    $c = $c -join "`r`n"
    if($c.Contains("error")){
        throw $c
    }
    $l = light -ext WixUIExtension $WIXNAME -out $MSINAME
    if ( $verbose ) { $l | write-verbose -verbose }
    $l = $l -join "`r`n"
    if($l.Contains("error")){
        throw $l
    }
    # cleanup
    if ( test-path fogfile.txt ) { rm fogfile.txt }
    if ( test-path $WIXNAME ) { rm $WIXNAME }
    if ( test-path $PDBNAME ) { rm $PDBNAME }
}

function replace-psd1version
{ 
	$targetDir = $targetDir.Trim('`"')
	$psd1Path = Join-Path -Path $targetDir -ChildPath "SMLets.psd1"
	
	$psd1 = [string]::Join("`r`n", (Get-Content $psd1Path))

	$reg = [regex]::new("(\s+ModuleVersion\s+=\s+\')(\d+\.\d+.\d+.\d+)(\')", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

	$match = $reg.Match($psd1)
	if(!$match.Success -or $match.Groups.Count -ne 4)
	{
		throw "Inable to find version in psd1 file"
	}

	$versionFull = [version]::Parse($match.Groups[2])
	$versionFull = [version]::new($versionFull.Major, $versionFull.Minor, [int]::Parse($version), $versionFull.Revision)
	$psd1 = $reg.Replace($psd1, "`${1}$($versionFull)`${3}")
	$psd1 | Set-Content -Path $psd1Path

}

Write-Progress -Activity "Building MSI" -status "Creating '$outputPrefix' x64 version"
replace-psd1version
$env:SMLETS64=1
new-smletsmsi -name $outputPrefix

if ( test-path env:SMLETS64 ) { rm env:SMLETS64 }



