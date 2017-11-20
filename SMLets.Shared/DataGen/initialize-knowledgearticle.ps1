param ( $count = 20, [switch]$whatif, [switch]$verbose)
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $DOCUMENT = "${PWD}\Document.RTF"
    $class = get-scsmclass "System.Knowledge.Article"
    # get rid of the article we're creating
    get-scsmobject $class -filter "DisplayName = 'TestArticle1'"|
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
            $Title = Get-Lorem 6
            $script:str = new-object io.filestream "${DOCUMENT}",$OPEN,$RACC,$RSHR
            new-scsmobject -whatif:$whatif -verbose:$verbose -class $class -PropertyHashtable @{ 
                ArticleID = "TestArticle: ${G}"
                Title = $Title
                Status = "Draft" 
                EndUserContent = $str
                }
            Write-Progress -Act "Create KA" -Stat $Title -perc ($i/$count*100)
        }
        catch 
        {
            throw $error[0]
        }
        finally
        {
            $str.close()
            $str.dispose()
        }
    }

}
