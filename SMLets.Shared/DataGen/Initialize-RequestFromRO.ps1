param ( $count = 2, [switch]$whatif, [switch]$verbose, $requestOfferingName = "*" )

# in this case, it is count per RequestOffering

Write-Progress -Status Retrieving -Activity "Request Offerings"
$ros = get-scsmrequestoffering -published -displayname $requestOfferingName -verbose:$verbose
$random = new-object system.random
$total = @($ros).count * $count
$current = 0
foreach ( $myRO in $ros )
{
    for($i = 0; $i -lt $count; $i++)
    {
        Write-Progress -Activity $myRo.Title -Status "Creating $i of $count" -perc ($current/$total * 100)
        $current++
        $qs = $myRO| get-scsmrequestofferingquestion
        foreach ( $q in $qs  )
        {
            switch ( $q.SourceControlType )
            {
                "string" 
                {
                    $q.answer = get-lorem ($random.Next(5,15))
                }
                "list"
                {
                    $enumlist = get-scsmchildenumeration -en ( get-scsmenumeration -id $q.ListId)
                    $q.Answer = $enumlist[$random.Next(0,$enumlist.count)]
                }
                "double"
                {
                    $q.Answer = $random.Next(60,100)/17
                }
                "Integer"
                {
                    $q.Answer = $random.Next(1,100);
                }
                "DateTime"
                {
                    $q.Answer = [datetime]::Now.AddDays($random.next(0,30)).AddHours($random.Next(0,24)).AddMinutes($random.next(0,60))
                }
                "Boolean"
                {
                    $q.Answer = [bool]$random.next(0,2)
                }
                "InlineList" 
                { 
                    $allowed = $q.InlineListElements
                    $q.Answer = $allowed[$random.next(0,$allowed.count)]
                }
                "FileAttachment"
                {
                    $tfile =  [io.path]::GetTempFileName().split("\\")[-1]
                    "Temporary data" > $tfile
                    $q.Answer = $tfile
                }
                default 
                {
                    # write-host $q.SourceControlType
                }
            }
        }
        New-SCSMServiceRequest $myRO -Question $qs -verbose:$verbose -whatif:$whatif
        if ( $tfile -and (test-path $tfile) ) { remove-item $tfile }
    }
}
