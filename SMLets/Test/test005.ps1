# this test ensures that the cmdlets that should be here are here
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $DOCUMENT = "${TESTDIR}\Document.RTF"
    $class = Get-SCSMClass -Name "System.Knowledge.Article"

    # get rid of the article we're creating
    get-scsmobject -class $class -filter "DisplayName = 'TestArticle1'"|
        remove-scsmobject -force
    $STARTTIME = [datetime]::Now
}
END
{


    try
    {
        $OPEN = ([io.filemode]::Open)
        $RACC = ([io.fileaccess]::Read)
        $RSHR = ([io.fileshare]::read)
        $str = new-object io.filestream ${DOCUMENT},$OPEN,$RACC,$RSHR
        new-scsmobject -class $class -PropertyHashtable @{ 
            ArticleID = "TestArticle1"
            Title = "test art1"
            Status = "Draft" 
            EndUserContent = $str
            }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":$TESTNAME" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
    }
    finally
    {
        $str.close()
        $str.dispose()
    }


    if (get-scsmobject -class $class -filter "DisplayName = 'TestArticle1'")
    {
        # cleanup
        Out-TestLog ("PASS: " + [datetime]::Now + ":$TESTNAME")
        get-scsmobject -class $class -filter "DisplayName = 'TestArticle1'"| remove-scsmobject -force
        return 0
    }
    else
    {
        return 1
    }

}
