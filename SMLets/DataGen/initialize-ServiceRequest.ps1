param ( $count = 10 , $ComputerName = "localhost", [switch]$whatif, [switch]$Verbose, $EMG)

BEGIN
{
	. ./common

    if ( ! ( get-scsmclass System.WorkItem.ServiceRequest))
    {
        Write-Host -For RED "System.WorkItem.ServiceRequest type not found, cannot continue"
        exit
    }


	#
	# CREATE SERVICE REQUEST  
	#
	$RANDOM = new-object System.Random
	$PTYPE = "System.WorkItem.ServiceRequestProjection"
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
	$KAList = get-scsmobject (get-scsmclass System.Knowledge.Article) -MaxCount 21
	if ( $KAList.Count -lt 20 ) {
        	Write-Error "Not enough KAs, go make some more"
	        exit
        }
        
        
    $IDD = new-object Microsoft.EnterpriseManagement.ConnectorFramework.IncrementalDiscoveryData
    if ( ! $EMG )
    {
    $EMG = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $ComputerName
    }
    # A USER
    $user = get-scsmobject (get-scsmclass ^System.User$) -filter "DisplayName -like 'james%'"
    $userList = get-scsmobject (get-scsmclass System.User$) -filter 'LastName -like "%"'
    $computerList = get-scsmobject (get-scsmclass Microsoft.Windows.computer$)
    $incidentList = get-scsmobject (get-scsmclass System.WorkItem.Incident$)

    trap { write-host "some error"; $error[0]; exit }
        
    # A function to add a Review and multiple Manual activities
    function Add-RAMA
    {
        param ( $currentActivity, [int]$manualActivityCount ) 
        # Review activity
        $ArgHash = Get-InstanceHash -id "RA{0}" -seq 1 -status "In Progress" -Stage "ValidateAndReview"
        $ReviewActivity = new-scsmobject (get-scsmclass System.WorkItem.Activity.ReviewActivity) -PropertyHashtable $ArgHash -nocommit
        $currentActivity.Add($ReviewActivity,$workItemContainsActivityRelationship.Target)
        # Create Reviewer Object, Add Reviewer Relationship and add user
            $Reviewer = new-scsmobject (get-scsmclass System.Reviewer) -PropertyHashtable @{ ReviewerId = "{0}" } -nocommit
            $CurrentActivity.Item("Activity")[0].Add($reviewer,$activityHasReviewerRelationship.Target)
            $CurrentActivity.Item("Activity")[0].Item("Reviewer")[0].Add((get-randomitemfromlist $userlist),$reviewerIsUserRelationship.Target)
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
        
        
	Write-Progress -Activity "Setting Up Environment" -Status "Getting Enumerations"
	# ENUMERATIONS
	# the "." at the end of the enumeration is required to be sure we get the list
	$areaListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestAreaEnum$)
	$implementationResultsEnumListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestImplementationResultsEnum$)
	$priorityListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestPriorityEnum$)
	$sourceListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestSourceEnum$)
	$statusListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestStatusEnum$)
	# $supportGroupListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestSupportGroupEnum$)
	$actionList = get-scsmchildenumeration -enum (get-scsmenumeration System.WorkItem.ActionLogEnum$)
	$urgencyListSR = get-scsmchildenumeration -enum (get-scsmenumeration ServiceRequestUrgencyEnum$)
    
	# CLASSES	
	$RelatedwIList = get-scsmclass System.Workitem | ?{$_.Name -match "m.Incident$|ChangeRequest$|Problem$|Activity$|ReleaseRecord$" -and ! $_.abstract}|get-scsmobject
	$relatedServiceList = get-scsmobject (get-scsmclass Microsoft.SystemCenter.BusinessService)
        Write-Progress -Activity "Setting Up Environment" -Status "Starting Service Request Creation"
        
    $srTemplate =  Get-SCSMObjectTemplate "servicerequest"
    #$desiredTemplate = $srTemplate.Identifier | Foreach-Object {$_.ToString()} 
}

END
{
	1..$count|%{
	$i = $_
	Write-Progress -Activity "Creating Service Request" -Status $i -perc ([int]($i/$count * 100))

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
	$Area				= Get-RandomItemFromList $areaListSR 
	$ImplementationResults		= Get-RandomItemFromList $implementationResultsEnumListSR 
	$Priority			= Get-RandomItemFromList $priorityListSR 
	$Source				= Get-RandomItemFromList $sourceListSR 
	$Status				= Get-RandomItemFromList $statusListSR 
	$Urgency			= Get-RandomItemFromList $urgencyListSR 
	$ActualCost			= $Random.Next(1000,9999)/100
	$ActualDowntimeEndDate		= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
	$ActualEndDate			= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))	
	$ActualWork			= $Random.Next(10000,999999)/100
	$CompletedDate			= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))	
	# DEBUG::: $Status         = $StatusList | ?{$_.Name -match "Resolv"}
	$Escalated = $false
	if ( $Resolution.Name -eq "IncidentResolutionCategoryEnum.FixedByHigherTierSupport") { $Escalated = $true }
	if     ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(4)  }
	elseif ( $Urgency -match "medium" ) { $TargetResolutionTime = $CreatedDate.AddHours(24) }
	elseif ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(72) }
	else  { $TargetResolutionTime = $CreatedDate.AddHours(48) }
	$PrimaryOwner =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
	$AffectedUser =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
	$AssignedUser =  Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
	$CreatedByUser = Get-RandomItemFromList $users # [$RANDOM.Next(0,$users.Count)]
	$RelatedWIs    = Get-RandomListFromList $RelatedWIList 2
	# by default this creates 5 
	# $global:FASTREAMS = new-FileAttachmentStream
	
	$seedclassSR = get-scsmclass ^System.WorkItem.ServiceRequest$
	$seedPropertyValuesSR = @{
		ActualCost			        = $Random.Next(1000,9999)/100
		ActualDowntimeEndDate		= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ActualDowntimeStartDate		= [datetime]::Now.AddDays(-$RANDOM.Next(30,60))
		ActualEndDate		     	= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ActualStartDate		     	= [datetime]::Now.AddDays(-$RANDOM.Next(30,60))
		ActualWork			        = $Random.Next(10000,999999)/100
		Area			         	= $Area				
		CompletedDate		     	= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ContactMethod		    	= Get-Lorem 8
		CreatedDate			        = [datetime]::Now.AddDays(-$RANDOM.Next(60,90))
		Description		         	= Get-Lorem 8
		DisplayName			        = Get-Lorem 8
		Id                          = "CustomSR{0}"
		ImplementationResults		= $ImplementationResults		
		IsDowntime			        = "False"
		IsParent			        = "False"
		Notes				        = Get-Lorem 8
		PlannedCost			        = $Random.Next(10000,999999)/100
		PlannedWork			        = $Random.Next(10000,999999)/100
		Priority			     	= $Priority			
		RequiredBy			        = [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ScheduledDowntimeEndDate	= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ScheduledDowntimeStartDate	= [datetime]::Now.AddDays(-$RANDOM.Next(30,60))
		ScheduledEndDate	     	= [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
		ScheduledStartDate	    	= [datetime]::Now.AddDays(-$RANDOM.Next(30,60))
		Source				        = $Source				
		Status				        = $Status				
		TemplateId			        = "ServiceManager.ServiceRequest.Library.Template.DefaultServiceRequest"
		Title				        = "Title as of " + [datetime]::Now
		Urgency		      	        = $Urgency				
		UserInput			        = "<UserInputs><UserInput Question='Required Question 1' Answer='DATA1' Type='string'/><UserInput Question='Optional Question 1' Answer='DATA2' Type='string'/><UserInput Question='Display Information' Answer='DATA3' Type='richtext'/></UserInputs>"
	}
    # write-host -for red "ServiceRequest Status is $status"

    # FINISH THE SEED HASH TABLE
    # set up for closed and resolved status
    if ( $status -match "Closed" -or $status -match "Resolved" )
    {
		$seedPropertyValuesSR['ActualCost'] = $ActualCost			
		$seedPropertyValuesSR['ActualDowntimeEndDate'] = $ActualDowntimeEndDate		
		$seedPropertyValuesSR['ActualWork'] = $ActualWork			
		$seedPropertyValuesSR['CompletedDate'] = $CompletedDate	
		if ( $status -match "Closed" )
		{
			$seedPropertyValuesSR['ActualEndDate'] = $ActualEndDate			
            }            
    }

    # NOW CREATE THE REST OF THE SERVICE REQUEST
    # User 10 Comments
    $AnalystComments = 1..10 | %{
            @{
            __CLASS = "System.WorkItem.TroubleTicket.AnalystCommentLog"
            __OBJECT = @{ 
               Id = [string][guid]::NewGuid()
               Comment = get-lorem 20 
               EnteredDate = $CreatedDate.AddHours($RANDOM.Next(0,5))
               EnteredBy = $CreatedByUser.DisplayName
               }
            }
        }
    # Create 10 analyst comments
    $ActionLog = 1..10 | %{
            @{
            __CLASS = "System.WorkItem.TroubleTicket.ActionLog"
            __OBJECT = @{ 
                Id = [string][guid]::NewGuid().ToString(); 
		    ActionType = Get-RandomItemFromList $actionList
                Description = get-lorem 20 
                EnteredDate = $CreatedDate.AddHours($RANDOM.Next(2,6));
                EnteredBy = $AssignedUser.DisplayName
		    Title = get-lorem 6
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

    $publishedEnum = (get-scsmenumeration system.offering.statusenum.published).id
	$rolist = get-scsmclass System.RequestOffering | get-scsmobject -filter "Status -eq $publishedEnum"
    
    $relatedServiceList = get-scsmobject (get-scsmclass Microsoft.SystemCenter.BusinessService)
    $Service = Get-RandomItemFromList $relatedServiceList
	
	#
	# CREATE SERVICE REQUEST PROJECTION
	#
    $p = new-scsmobjectprojection -nocommit System.WorkItem.ServiceRequestProjection @{
            __CLASS = "System.WorkItem.ServiceRequest"
            __OBJECT = $seedPropertyValuesSR 
            # Now for the Aliases            
            AssignedTo               = $AssignedUser
            CreatedBy                = $CreatedByUser
            AffectedUser             = $AffectedUser
            RelatedConfigItems       = $RCIs
            AboutConfigItem          = $ACIs            
            RelatedWorkItems         = $RelatedWIs            
            RelatedKnowledgeArticles = $KnowledgeArticles
	        RelatedRequestOffering   = Get-RandomItemFromList $rolist 
            ActionLog                = $ActionLog
            AnalystCommentLog        = $AnalystComments
            AffectedServices         = $Service
            #Activity
            #RelatedWorkItemSource 
            #FileAttachments            
            }
  
    $workItemContainsActivityRelationship = Get-SCSMRelationshipClass WorkItemContainsActivity
    
    $currentActivity = $p
    #Write-Progress -Activity "Creating Projection" -Status "Creating RA/MA Activities ($i of 3)" -perc (($ChangeCount/$count)*100)
    Add-RAMA $currentActivity ($Random.Next(3,6))
            
	        # add the projection to the IncrementalDiscoveryDataPacket
        if ( $whatif )
        {
            "What if: Performing operation 'Intialize-ServiceRequest' on Target $p"
        }
        else
        {
            Write-Verbose -verbose:$verbose $p
            $IDD.Add($p)           
        }
   }
   
   #Write-Progress -Activity "Creating Projection" -Status "Committing Projections" -perc (($ChangeCount/$count)*100)

    if ( ! $whatif )
    {
        write-host -for red "ServiceRequest committing"
        Write-Verbose -verbose:$verbose "Committing $count ServiceRequest"
        $IDD.Commit($EMG)
    }

}
