BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $STARTTIME = [datetime]::Now
    $TESTFAILED = $FALSE
}
END
{
    try
    {
        [array]$r = get-scsmtask
        if ( $null -ne $r[0] ) 
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}a" ) 
        }
        else
        {
            $TESTFAILED = $TRUE
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}a" ) 
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}a" ) 
        $TESTFAILED = $TRUE
    }

    try
    {
        [array]$r = get-scsmtaskresult
        if ( $null -ne $r[0] ) 
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}b" ) 
        }
        else
        {
            $TESTFAILED = $TRUE
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}b" ) 
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}b" ) 
        $TESTFAILED = $TRUE
    }

    if ( $TESTFAILED )
    {
        return 1
    }
    else
    {
        return 0
    }
}
