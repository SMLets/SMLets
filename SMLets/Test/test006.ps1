# this test ensures that the cmdlets that should be here are here
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
        $o = get-scsmclass $name$|get-scsmobject -ea stop -max 1
        if ( $null -ne $o ) { Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}a" ) }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}a" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $o = get-scsmtypeprojection $name|get-scsmobjectprojection -ea stop -max 1
        if ( $null -ne $o ) { Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}b" ) }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}b" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }
    return 0

}
