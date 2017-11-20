param ( $count = 10 , [switch]$whatif, [switch]$Verbose, [switch]$nobulk )

BEGIN
{

    . ./Common.ps1

    $RANDOM = new-object System.Random
    $PTYPE = "System.WorkItem.Incident.ProjectionType"
    ###
    ### SETUP 
    ### Retrieve stuff from the CMDB which will be used later
    ###
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Users"
    $Users = Get-scsmobject (get-scsmclass Microsoft.AD.User$) -MaxCount 60
    if ( $Users.Count -lt 10 ) {
        Write-Error "Not enough users, go make some more"
        exit
        }
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Config Items"
    $CIList = get-scsmobject (get-scsmclass System.ConfigItem$) -MaxCount 60
    if ( $CIList.Count -lt 10 ) {
        Write-Error "Not enough CIs, go make some more"
        exit
        }
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Knowledge Articles"
    $KAList = get-scsmobject (get-scsmclass System.Knowledge.Article) -MaxCount 60
    if ( $KAList.Count -lt 20 ) {
        Write-Error "Not enough KAs, go make some more"
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

    $RelatedwIList = get-scsmclass System.Workitem | ?{$_.Name -match "m.Incident$|ChangeRequest$|Problem$|Activity$|ReleaseRecord$" -and ! $_.abstract}|get-scsmobject

    $relatedServiceList = get-scsmobject (get-scsmclass Microsoft.SystemCenter.BusinessService)
    
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Incident Creation"
}

END
{
    $ProjectionArgs = @{
        Type    = $PType 
        bulk    = ! $nobulk
        verbose = $verbose 
        whatif  = $whatif
        }

    1..$count|%{
        $i = $_
        Write-Progress -Activity "Creating Incident" -Status $i -perc ([int]($i/$count * 100))

        # This is the date 
        $CreatedDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,90))

        $ACIs = Get-RandomListFromList $CIList 5 # $CIList[$rlist]
        $ACIs += Get-RandomItemFromList $relatedServiceList
        $KAIs = Get-RandomListFromList $KAList 4 # $CIList[$rlist]
        $RCIs = Get-RandomListFromList $CIList 4 # $CIList[$rlist]

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
        $RelatedWIs    = Get-RandomListFromList $RelatedWIList 2

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
                Title                = get-lorem 6
                Description          = get-lorem 22
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
        # User 10 Comments
        $UserComments = 1..10 | %{
                @{
                __CLASS = "System.WorkItem.TroubleTicket.UserCommentLog"
                __OBJECT = @{ 
                   Id = [string][guid]::NewGuid()
                   Comment = get-lorem 20 
                   EnteredDate = $CreatedDate.AddHours($RANDOM.Next(0,5))
                   EnteredBy = $CreatedByUser.DisplayName
                   }
                }
            }
        # Create 10 analyst comments
        $AnalystComments = 1..10 | %{
                @{
                __CLASS = "System.WorkItem.TroubleTicket.AnalystCommentLog"
                __OBJECT = @{ 
                    Id = [string][guid]::NewGuid().ToString(); 
                    Comment = get-lorem 20 
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
                    Title = get-lorem 6
                    Abstract = get-lorem 4
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
        #            Description = Get-Lorem 12
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
            RelatedWorkItems         = $RelatedWIs
            RelatedConfigItems       = $RCIs
            RelatedKnowledgeArticles = $KnowledgeArticles
            AffectedConfigItems      = $ACIs
            # FileAttachments
            }

    $p } | new-SCSMOBjectProjection @ProjectionArgs

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
