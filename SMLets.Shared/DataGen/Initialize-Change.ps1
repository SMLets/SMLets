param ( $count = 10 , [switch]$whatif, [switch]$Verbose, [switch]$debug)

BEGIN
{
    . ./Common.ps1

    $PTYPE = "System.WorkItem.ChangeRequestProjection"

    # RESOURCES NEEDED BUILT BY COMMON.PS1
    #
    # INSTANCEHASH[$UserClass]
    # INSTANCEHASH[$CIClass]
    # ENUMHASH["ProblemStatusEnum"]
    # ENUMHASH["ProblemSourceEnum"]
    # ENUMHASH["ProblemResolutionEnum"]
    # ENUMHASH["ProblemClassificationEnum"]
    # ENUMHASH["System.WorkItem.TroubleTicket.ImpactEnum"]
    # ENUMHASH["System.WorkItem.TroubleTicket.UrgencyEnum"]

    # Add to ENUMHASH
        "ChangeAreaEnum", "ChangeCategoryEnum", "ChangeImpactEnum",
        "ChangeImplementationResultsEnum", "ChangePriorityEnum",
        "ChangeRiskEnum", "ChangeStatusEnum" | %{
            $ENUMHASH[$_] = Get-EnumList $_
            }

    # Add to ENUMHASH for Activities
        "ApprovalEnum", "ActivityAreaEnum", "ActivityPriorityEnum",
        "ActivityStageEnum", "ActivityStatusEnum" | %{
            $ENUMHASH[$_] = Get-EnumList $_
            }

    $ENUMHASH['DecisionEnum'] = Get-EnumList DecisionEnum

    # INSTANCEHASH needs a System.WorkItem
    $INSTANCEHASH['System.WorkItem'] = Get-Instances "System.WorkItem" 60

    # SEED PROPERTIES
        #CreatedDate               DateTime
        #ScheduledStartDate        DateTime
        #ScheduledEndDate          DateTime
        #ActualStartDate           DateTime
        #ActualEndDate             DateTime

    # Projection components
    # Activity                System.WorkItem.Activity
    # CreatedBy               System.User
    # AssignedTo              System.User
    # AffectedUser            System.User
    # RelatedWorkItem         System.WorkItem
    # RelatedWorkItemSource   System.WorkItem
    # RelatedConfigItem       System.ConfigItem
    # AboutConfigItem         System.ConfigItem
    # RelatedKnowledge        System.Knowledge.Article
    

    
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Problem Creation"
}

END
{
    # THIS MUST BE DONE BY FOREACH-OBJECT TO GET BULK OPERATIONS
    1..$count|foreach-object {
        $i = $_
        Write-Progress -Activity "Creating Problem" -Status $i -perc ([int]($i/$count * 100))
        # This is the created date for the projection
        $CreatedDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,90))

        $ACIs = Get-RandomListFromList $INSTANCEHASH[$CIClass] 4

        $Status         = Get-RandomItemFromList $ENUMHASH['ActivityStatusEnum']
        $Classification = Get-RandomItemFromList $ENUMHASH['ProblemClassificationEnum']
        $Resolution     = Get-RandomItemFromList $ENUMHASH['ProblemResolutionEnum']
        $ResolvedBy     = Get-RandomItemFromList $INSTANCEHASH[$UserClass]
        $ClosedBy       = Get-RandomItemFromList $INSTANCEHASH[$UserClass]

        # CREATE THE SEED HASH TABLE
        # WorkItem.Problem
        $Seed = @{
            Id                        = "CustomCR{0}"
            Reason                    = Get-LoremIpsum 12
            Notes                     = Get-LoremIpsum 24
            ImplementationPlan        = Get-LoremIpsum 30
            RiskAssessmentPlan        = Get-LoremIpsum 40
            BackoutPlan               = Get-LoremIpsum 80
            TestPlan                  = Get-LoremIpsum 60
            PostImplementationReview  = Get-LoremIpsum 32
            Title                     = Get-LoremIpsum 8
            Description               = Get-LoremIpsum 32
            ContactMethod             = Get-LoremIpsum 5

            Category                  = Get-RandomItemFromList $ENUMHASH['ChangeCategoryEnum']
            Priority                  = Get-RandomItemFromList $ENUMHASH['ChangePriorityEnum']
            Impact                    = Get-RandomItemFromList $ENUMHASH['System.WorkItem.TroubleTicket.ImpactEnum']
            Risk                      = Get-RandomItemFromList $ENUMHASH['ChangeRiskEnum']
            ImplementationResults     = Get-RandomItemFromList $ENUMHASH['ChangeImplementationResultsEnum']
            Status                    = $Status
            CreatedDate               = $CreatedDate
            }
        $Activites = 1..3 | %{
            #set-psdebug -trace 2
            $Start = $CreatedDate.Addhours((Get-RandomPercentage))
            $End   = $Start.AddHours((Get-RandomPercentage)*2)
            #set-psdebug -off
            $activitySeed = @{
                ActualCost                     = Get-RandomDouble
                ActualDowntimeEndDate          = $end.AddMinutes((Get-RandomPercentage))
                ActualDowntimeStartDate        = $start.AddMinutes((Get-RandomPercentage))
                ActualEndDate                  = $end.AddMinutes((Get-RandomPercentage))
                ActualStartDate                = $start.AddMinutes((Get-RandomPercentage))
                ActualWork                     = Get-RandomDouble
                ApprovalCondition              = Get-RandomItemFromList $ENUMHASH['ApprovalEnum']
                ApprovalPercentage             = Get-RandomPercentage
                Area                           = Get-RandomItemFromList $ENUMHASH['ActivityAreaEnum']
                Comments                       = get-loremipsum 22
                ContactMethod                  = get-loremipsum 22
                CreatedDate                    = $date
                Description                    = get-loremipsum 22
                Documentation                  = Get-LoremIpsum
                Id                             = "CustomRA{0}"
                IsDowntime                     = Get-RandomBool
                IsParent                       = Get-RandomBool
                LineManagerShouldReview        = Get-RandomBool
                Notes                          = get-loremipsum 12
                OwnersOfConfigItemShouldReview = Get-RandomBool
                PlannedCost                    = Get-RandomDouble
                PlannedWork                    = Get-RandomDouble
                Priority                       = Get-RandomItemFromList $ENUMHASH['ActivityPriorityEnum']
                RequiredBy                     = $end
                ScheduledDowntimeEndDate       = $end
                ScheduledDowntimeStartDate     = $start
                ScheduledEndDate               = $End
                ScheduledStartDate             = $Start
                SequenceId                     = $_
                Skip                           = Get-RandomBool
                Stage                          = Get-RandomItemFromList $ENUMHASH['ActivityStageEnum']
                Status                         = Get-RandomItemFromList $ENUMHASH['ActivityStatusEnum']
                Title                          = get-loremipsum 12
                }
            $ActivityProjectionType = "System.WorkItem.Activity.ReviewActivityProjection"
            
            @{
                __CLASS = "System.WorkItem.Activity.ReviewActivity"
                __OBJECT = $seed
                ActivityCreatedBy = Get-RandomItemFromList $INSTANCEHASH[$UserClass]
                ActivityAssignedTo = Get-RandomItemFromList $INSTANCEHASH[$UserClass]
                Reviewer = @{
                    __CLASS = "System.Reviewer"
                    __OBJECT = @{
                        Comments = Get-LoremIpsum 22
                        Decision = Get-RandomItemFromList $ENUMHASH['DecisionEnum']
                        DecisionDate = $CreatedDate.AddHours(12)
                        ReviewerId = [guid]::NewGuid().ToString()
                        }
                    }
                }| new-scsmobjectprojection -Type $ActivityProjectionType -nocommit -whatif:$whatif -verbose:$verbose

        }
        # CREATE THE PROJECTION HASH TABLE
        $p = @{
            __CLASS = "System.WorkItem.Problem"
            __OBJECT = $Seed
            # Now for the Aliases
            AssignedTo          = Get-RandomItemFromList $INSTANCEHASH[$UserClass]
            CreatedBy           = Get-RandomItemFromList $INSTANCEHASH[$UserClass]
            AffectedConfigItems = $ACIs
            }

        # FINISH THE PROJECTION
        if     ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(4)  }
        elseif ( $Urgency -match "medium" ) { $TargetResolutionTime = $CreatedDate.AddHours(24) }
        elseif ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(72) }
        else                                { $TargetResolutionTime = $CreatedDate.AddHours(48) }
        # set up for closed and resolved status
        if ( $status -match "Closed" -or $status -match "Resolved" )
        {
            $p.__OBJECT['ResolutionDescription'] = get-lorem 22
            $ResolveDiff          = ($TargetResolutionTime - $CreatedDate).TotalHours
            $TimeVariance         = $RANDOM.Next(-$ResolveDiff,$ResolveDiff)
            $ResolvedDate         = $TargetResolutionTime.AddHours($TimeVariance)
            $p.__OBJECT['ResolvedDate'] = $ResolvedDate
            if ( $status -match "Closed" )
            {
                $p.__OBJECT['ClosedDate'] = $ResolvedDate.AddHours($RANDOM.Next(0,2)*24)

                $p['ResolvedBy']    = $ResolvedBy
                $p['ClosedBy']      = $ClosedBy 
            }
            else
            {
                $p['ResolvedBy'] = $ResolvedBy
            }
        }
        # HAND THE PROJECTION TO THE CMDLET
    $p }  | new-SCSMOBjectProjection -Type $PType -bulk -verbose:$verbose -whatif:$whatif -debug:$debug
}
