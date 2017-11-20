param ( $count = 1)
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $DOCUMENT = "${PWD}\Document.RTF"
    $class = get-scsmclass "System.Knowledge.Article"
    # get rid of the article we're creating
    get-scsmobject -class $class -filter "DisplayName = 'TestArticle1'"|
        remove-scsmobject -force
    $STARTTIME = [datetime]::Now
    $OPEN = ([io.filemode]::Open)
    $RACC = ([io.fileaccess]::Read)
    $RSHR = ([io.fileshare]::read)
}
END
{


    for($i = 0; $i -lt $count; $i++)
    {
        try
        {
            $G = [guid]::NewGuid()
            $script:str = new-object io.filestream "${DOCUMENT}",$OPEN,$RACC,$RSHR
            new-scsmobject -class $class -PropertyHashtable @{ 
                ArticleID = "TestArticle: ${G}"
                Title = "KB ${G}"
                Status = "Draft" 
                EndUserContent = $str
                }
        }
        catch 
        {
            Write-Host -for red $error
        }
        finally
        {
            $str.close()
            $str.dispose()
        }
    }

}
