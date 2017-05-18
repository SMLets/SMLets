param ( $count = 1, [switch]$whatif, [switch]$verbose )
BEGIN
{
    . ./Common.ps1
    $global:IDD = new-object Microsoft.EnterpriseManagement.ConnectorFramework.IncrementalDiscoveryData
    $EMG = @(get-scsmsession)[0]
    # A USER
    $user = get-scsmobject (get-scsmclass ^System.User$) -filter "DisplayName -like 'asttest%'"
    $userList = get-scsmobject (get-scsmclass System.User$) -filter 'LastName -like "%"'
    $computerList = get-scsmobject (get-scsmclass Microsoft.Windows.computer$)
    $incidentList = get-scsmobject (get-scsmclass System.WorkItem.Incident$) -max 60

    $areaList = Get-SCSMChildEnumeration -enumeration (get-scsmenumeration ^ChangeAreaEnum$)

    $statusList = Get-SCSMChildEnumeration -enumeration (get-scsmenumeration ^ChangeStatusEnum$)
    $categoryList = Get-SCSMChildEnumeration -enumeration (get-scsmenumeration ^ChangeCategoryEnum$)
    $riskList = Get-SCSMChildEnumeration -enumeration (Get-SCSMEnumeration ^ChangeRiskEnum$)
    $impactList = Get-SCSMChildEnumeration -enumeration (Get-SCSMEnumeration ^ChangeImpactEnum$)
    $priorityList = Get-SCSMChildEnumeration -enumeration (Get-SCSMEnumeration ^ChangePriorityEnum$)

    trap { write-host "some error"; $error[0]; exit }

    # A function to add a Review and multiple Manual activities
    function Add-RAMA
    {
        param ( $currentActivity, [int]$manualActivityCount ) 
        # Review activity
        $ArgHash = Get-InstanceHash -id "RA{0}" -seq 1 -status "In Progress" -Stage "ValidateAndReview"
        $ReviewActivity = new-scsmobject (get-scsmclass System.WorkItem.Activity.ReviewActivity) -PropertyHashtable $ArgHash -nocommit
        $currentActivity.Add($ReviewActivity,$workItemContainsActivityRelationship.Target)

        # Create multiple Reviewer Objects, Add Reviewer Relationship and add user
        $currentReviewer = 0
        foreach($user in Get-RandomListFromList -list $UserList -count 4)
        {
            $reviewerHash = @{ ReviewerId = "{0}" } 
            if ( $RANDOM.Next(0,2))
            {
                $reviewerHash.Veto = $true
                $reviewerHash.MustVote = $true
            }
            $Reviewer = new-scsmobject (get-scsmclass System.Reviewer) -PropertyHashtable $reviewerHash -nocommit
            $CurrentActivity.Item("Activity")[0].Add($reviewer,$activityHasReviewerRelationship.Target)
            $CurrentActivity.Item("Activity")[0].Item("Reviewer")[$currentReviewer].Add($user,$reviewerIsUserRelationship.Target)
            $currentReviewer++
        }
        # Manual Activities
        $stages = "","", "Approve", "Initiate", "Test", "Develop", "Release"

        for($i = 2; $i -lt (2+$manualActivityCount); $i++)
        {
            $ArgHash = Get-InstanceHash -id "MA{0}" -seq $i -status "Pending" -Stage $stages[$i]
            $MA = new-scsmobject (get-scsmclass System.WorkItem.Activity.ManualActivity) -PropertyHashtable $ArgHash -nocommit
            $currentActivity.Add($MA,$workItemContainsActivityRelationship.Target)
            $assignedToRelationship = get-scsmrelationshipclass System.WorkItemAssignedToUser
            $currentActivity.Item("Activity")[$i-2].Add((get-randomitemfromlist $userList),$assignedToRelationship.Target)

            $aboutConfigItem        = get-scsmrelationshipclass System.WorkItemAboutConfigItem
            $global:foo = $currentactivity
            get-randomlistfromlist $computerlist 5 | %{
                $currentActivity.Item("Activity")[$i-1].Add($_, $aboutConfigItem.Target)
                }
        }
    }

}

END
{
    for ( $ChangeCount = 0; $ChangeCount -lt $count; $ChangeCount++ )
    {
        Write-Progress -Activity "Creating Projection" -Status "Creating Seed" -perc (($ChangeCount/$count)*100)
        $CreatedDate = [datetime]::Now.AddDays($RANDOM.Next(-90,-30))
        $RequiredBy = $CreatedDate.AddDays($RANDOM.Next(30,90))
        $ChangeProjection = new-scsmobjectprojection -nocommit System.WorkItem.ChangeRequestProjection @{ 
            __CLASS = "System.WorkItem.ChangeRequest"; 
            __OBJECT = @{ 
                Id                 = "customCR{0}" 
                Title              = get-lorem 6
                Description        = get-lorem 24
                Reason             = get-lorem 48
                ContactMethod      = "Telegraph"
                RequiredBy         = $RequiredBy
                ScheduledEndDate   = $RequiredBy.AddDays($RANDOM.Next(-7,-2))
                ScheduledStartDate = $CreatedDate.AddDays($RANDOM.Next(7,14))
                PlannedCost        = $Random.Next(10000,999999)/100
                Notes              = get-lorem 60
                CreatedDate        = $CreatedDate
                Category           = Get-RandomItemFromList $categoryList
                Impact             = Get-RandomItemFromList $impactList
                Priority           = Get-RandomItemFromList $priorityList
                Risk               = Get-RandomItemFromList $riskList
                Status             = Get-RandomItemFromList $statusList
                Area               = Get-RandomItemFromList $areaList
                ImplementationPlan = get-lorem 60
                RiskAssessmentPlan = get-lorem 60
                TestPlan           = get-lorem 60
                BackoutPlan        = get-lorem 60
                PostImplementationPlan = get-lorem 120
                }
            } 

        $ChangeProjection.Add( (get-randomitemfromlist $userlist), $workItemCreatedByUserRelationship.Target)
        $ChangeProjection.Add( (get-randomitemfromlist $userlist), $assignedToUserRelationship.Target)

        $relatesToRelationship = get-scsmrelationshipclass System.WorkItemRelatesToConfigItem
        get-randomlistfromlist $computerList 4 | %{ $ChangeProjection.Add($_,$relatesToRelationship.Target) }

        $relatesToWIRelationship = get-scsmrelationshipclass System.WorkItemRelatesToWorkItem
        $ChangeProjection.Add((Get-RandomItemFromList $incidentList),$relatesToWIRelationship.Target)

        $aboutConfigItemRelationship = get-scsmrelationshipclass System.WorkItemAboutConfigItem
        get-randomlistfromlist $computerList 3 | %{ $ChangeProjection.Add($_,$aboutConfigItemRelationship.Target) }
        # $ChangeProjection.Add( (get-randomlistfromlist $computerList 4), $relatesToRelationship.Target))

        #Write-Progress -Activity "Creating Projection" -Status "Creating Parallel Activity" -perc (($ChangeCount/$count)*100)
        # THE PARALLEL ACTIVITY - ONLY ONE OF THESE PER Change
        #$ArgHash = Get-InstanceHash -id "PA{0}" -seq 1 -status "In Progress" -Stage "Change"
        #$pa = new-scsmobject System.WorkITem.Activity.ParallelActivity -PropertyHashtable $ArgHash -nocommit
        $workItemContainsActivityRelationship = Get-SCSMRelationshipClass WorkItemContainsActivity
        #$ChangeProjection.Add($pa,$workItemContainsActivityRelationship.Target)

        Write-Progress -Activity "Creating Projection" -Status "Creating Sequential Activities" -perc (($ChangeCount/$count)*100)
        # CREATE THE SEQUENTIAL ACTIVITIES
        #for($i = 0; $i -lt 3; $i++)
        #{
        #    $ArgHash = Get-InstanceHash -id "SA{0}" -seq $i -status "In Progress" -Stage "Change"
        #    $sa = new-scsmobject System.WorkItem.Activity.SequentialActivity -PropertyHashtable $ArgHash -nocommit
        #    $ChangeProjection.Item("Activity")[0].Add($sa,$workItemContainsActivityRelationship.Target)
        #    $currentActivity = $ChangeProjection.Item("Activity")[0].Item("Activity")[$i]
            $currentActivity = $ChangeProjection
            Write-Progress -Activity "Creating Projection" -Status "Creating RA/MA Activities" -perc (($ChangeCount/$count)*100)
            Add-RAMA $currentActivity ($Random.Next(3,6))
        #}
        # add the projection to the IncrementalDiscoveryDataPacket
        if ( $whatif )
        {
            "What if: Performing operation 'Intialize-ChangeRequest' on Target $changeProjection"
        }
        else
        {
            Write-Verbose -verbose:$verbose $changeProjection 
            # $IDD.Add($changeProjection)
            $changeProjection.Commit()
        }
    }
    # commit the projections via IncrementalDiscoveryData
    Write-Progress -Activity "Creating Projection" -Status "Committing Projections" -perc (($ChangeCount/$count)*100)

    if ( ! $whatif )
    {
        Write-Verbose -verbose:$verbose "Committing $count ChangeRequest"
        #$EMG.Reconnect()
        #$IDD.Commit($EMG)
    }
}
