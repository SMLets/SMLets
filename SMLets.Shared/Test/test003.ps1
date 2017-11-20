BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $STARTTIME = [datetime]::Now
}
END
{
try
{
    $a = get-scsmRule
    foreach($rule in $a)
    {
        $b = [smlets.workflowhelper]::GetJobStatus($rule)
        # once we have a winner, don't continue
        if ( $b ) 
        { 
            if ( $b[0] -ne $null )
            {
                Out-TestLog ("PASS:" + [datetime]::now + ":${TESTNAME}")
                return 0
            }
        }
    }
}
catch
{
    Out-TestLog ("FAIL:" + [datetime]::now + ":${TESTNAME}")
    Out-TestLog ("   DETAILS: " + $error[0])
    return 1
}
}
