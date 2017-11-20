$LOGFILE = "TestLog.txt"
$TESTDIR = (split-path $myInvocation.mycommand.definition)
$OUTLOG  = "$TESTDIR\$LOGFILE"
function Out-TestLog
{
    param ( [string]$message )
    $message | out-file -append $OUTLOG
}
