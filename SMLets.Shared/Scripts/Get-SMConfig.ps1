# a script to retrieve information about a running SM installation
# we need to collect the following:
#    The Setup logs:
#        $env:homedrive\$env:homepath\appdata\local\scsm\setup\*.log
# the Enumeration of SM administrators role and members
# the runas profiles and members
# service config info for healthservice, omcfg,omsdk
# eventlog from Operations Manager
# SETUP:
# go get the location of the SDK binaries and load them
$ProgMsg = "Capturing SM Configuration Information" 
Write-Progress ${ProgMsg} "Reading Install Dir" -perc 10
$OMREGPATH = "HKLM:\software\microsoft\Microsoft Operations Manager\3.0\Setup"
$InstallDir = (Get-ItemProperty -path $OMREGPATH).InstallDirectory 
$SMDIR = "${InstallDir}/SDK Binaries"
# load the binaries
Write-Progress ${ProgMsg} "Loading SDK Assemblies" -perc 20
get-childitem $SMDIR | %{ [reflection.assembly]::LoadFile($_.fullname) } | out-null

# First, we'll create a directory for our stuff
Write-Progress ${ProgMsg} "Creating TempDir" -perc 30
${TDIR} = "${env:temp}\SCSMPS.${PID}"
if ( test-path ${TDIR} )
{ 
    remove-item -rec ${TDIR} 
}
new-item ${TDIR} -type directory |out-null
# copy log files, if present
Write-Progress ${ProgMsg} "Copying Installation Logs" -perc 40
if ( $logdir )
{
    $logdir = ls c:\users\*\appdata\local\scsm\logs\* -force
    if ( ! (test-path ${TDIR})) { throw "Could not create ${TDIR}" }
    $logdir | %{ copy-item $_ ${TDIR} }
}
# get SM Administrators
# setup an EMG
Write-Progress ${ProgMsg} "Getting UserRole Information" -perc 50
$EMG = new-object microsoft.enterprisemanagement.enterprisemanagementgroup localhost
$EMG.Security.GetUserRoles()|
    ?{$_.name -eq "ServiceManagerAdministrators"}|
    export-clixml "${TDIR}/SMAdmins.clixml"

# get runas profiles
Write-Progress ${ProgMsg} "Getting RunAsProfile Information" -perc 70
# go get the accounts that we can find
$ServiceReferences = $EMG.Security.GetSecureDataHealthServiceReferences()
$SecureReferences = $ServiceReferences|%{
        $emg.Security.GetSecureDataHealthServiceReferenceBySecureReferenceId($_.MonitoringSecureReferenceID)
        }
$SecureReferences|
    %{ $eMG.Security.GetSecureData($_.MonitoringSecureDataId) }|
    export-clixml "${TDIR}/Profiles.clixml"
remove-item variable:SecureReferences -ea silentlycontinue
remove-item variable:ServiceReferences -ea silentlycontinue
remove-item variable:MG -ea silentlycontinue
remove-item variable:EMG -ea silentlycontinue
# get services
Write-Progress ${ProgMsg} "Getting Service Information" -perc 70
$services = "healthservice","omcfg","omsdk"
get-wmiobject win32_service|
    where-object { $services -contains $_.name }|
    export-clixml "${TDIR}\Services.clixml"
Write-Progress ${ProgMsg} "Getting OpsMgr Eventlog Information" -perc 80
get-eventlog "Operations Manager" -newest 100 | 
    export-clixml "${TDIR}\OMEventlog.clixml"
Write-Progress ${ProgMsg} "All Done" -perc 90
"Files are in ${TDIR}"
