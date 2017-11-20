param ( $count = 10 , [switch]$whatif, [switch]$Verbose)

BEGIN
{

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
    $PTYPE = "System.WorkItem.Incident.ProjectionType"
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
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Knowledge Articles"
    $KAList = get-scsmobject -class (get-scsmclass -name System.Knowledge.Article) -MaxCount 60
    if ( $KAList.Count -lt 20 ) {
        Write-Error "Not enough CIs, go make some more"
        exit
        }
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Enumerations"
    # ENUMERATIONS
    # the "." at the end of the enumeration is required to be sure we get the list
    $TierQueueList      = Get-SCSMEnumeration IncidentTierQueuesEnum.
    $StatusList         = Get-SCSMEnumeration IncidentStatusEnum.
    $SourceList         = get-scsmenumeration IncidentSourceEnum.
    $ResolutionList     = Get-SCSMEnumeration IncidentResolutionCategoryEnum.
    $ClassificationList = Get-SCSMEnumeration IncidentClassificationEnum.
    # RELATIONSHIPS
    $billableTimeUser   = Get-SCSMRelationshipClass System.WorkItem.BillableTimeHasWorkingUser
    $billableTimeWork   = Get-SCSMRelationshipClass System.WorkItemHasBillableTime

    # CLASSES
    $billableTimeClass = get-scsmclass system.workitem.billabletime
    
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Incident Creation"
}

END
{
    1..$count|%{
        $i = $_
        Write-Progress -Activity "Creating Incident" -Status $i -perc ([int]($i/$count * 100))

        # This is the date 
        $CreatedDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,90))

        $ACIs = Get-RandomListFromList $CIList 5 # $CIList[$rlist]
        $RCIs = Get-RandomListFromList $CIList 4 # $CIList[$rlist]
        $RCIs = Get-RandomListFromList $KAList 4 # $CIList[$rlist]

        $PriorityHash = @{ Low = 1; Medium = 2; High = 3 }
        $ImpactList   = "Low","Medium","High"
        $Impact         = Get-RandomItemFromList $ImpactList
        # bloody random numbers - sleep for a bit here
        start-sleep -m 5
        $Urgency        = Get-RandomItemFromList $ImpactList
        $PriorityValue  = $PriorityHash[$Urgency] * $PriorityHash[$Impact]
        $Priority       = $PriorityValue
        $TierQueue      = Get-RandomItemFromList $TierQueueList
        $Status         = Get-RandomItemFromList $StatusList
        # DEBUG::: $Status         = $StatusList | ?{$_.Name -match "Resolv"}
        $Source         = Get-RandomItemFromList $SourceList
        $Classification = Get-RandomItemFromList $ClassificationList

        $Escalated = $false
        if ( $Resolution.Name -eq "IncidentResolutionCategoryEnum.FixedByHigherTierSupport") { $Escalated = $true }

        if     ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(4)  }
        elseif ( $Urgency -match "medium" ) { $TargetResolutionTime = $CreatedDate.AddHours(24) }
        elseif ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(72) }
        else                                { $TargetResolutionTime = $CreatedDate.AddHours(48) }

        $PrimaryOwner =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
        $AffectedUser =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
        $AssignedUser =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
        $CreatedByUser = Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]

        # by default this creates 5 
        # $global:FASTREAMS = new-FileAttachmentStream

        # CREATE THE SEED HASH TABLE
        $IncidentSeed = @{
                Status               = $Status
                Source               = $Source
                Impact               = $Impact
                Urgency              = $Urgency
                TierQueue            = $TierQueue
                CreatedDate          = $CreatedDate
                Classification       = $Classification
                Title                = get-loremipsum 6
                Description          = get-loremipsum 22
                Priority             = $Priority
                Escalated            = $Escalated
                TargetResolutionTime = $TargetResolutionTime
                Id                   = "CustomIR{0}"
            }

        # FINISH THE SEED HASH TABLE
        # set up for closed and resolved status
        if ( $status -match "Closed" -or $status -match "Resolved" )
        {
            $IncidentSeed['ResolutionDescription'] = get-lorem 22
            $IncidentSeed['ResolutionCategory']    = Get-RandomItemFromList $ResolutionList
            $ResolveDiff = ($TargetResolutionTime - $CreatedDate).TotalHours
            $TimeVariance = $RANDOM.Next(-$ResolveDiff,$ResolveDiff)
            $ResolvedDate = $TargetResolutionTime.AddHours($TimeVariance)
            $IncidentSeed['ResolvedDate'] = $ResolvedDate
            if ( $status -match "Closed" )
            {
                $IncidentSeed['ClosedDate'] = $ResolvedDate.AddHours($RANDOM.Next(0,2)*24)
            }
            
        }

        # NOW CREATE THE REST OF THE INCIDENT
        # User Comments
        $UserComments = @{
                __CLASS = "System.WorkItem.TroubleTicket.UserCommentLog"
                __OBJECT = @{ 
                   Id = [string][guid]::NewGuid()
                   Comment = get-loremipsum 20 
                   EnteredDate = $CreatedDate.AddHours($RANDOM.Next(0,5))
                   EnteredBy = $CreatedByUser.DisplayName
                   }
            }
        # Create 3 analyst comments
        $AnalystComments = 1..3 | %{
                @{
                __CLASS = "System.WorkItem.TroubleTicket.AnalystCommentLog"
                __OBJECT = @{ 
                    Id = [string][guid]::NewGuid().ToString(); 
                    Comment = get-loremipsum 20 
                    EnteredDate = $CreatedDate.AddHours($RANDOM.Next(2,6));
                    EnteredBy = $AssignedUser.DisplayName
                    }
                }
            }
        
        $KnowledgeArticles = @{
                __CLASS = "System.Knowledge.Article"
                __OBJECT = @{ 
                    Status = "Published" 
                    ArticleId = "CustomKA{0}"
                    Title = get-loremipsum 6
                    Abstract = get-loremipsum 4
                    CreatedBy = "Joe User"
                    CreatedDate = $CreatedDate
                    }
                },$KAs

        #### File Attachments must have a new instance for each attachement
        #### even if it's the same file
        # NO FILE ATTACHMENTS FOR NOW
        # $FileAttachments = $FASTREAMS | %{
        #    $stream = $_
        #    @{
        #        __CLASS = "System.FileAttachment"
        #        ### New-FileAttachmentInstance needs clean up which is done in the finally
        #        ### block. It creates a script scope array of file streams
        #        ### which must be cleaned up
        #        __OBJECT = @{
        #            Description = Get-Loremipsum 12
        #            AddedDate = [datetime]::Now.AddDays(-180)
        #            Extension = ".tmp"
        #            Id = [guid]::NewGuid().ToString()
        #            Content = $stream
        #            Size = $stream.Length
        #            }
        #        }
        #    }

        $p = @{
            __CLASS = "System.WorkItem.Incident"
            __OBJECT = $IncidentSeed
            # Now for the Aliases
            PrimaryOwner             = $PrimaryOwner
            AffectedUser             = $AffectedUser
            AssignedUser             = $AssignedUser
            CreatedByUser            = $CreatedByUser
            UserComments             = $UserComments
            AnalystComments          = $AnalystComments
            AffectedConfigItems      = $ACIs
            RelatedConfigItems       = $RCIs
            RelatedKnowledgeArticles = $KnowledgeArticles
            # FileAttachments
            }

    $p } | new-SCSMOBjectProjection -Type $PType -bulk -verbose:$verbose -whatif:$whatif

        # Create the Billable time - this doesn't use a projection, so create the relationships
        # directly
        # New-SCSMObjectProjection must use -passthru and then this is done in a foreach loop
        #| %{
        #    $workitem = $_.object
        #    1..3 | %{
        #        $id = [guid]::NewGuid().ToString()
        #        $bti = new-scsmobject -NoCommit $BillableTimeClass -PropertyHashtable @{ 
        #            DisplayName = $id
        #            Id = $id
        #            TimeInMinutes = $RANDOM.Next(30,55);
        #            LastUpdated = [datetime]::Now 
        #            }
        #        @{ 
        #            Relationship = $billableTimeUser
        #            Source = $workitem
        #            Target = $bti
        #        }
        #        @{
        #            Relationship = $billableTimeWork
        #            Source = $bti
        #            Target = $AssignedUser
        #        }
        #        start-sleep -mil 10
        #    }
        #} | new-scsmRelationshipObject  
        #$wi = $pp.object
        #for($itr = 0; $itr -lt 3; $itr++)
        #{
        #    $id = [guid]::newGuid().ToString()
        #    # this requires an uncommited instance
        #    # when the relationship is commited, so will the instance
        #    $bti = new-scsmobject -NoCommit $BillableTimeClass -PropertyHashtable @{ 
        #        DisplayName = $id
        #        Id = $id
        #        TimeInMinutes = $RANDOM.Next(30,55);
        #        LastUpdated = [datetime]::Now 
        #        }
        #    New-SCSMRelationshipObject -Relationship $billableTimeUser -Source $wi -Target $bti
        #    New-SCSMRelationshipObject -Relationship $billableTimeWork -Source $bti -Target $AssignedUser
        #    this sleep is to be sure that the time spent changes
        #}
}
