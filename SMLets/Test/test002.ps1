
# this makes sure that all the cmdlets have a ComputerName and Credential
# parameter except for the ones that shouldn't

# this test ensures that the cmdlets that should be here are here
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $STARTTIME = [datetime]::Now
    $ExpectedList = "Export-SCManagementPack",
        "New-SCSealedManagementPack",
        "Get-SCSMSession",
        "Remove-SCSMSession"
    $emg = new-mg
    if ( $emg.Version -gt (new-object Version 7.5.0.0))
    {
        $ExpectedList += "New-SCSMServiceRequest",
            "Get-SCSMRequestOfferingQuestion",
            "Add-SCSMRequestOffering" 
        }
}
END
{
    $DiscoveredList = get-command -module smlets -type cmdlet | 
    ?{ ! ($_.parameters['ComputerName'] -and $_.parameters['Credential']) }|
    %{$_.name}|sort


    $Results = Compare-Object $ExpectedList $DiscoveredList -sync 100
    if ( $results )
    {
        Out-TestLog ("FAIL:" + [datetime]::Now + ":${TESTNAME}:Required parameter (Computer|Credential)(Computer|Credential) missing")
        $results | %{ Out-TestLog ("  DETAIL:" + [datetime]::Now + ":$TESTNAME:$_") }
        return 1
    }
    Out-TestLog ("PASS: " + [datetime]::Now + ":$TESTNAME")
    return 0
}
