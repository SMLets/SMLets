param ( $count = 1, $ComputerName = "localhost" )
BEGIN
{
    . ./Common.ps1
    $IDD = new-object Microsoft.EnterpriseManagement.ConnectorFramework.IncrementalDiscoveryData
    $EMG = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $computername
    # A USER
    $user = get-scsmobject ^System.User$ -filter "DisplayName -like 'james%'"
    trap { write-error $error[0]; exit }
    # A function to add a Review and multiple Manual activities
    function Add-RAMA
    {
        param ( $currentActivity, [int]$manualActivityCount ) 
        # Review activity
        $ArgHash = Get-InstanceHash -id "RA{0}" -seq 1 -status "In Progress" -Stage "Release"
        $ReviewActivity = new-scsmobject System.WorkItem.Activity.ReviewActivity -PropertyHashtable $ArgHash -nocommit
        $currentActivity.Add($ReviewActivity,$workItemContainsActivityRelationship.Target)
        # Create Reviewer Object, Add Reviewer Relationship and add user
            $Reviewer = new-scsmobject System.Reviewer -PropertyHashtable @{ ReviewerId = "{0}" } -nocommit
            $CurrentActivity.Item("Activity")[0].Add($reviewer,$activityHasReviewerRelationship.Target)
            $CurrentActivity.Item("Activity")[0].Item("Reviewer")[0].Add($user,$reviewerIsUserRelationship.Target)
        # Manual Activities
        for($i = 2; $i -lt (2+$manualActivityCount); $i++)
        {
            $ArgHash = Get-InstanceHash -id "MA{0}" -seq $i -status "Pending" -Stage "Release"
            $MA = new-scsmobject System.WorkItem.Activity.ManualActivity -PropertyHashtable $ArgHash -nocommit
            $currentActivity.Add($MA,$workItemContainsActivityRelationship.Target)
        }
    }
}

END
{
    trap { write-error $error[0]; exit }
    for ( $ReleaseCount = 0; $ReleaseCount -lt $count; $ReleaseCount++ )
    {
        Write-Progress -Activity "Creating Projection" -Status "Creating Seed" -perc (($ReleaseCount/$count)*100)
        $CreatedDate = [datetime]::Now.AddDays($RANDOM.Next(-90,-30))
        $RequiredBy = $CreatedDate.AddDays($RANDOM.Next(30,90))
        $ReleaseProjection = new-scsmobjectprojection -nocommit System.WorkItem.ReleaseRecordProjection @{ 
            __CLASS = "System.WorkItem.ReleaseRecord"; 
            __OBJECT = @{ 
                Id                 = "customRR{0}" 
                Title              = get-lorem 6
                Description        = get-lorem 24
                RequiredBy         = $RequiredBy
                ScheduledEndDate   = $RequiredBy.AddDays($RANDOM.Next(-7,-2))
                ScheduledStartDate = $CreatedDate.AddDays($RANDOM.Next(7,14))
                PlannedCost        = $Random.Next(10000,999999)/100
                Notes              = get-lorem 60
                CreatedDate        = $CreatedDate
                Category           = "Project"
                Impact             = "Standard"
                Priority           = "High"
                Risk               = "Medium"
                Status             = "New"
                Type               = "Planned"
                }
            } 

        $ReleaseProjection.Add( $user, $workItemCreatedByUserRelationship.Target)
        $ReleaseProjection.Add( $user, $assignedToUserRelationship.Target)

        Write-Progress -Activity "Creating Projection" -Status "Creating Parallel Activity" -perc (($ReleaseCount/$count)*100)
        # THE PARALLEL ACTIVITY - ONLY ONE OF THESE PER Release
        $ArgHash = Get-InstanceHash -id "PA{0}" -seq 1 -status "In Progress" -Stage "Release"
        $pa = new-scsmobject System.WorkITem.Activity.ParallelActivity -PropertyHashtable $ArgHash -nocommit
        $workItemContainsActivityRelationship = Get-SCSMRelationshipClass WorkItemContainsActivity
        $ReleaseProjection.Add($pa,$workItemContainsActivityRelationship.Target)

        Write-Progress -Activity "Creating Projection" -Status "Creating Sequential Activities" -perc (($ReleaseCount/$count)*100)
        # CREATE THE SEQUENTIAL ACTIVITIES
        for($i = 0; $i -lt 3; $i++)
        {
            $ArgHash = Get-InstanceHash -id "SA{0}" -seq $i -status "In Progress" -Stage "Release"
            $sa = new-scsmobject System.WorkItem.Activity.SequentialActivity -PropertyHashtable $ArgHash -nocommit
            $ReleaseProjection.Item("Activity")[0].Add($sa,$workItemContainsActivityRelationship.Target)
            $currentActivity = $ReleaseProjection.Item("Activity")[0].Item("Activity")[$i]
            Write-Progress -Activity "Creating Projection" -Status "Creating RA/MA Activities ($i of 3)" -perc (($ReleaseCount/$count)*100)
            Add-RAMA $currentActivity ($Random.Next(3,6))
        }
        # add the projection to the IncrementalDiscoveryDataPacket
        $IDD.Add($releaseProjection)
    }

    # commit the projections via IncrementalDiscoveryData
    Write-Progress -Activity "Creating Projection" -Status "Committing Projections" -perc (($ReleaseCount/$count)*100)

    $IDD.Commit($EMG)
}
