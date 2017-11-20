
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $name = "Microsoft.windows.computer"
    $STARTTIME = [datetime]::Now
}
END
{

    try
    {
        $o = get-scsmannouncement
        if ( $null -ne $o ) { Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}" ) }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    return 0

}
