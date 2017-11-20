param ( $count = 10 , [switch]$whatif, [switch]$Verbose, [switch]$debug)

BEGIN
{

    function Get-Priority
    {
        param ( [string]$myurgency, [string]$myimpact )
        $hunt = "${myurgency}${myimpact}"
        $PriorityData = (get-scsmobject -class (get-scsmclass -name System.WorkItem.Incident.GeneralSetting)).PriorityMatrix
        if ( $PriorityData )
        {
            $matrix = [xml]$PriorityData
        }
        $Impact  = Get-SCSMEnumeration System.WorkItem.TroubleTicket.ImpactEnum.|sort-object Ordinal
        $Urgency = Get-SCSMEnumeration System.WorkItem.TroubleTicket.UrgencyEnum.|sort-object Ordinal
        $hash = @{}
        $count=1
        foreach($U in $Urgency)
        {
            foreach($I in $Impact)
            {
                $UN = $U.DisplayName; $IN = $I.DisplayName
                $UID = $U.ID; $IID = $I.ID
                $xpath = "Matrix/U[@Id='$UID']/I[@Id='$IID']/P"
                if ( $ProrityData )
                {
                    $value = $Matrix.SelectSingleNode($xpath)."#text"
                }
                else
                {
                    $Value = $count++
                }
                $hash["${UN}${IN}"] = $value
            }
        }
        $hash[$hunt]
    }

    function new-FileAttachmentStream
    {
        param ( $count = 3 )
        $OPEN = ([io.filemode]::Open)
        $RACC = ([io.fileaccess]::Read)
        $RSHR = ([io.fileshare]::read)
        for($i = 0; $i -lt $count; $i++)
        {
            $DOCUMENT = [io.path]::GetTempFileName()
            get-loremipsum 60 > $DOCUMENT
            new-object io.filestream $DOCUMENT,$OPEN,$RACC,$RSHR
        }
    }

    function get-loremipsum
    {
        param ( [int]$count = 11 )
        #$s = $Start.ToString().ToLower()
        #$URL =  "http://www.lipsum.com/feed/xml?amount=${count}&what=words&start=${s}"
        #[xml]$x = ((new-object net.webclient).downloadstring($URL))
        #$x.feed.lipsum

        $words = "consetetur","sadipscing","elitr","sed","diam","nonumy","eirmod","tempor","invidunt","ut","labore","et","dolore","magna","aliquyam","erat","sed","diam","voluptua","at","vero","eos","et","accusam","et",
            "justo","duo","dolores","et","ea","rebum","stet","clita","kasd","gubergren","no","sea","takimata","sanctus", "est","lorem","ipsum","dolor","sit","amet","lorem","ipsum","dolor","sit","amet","consetetur","sadipscing",
            "elitr","sed","diam","nonumy","eirmod","tempor","invidunt","ut","labore","et","dolore","magna","aliquyam", "erat","sed","diam","voluptua","at","vero","eos","et","accusam","et","justo","duo","dolores","et","ea",
            "rebum","stet","clita","kasd","gubergren","no","sea","takimata","sanctus","est","lorem","ipsum","dolor", "sit","amet","lorem","ipsum","dolor","sit","amet","consetetur","sadipscing","elitr","sed","diam","nonumy",
            "eirmod","tempor","invidunt","ut","labore","et","dolore","magna","aliquyam","erat","sed","diam","voluptua", "at","vero","eos","et","accusam","et","justo","duo","dolores","et","ea","rebum","stet","clita","kasd",
            "gubergren","no","sea","takimata","sanctus","est","lorem","ipsum","dolor","sit","amet","duis","autem","vel", "eum","iriure","dolor","in","hendrerit","in","vulputate","velit","esse","molestie","consequat","vel","illum",
            "dolore","eu","feugiat","nulla","facilisis","at","vero","eros","et","accumsan","et","iusto","odio", "dignissim","qui","blandit","praesent","luptatum","zzril","delenit","augue","duis","dolore","te","feugait",
            "nulla","facilisi","lorem","ipsum","dolor","sit","amet","consectetuer","adipiscing","elit","sed","diam", "nonummy","nibh","euismod","tincidunt","ut","laoreet","dolore","magna","aliquam","erat","volutpat","ut",
            "wisi","enim","ad","minim","veniam","quis","nostrud","exerci","tation","ullamcorper","suscipit","lobortis", "nisl","ut","aliquip","ex","ea","commodo","consequat","duis","autem","vel","eum","iriure","dolor","in",
            "hendrerit","in","vulputate","velit","esse","molestie","consequat","vel","illum","dolore","eu","feugiat", "nulla","facilisis","at","vero","eros","et","accumsan","et" 
         
        $RANDOM = new RANDOM
        [byte[]]$b = new byte[] $count
        $RANDOM.NextBytes($b)
        ($words[$b] -join " ").Trim() + "."
    }

    function Get-RandomItemFromList
    {
        param ( [Parameter(Mandatory=$true,Position=0)]$list )
        $list[$RANDOM.Next(0,$list.Count)]
    }

    function Get-RandomListFromList
    {
        param ( 
            [Parameter(Mandatory=$true,Position=0)]$list, 
            [Parameter(Mandatory=$true,Position=1)][int]$count
            )
        $mylist = [Collections.ArrayList]$list
        $RandomList =  @()
        for($i = 0; $i -lt $count -and $mylist.Count -gt 0; $i++)
        {
            $r = $RANDOM.Next(0,$mylist.Count)
            $RandomList += $mylist[$r]
            $mylist.RemoveAt($r)
        }
        $RandomList
    }

    $RANDOM = new-object System.Random
    $PTYPE = "System.WorkItem.Problem.ProjectionType"
    ###
    ### SETUP 
    ### Retrieve stuff from the CMDB which will be used later
    ###
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Users"
    $Users = Get-scsmobject -class (get-scsmclass -name Microsoft.AD.User$) -MaxCount 60
    if ( $Users.Count -lt 20 ) {
        Write-Error "Not enough users, go make some more"
        exit
        }
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Config Items"
    $CIList = get-scsmobject -class (get-scsmclass -name System.ConfigItem$) -MaxCount 60
    if ( $CIList.Count -lt 20 ) {
        Write-Error "Not enough CIs, go make some more"
        exit
        }

    Write-Progress -Activity "Setting Up Environment" -Status "Getting Enumerations"
    # ENUMERATIONS
    # the "." at the end of the enumeration is required to be sure we get the list
    $StatusList         = Get-SCSMEnumeration ProblemStatusEnum.
    $SourceList         = get-scsmenumeration ProblemSourceEnum.
    $ResolutionList     = Get-SCSMEnumeration ProblemResolutionEnum.
    $ClassificationList = Get-SCSMEnumeration ProblemClassificationEnum.
    $ImpactList         = Get-SCSMEnumeration System.WorkItem.TroubleTicket.ImpactEnum.
    $UrgencyList        = Get-SCSMEnumeration System.WorkItem.TroubleTicket.UrgencyEnum.
    
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Problem Creation"
}

END
{
    1..$count|%{
        $i = $_
        Write-Progress -Activity "Creating Problem" -Status $i -perc ([int]($i/$count * 100))

        # This is the date 
        $CreatedDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,90))

        $ACIs = Get-RandomListFromList $CIList 5 # $CIList[$rlist]
        $RCIs = Get-RandomListFromList $CIList 4 # $CIList[$rlist]

        $Impact         = Get-RandomItemFromList $ImpactList
        $Urgency        = Get-RandomItemFromList $UrgencyList
        $Priority       = Get-Priority $Urgency.DisplayName $Impact.DisplayName
        $Status         = Get-RandomItemFromList $StatusList
        # DEBUG::: $Status         = $StatusList | ?{$_.Name -match "Resolv"}
        $Source         = Get-RandomItemFromList $SourceList
        $Classification = Get-RandomItemFromList $ClassificationList
        $Resolution     = Get-RandomItemFromList $ResolutionList


        if     ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(4)  }
        elseif ( $Urgency -match "medium" ) { $TargetResolutionTime = $CreatedDate.AddHours(24) }
        elseif ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(72) }
        else                                { $TargetResolutionTime = $CreatedDate.AddHours(48) }

        $AffectedUser  = Get-RandomItemFromList $users
        $AssignedUser  = Get-RandomItemFromList $users
        $CreatedByUser = Get-RandomItemFromList $users
        $ResolvedBy    = Get-RandomItemFromList $users
        $ClosedBy      = Get-RandomItemFromList $users

        # by default this creates 5 
        # $global:FASTREAMS = new-FileAttachmentStream

        # CREATE THE SEED HASH TABLE
        $ProblemSeed = @{
                Id                   = "CustomProblem{0}"
                Urgency              = $Urgency
                Impact               = $Impact
                Status               = $Status
                Source               = $Source
                Classification       = $Classification
                Resolution           = $Resolution
                Priority             = $Priority
                # ClosedDate
                # ResolvedDate
                Title                = get-loremipsum 6
                Description          = get-loremipsum 22
                CreatedDate          = $CreatedDate
                KnownError           = [bool]($random.Next(0,2))
                RequiresMajorProblemReview = [bool]($random.Next(0,2))
            }

        # FINISH THE SEED HASH TABLE
        # set up for closed and resolved status
        if ( $status -match "Closed" -or $status -match "Resolved" )
        {
            $ProblemSeed['ResolutionDescription'] = get-lorem 22
            $ProblemSeed['ResolutionCategory']    = Get-RandomItemFromList $ResolutionList
            $ResolveDiff = ($TargetResolutionTime - $CreatedDate).TotalHours
            $TimeVariance = $RANDOM.Next(-$ResolveDiff,$ResolveDiff)
            $ResolvedDate = $TargetResolutionTime.AddHours($TimeVariance)
            $ProblemSeed['ResolvedDate'] = $ResolvedDate
            if ( $status -match "Closed" )
            {
                $ProblemSeed['ClosedDate'] = $ResolvedDate.AddHours($RANDOM.Next(0,2)*24)
            }
            
        }

        $p = @{
            __CLASS = "System.WorkItem.Problem"
            __OBJECT = $ProblemSeed
            # Now for the Aliases
            AssignedTo             = $AssignedUser
            CreatedBy            = $CreatedByUser
            AffectedConfigItems      = $ACIs
            }
        if ( $status -match "Resolved" ) { $p['ResolvedBy'] = $ResolvedBy }
        if ( $status -match "Closed" )   { $p['ResolvedBy'] = $ResolvedBy; $p['ClosedBy'] = $ClosedBy }

    $p } | new-SCSMOBjectProjection -Type $PType -bulk -verbose:$verbose -whatif:$whatif -debug:$debug

}
