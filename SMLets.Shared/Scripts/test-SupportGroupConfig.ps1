param ($file, $parentenumname, $verbose = $true)

#For Debugging Only
# $file = "C:\Users\Administrator\Documents\Software\CodePlex\SMLets\Main\Source\SMLets\SMLets\Scripts\SupportGroups.txt"
#$SupportGroups = Get-Content $file
$SupportGroups = "Group1","Group2"

$supportGroups
$parentenumname = "CANRootEnum"

function MakeMPElementIDSafeName 
{
    param([string]$name, [string]$element)
    $return = $name + "." + $element
    # be sure that we done
    $disallowed = '!@#$%^&*()_-+={}[]|\:?/<>.~;''" '.ToCharArray()
    foreach ($char in $disallowed)
    {
        $return = $return.Replace($char, "")
    }
    $return
}

Import-Module SMLets

$IncidentSuffix = "-IN"
$ChangeRequestSuffix = "-CR"
$ProblemSuffix = "-PR"

$ADUserGroupNamePrefix = "SCSM-"
$ADUserGroupNameNotificationRecipientPrefix = "SCSM-"
$ADUserGroupNameNotificationRecipientSuffix = "-FE"

$NotificationSubscriptionUpdateSuffix = "-U"
$NotificationSubscriptionCreateSuffix = "-C"

$EnumElement = "Enum"
$QueueElement = "Queue"
$FolderElement = "Folder"
$Subscription = "Subscription"
$ManagementPack = "ManagementPack"

$IncidentViewSuffix = " Incidents"
$ProblemViewSuffix = " Problems"
$ChangeRequestViewSuffix = " Change Requests"

$ActiveIncidentViewNameSuffix = " Active" + $IncidentViewSuffix
$PendingIncidentViewNameSuffix = " Pending" + $IncidentViewSuffix
$UserRespondedByEmailIncidentViewNameSuffix = " User Responded By Email" + $IncidentViewSuffix
$ResolvedIncidentViewNameSuffix = " Resolved" + $IncidentViewSuffix
$ClosedIncidentViewNameSuffix = " Closed" + $IncidentViewSuffix

$InProgressChangeRequestViewNameSuffix = " In Progress" + $ChangeRequestViewSuffix
$ClosedChangeRequestViewNameSuffix = " Closed" + $ChangeRequestViewSuffix

$ActiveProblemViewNameSuffix = " Active" + $ProblemViewSuffix
$ResolvedProblemViewNameSuffix = " Resolved" + $ProblemViewSuffix
$ClosedProblemViewNameSuffix = " Closed" + $ProblemViewSuffix

$UserRoleIncidentResolverNameSuffix = " Incident Resolvers"
$UserRoleChangeRequestManagerNameSuffix = " Change Managers"
$UserRoleProblemAnalystNameSuffix = " Problem Analysts"

#Get the root incident; problem; and change folders so we can create the support group folders underneath them
$IncidentManagementFolder = Get-SCSMFolder -Name ServiceManager.Console.IncidentManagement
$ChangeManagementFolder = Get-SCSMFolder -Name ServiceManager.Console.ChangeManagementFolder
$ProblemManagementFolder = Get-SCSMFolder -Name ServiceManager.ProblemManagement.Folder.Problem
$ActivitiesFolder = Get-SCSMFolder -Name ServiceManager.Console.ActivityManagement
if($Verbose)
{
    Write-Host "Got $IncidentManagementFolder"
    Write-Host "Got $ChangeManagementFolder"
    Write-Host "Got $ProblemManagementFolder"
    Write-Host "Got $ActivitiesFolder"
}

#Get the Incident Tier Queue (aka Support Group) enum and other enums

$IncidentStatusActive = Get-SCSMEnumeration -Name IncidentStatusEnum.Active$
$IncidentStatusPending = Get-SCSMEnumeration -Name IncidentStatusEnum.Active.Pending$
$IncidentStatusResolved = Get-SCSMEnumeration -Name IncidentStatusEnum.Resolved$
$IncidentStatusClosed = Get-SCSMEnumeration -Name IncidentStatusEnum.Closed$
$ChangeRequestStatusInProgress = Get-SCSMEnumeration -Name ChangeStatusEnum.InProgress$
$ChangeRequestStatusClosed = Get-SCSMEnumeration -Name ChangeStatusEnum.Closed$
$ProblemStatusActive = Get-SCSMEnumeration -Name ProblemStatusEnum.Active$
$ProblemStatusClosed = Get-SCSMEnumeration -Name ProblemStatusEnum.Closed$
$ActivityStatusInProgress = Get-SCSMEnumeration -Name ActivityStatusEnum.Active$
$ActivityStatusCompleted = Get-SCSMEnumeration -Name ActivityStatusEnum.Completed$
if($Verbose)
{
    Write-Host "Got $IncidentStatusActive"
    Write-Host "Got $IncidentStatusPending"
    Write-Host "Got $IncidentStatusResolved"
    Write-Host "Got $IncidentStatusClosed"
    Write-Host "Got $ChangeRequestStatusInProgress"
    Write-Host "Got $ChangeRequestStatusClosed"
    Write-Host "Got $ProblemStatusActive"
    Write-Host "Got $ProblemStatusClosed"
    Write-Host "Got $ActivityStatusInProgress"
    Write-Host "Got $ActivityStatusCompleted"
}

#Get the GUID IDs of those enums.  We'll need them later in view criteria
$IncidentStatusActiveId = $IncidentStatusActive.Id
$IncidentStatusPendingId = $IncidentStatusPending.Id
$IncidentStatusResolvedId = $IncidentStatusResolved.Id
$IncidentStatusClosedId = $IncidentStatusClosed.Id
$ChangeRequestStatusInProgressId = $ChangeRequestStatusInProgress.Id
$ChangeRequestStatusClosedId = $ChangeRequestStatusClosed.Id
$ProblemStatusActiveId = $ProblemStatusActive.Id
$ProblemStatusClosedId = $ProblemStatusClosed.Id
$ActivityStatusInProgressId = $ActivityStatusInProgress.Id
$ActivityStatusCompletedId = $ActivityStatusCompleted.Id

#Get-Classes
$IncidentClass = Get-SCSMClass -Name System.WorkItem.Incident$
$ChangeRequestClass = Get-SCSMClass -Name System.WorkItem.ChangeRequest$
$ProblemClass = Get-SCSMClass -Name System.WorkItem.Problem$
$ManualActivityClass = Get-SCSMClass -Name System.WorkItem.Activity.ManualActivity$
$ReviewActivityClass = Get-SCSMClass -Name System.WorkItem.Activity.ReviewActivity$
if($Verbose)
{
    Write-Host "Got $IncidentClass"
    Write-Host "Got $ChangeRequestClass"
    Write-Host "Got $ProblemClass"
    Write-Host "Got $ManualActivityClass"
    Write-Host "Got $ReviewActivityClass"
}

#Get User Role Profiles
$IncidentResolverUserRoleProfile = Get-SCSMUserRoleProfile -Name IncidentResolver
$ChangeManagerUserRoleProfile = Get-SCSMUserRoleProfile -Name ChangeManager
$ProblemAnalystUserRoleProfile = Get-SCSMUserRoleProfile -Name ProblemAnalyst
if($Verbose)
{
    Write-Host "Got $IncidentResolverUserRoleProfile"
    Write-Host "Got $ChangeManagerUserRoleProfile"
    Write-Host "Got $ProblemAnalystUserRoleProfile"
}

#Columns for Views
$IDColumn = New-SCSMColumn -Name "`$Id`$" -DisplayName "ID" -BindingPath "`$Id`$" -Width "100" -DataType "s:Guid" -PassThru
$TitleColumn = New-SCSMColumn -Name "Title" -DisplayName "Title" -BindingPath "Title" -Width "100" -DataType "s:String" -PassThru
$StatusColumn = New-SCSMColumn -Name "Status.DisplayName" -DisplayName "Status" -BindingPath "Status.DisplayName" -Width "100" -DataType "s:String" -PassThru
$AssignedToColumn = New-SCSMColumn -Name "AssignedUser.DisplayName" -DisplayName "Assigned To" -BindingPath "AssignedUser.DisplayName" -DataType "s:String" -Width "100" -PassThru
$AffectedUserColumn = New-SCSMColumn -Name "AffectedUser.DisplayName" -DisplayName "Affected User" -BindingPath "AffectedUser.DisplayName" -Width "100" -DataType "s:String" -PassThru
$CreatedDateColumn = New-SCSMColumn -Name "CreatedDate" -DisplayName "Created Date" -BindingPath "CreatedDate" -Width "100" -DataType "s:DateTime" -PassThru
$ResolveByColumn = New-SCSMColumn -Name "TargetResolutionTime" -DisplayName "Resolve By" -BindingPath "TargetResolutionTime" -Width "100" -DataType "s:DateTime" -PassThru
$PriorityColumn = New-SCSMColumn -Name "Priority" -DisplayName "Priority" -BindingPath "Priority" -Width "100" -DataType "s:Int32" -PassThru
$LastModifiedColumn = New-SCSMColumn -Name "`$LastModified`$" -DisplayName "Last Modified" -BindingPath "`$LastModified`$" -Width "100" -DataType "s:DateTime" -PassThru
$ClassificationColumn = New-SCSMColumn -Name "Classification.DisplayName" -DisplayName "Classification Category" -BindingPath "Classification.DisplayName" -Width "100" -DataType "s:String" -PassThru
#TODO: Add support for WP specific columns:
#WPOffice_Building - Building
#WPTechnical_Category - Technical Category
#WPProj_Num - Project Number

#TODO: Get columns for the change, problem, and activity views from WP
$IncidentColumns = @($IDColumn; 
                        $AssignedToColumn; 
                        $TitleColumn;
                        $AffectedUserColumn; 
                        $CreatedDateColumn;
                        $ResolveByColumn; 
                        $PriorityColumn
                        $LastModifiedColumn
                        $ClassificationColumn
                        )
$IncidentColumns

$ChangeRequestColumns = @($IDColumn; $TitleColumn; $StatusColumn)
$ProblemColumns = @($IDColumn; $TitleColumn; $StatusColumn)
$ManualActivityColumns = @($IDColumn; $TitleColumn; $StatusColumn)
$ReviewActivityColumns = @($IDColumn; $TitleColumn; $StatusColumn)
if($Verbose)
{
    Write-Host "Got Incident View Columns"
    Write-Host "Got Change Request View Columns"
    Write-Host "Got Problem Columns"
    Write-Host "Got Manual Activity Columns"
}

#Images for Views
$IncidentViewActiveImage = Get-SCSMImage -Name IncidentMgmt_AllActiveIncidents_16.png -ListOnly
$IncidentViewPendingImage = Get-SCSMImage -Name IncidentMgmt_AllActiveIncidents_16.png -ListOnly
$IncidentViewUserRespondedImage = Get-SCSMImage -Name IncidentMgmt_AllOpenEmailIncidents_16.png -ListOnly
$IncidentViewResolvedImage = Get-SCSMImage -Name IncidentMgmt_AllActiveIncidents_16.png -ListOnly
$IncidentViewClosedImage = Get-SCSMImage -Name IncidentMgmt_AllActiveIncidents_16.png -ListOnly
$ChangeRequestViewInProgresImage = Get-SCSMImage -Name ChangeMgmt_AllChangeRequests_16.png -ListOnly
$ChangeRequestViewClosedImage = Get-SCSMImage -Name ChangeMgmt_ChangeRequestClosed_16.png -ListOnly
$ProblemViewActiveImage = Get-SCSMImage -Name ProblemMgmt_ActiveProblems_16.png -ListOnly
$ProblemViewClosedImage = Get-SCSMImage -Name ProblemMgmt_ClosedProblems_16.png -ListOnly
$ManualActivityInProgressViewImage = Get-SCSMImage -Name ActivityMgmt_AllManualActivities_16.png -ListOnly
$ManualActivityCompletedViewImage = Get-SCSMImage -Name ActivityMgmt_CompletedManualActivity_16.png -ListOnly
$ReviewActivityInProgressViewImage = Get-SCSMImage -Name ActivityMgmt_ActiveReviewActivities_16.png -ListOnly
$ReviewActivityCompletedViewImage = Get-SCSMImage -Name ActivityMgmt_ApprovedReviewActivities_16.png -ListOnly
if($Verbose)
{
    Write-Host "Got Image $IncidentViewActiveImage"
    Write-Host "Got Image $IncidentViewUserRespondedImage"
    Write-Host "Got Image $IncidentViewResolvedImage"
    Write-Host "Got Image $IncidentViewClosedImage"
    Write-Host "Got Image $ChangeRequestViewInProgresImage"
    Write-Host "Got Image $ChangeRequestViewClosedImage"
    Write-Host "Got Image $ProblemViewActiveImage"
    Write-Host "Got Image $ProblemViewClosedImage"
    Write-Host "Got Image $ManualActivityInProgressViewImage"
    Write-Host "Got Image $ManualActivityCompletedViewImage"
    Write-Host "Got Image $ReviewActivityInProgressViewImage"
    Write-Host "Got Image $ReviewActivityCompletedViewImage"
}

#TypeProjections
#*****  NOTE: This type projection is specific to WP
$IncidentTypeProjection = Get-SCSMTypeProjection -Name WP.WorkItem.Incident.View.ProjectionType -NoAdapt
$ManualActivityTypeProjection = Get-SCSMTypeProjection -Name System.WorkItem.Activity.ManualActivityProjection -NoAdapt

if($Verbose)
{
    Write-Host "Got Manual Activity Type Projection"
    Write-Host "Got Incident Type Projection"
}

#Get a notification template
#TODO: Need to specify which templates here.  Just using test templates for now.
$IncidentNotificationTemplate = Get-SCSMObjectTemplate -ID "50DC32C2-6517-E9EA-DD99-0455383CAB17"
$ChangeRequestNotificationTemplate = Get-SCSMObjectTemplate -ID "D1142E92-3221-3B6F-69DB-5067E1CFC29A"
$ProblemNotificationTemplate = Get-SCSMObjectTemplate -ID "007163B7-8966-AC5D-9F74-1C2B182C0BB1"

#Common Tasks
$MicrosoftEnterpriseManagementServiceManagerUIAuthoringGenericEditWI = Get-SCSMConsoleTask -Name Microsoft.EnterpriseManagement.ServiceManager.UI.Authoring.GenericEditWI
$MicrosoftEnterpriseManagementServiceManagerUIConsoleTaskEditGridView = Get-SCSMConsoleTask -Name Microsoft.EnterpriseManagement.ServiceManager.UI.Console.Task.EditGridView
$SystemKnowledgeArticleLinkToTask = Get-SCSMConsoleTask -Name System.Knowledge.Article.LinkTo.Task

#Incident Tasks
$SystemWorkItemIncidentActivateIncidentCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.ActivateIncidentCommand.Task
$SystemWorkItemIncidentAssignCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.AssignCommand.Task
$SystemWorkItemIncidentAssignToMeCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.AssignToMeCommand.Task
$SystemWorkItemIncidentEscalateIncidentCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.EscalateIncidentCommand.Task
$SystemWorkItemIncidentRequestUserInputCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.RequestUserInputCommand.Task
$SystemWorkItemIncidentResolveIncidentCommandTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.ResolveIncidentCommand.Task
$MicrosoftEnterpriseManagementServiceManagerUIConsoleTaskRefresh = Get-SCSMConsoleTask -Name Microsoft.EnterpriseManagement.ServiceManager.UI.Console.Task.Refresh
$SystemWorkItemIncidentNewTask = Get-SCSMConsoleTask -Name System.WorkItem.Incident.New.Task

#Change Request Tasks
$CloseChangeRequest = Get-SCSMConsoleTask -Name CloseChangeRequest 
$ChangeRequestConfigureTask = Get-SCSMConsoleTask -Name ChangeRequestConfigureTask 
$ResumeChangeRequest = Get-SCSMConsoleTask -Name ResumeChangeRequest 
$CreateChangeRequest = Get-SCSMConsoleTask -Name CreateChangeRequest 
$PutChangeRequestOnHold = Get-SCSMConsoleTask -Name PutChangeRequestOnHold 
$CancelChangeRequest = Get-SCSMConsoleTask -Name CancelChangeRequest 
$CreateChangeRequestforConfigItem = Get-SCSMConsoleTask -Name CreateChangeRequestforConfigItem
$ReturnToActivity = Get-SCSMConsoleTask -Name ReturnToActivity
$FailManualActivity = Get-SCSMConsoleTask -Name FailManualActivity
$CompleteManualActivity = Get-SCSMConsoleTask -Name CompleteManualActivity

#Problem Activities
$ServiceManagerProblemManagementLibraryTaskClose = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.Close
$ServiceManagerProblemManagementLibraryTaskResolve = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.Resolve
$ServiceManagerProblemManagementLibraryTaskReactivate = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.Reactivate
$ServiceManagerProblemManagementLibraryTaskLinkProblem = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.LinkProblem
$ServiceManagerProblemManagementLibraryTaskChangeStatus = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.ChangeStatus
$ServiceManagerProblemManagementLibraryTaskCreate = Get-SCSMConsoleTask -Name ServiceManager.ProblemManagement.Library.Task.Create


if($Verbose)
{
    Write-Host "Got $SystemWorkItemIncidentActivateIncidentCommandTask"
    Write-Host "Got $SystemWorkItemIncidentAssignCommandTask"
    Write-Host "Got $SystemWorkItemIncidentAssignToMeCommandTask"
    Write-Host "Got $MicrosoftEnterpriseManagementServiceManagerUIAuthoringGenericEditWI"
    Write-Host "Got $SystemWorkItemIncidentEscalateIncidentCommandTask"
    Write-Host "Got $SystemWorkItemIncidentRequestUserInputCommandTask"
    Write-Host "Got $SystemWorkItemIncidentResolveIncidentCommandTask"
    Write-Host "Got $MicrosoftEnterpriseManagementServiceManagerUIConsoleTaskRefresh"
    Write-Host "Got $SystemWorkItemIncidentNewTask"
    Write-Host "Got $CloseChangeRequest"
    Write-Host "Got $ChangeRequestConfigureTask"
    Write-Host "Got $ResumeChangeRequest"
    Write-Host "Got $CreateChangeRequest"
    Write-Host "Got $PutChangeRequestOnHold"
    Write-Host "Got $CancelChangeRequest"
    Write-Host "Got $CreateChangeRequestforConfigItem"
    Write-Host "Got $ReturnToActivity"
    Write-Host "Got $FailManualActivity"
    Write-Host "Got $CompleteManualActivity"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskClose"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskResolve"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskReactivate"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskLinkProblem"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskChangeStatus"
    Write-Host "Got $ServiceManagerProblemManagementLibraryTaskCreate"
}

$IncidentConsoleTasks = @($SystemWorkItemIncidentActivateIncidentCommandTask; $SystemWorkItemIncidentAssignCommandTask; $SystemWorkItemIncidentAssignToMeCommandTask; 
                            $MicrosoftEnterpriseManagementServiceManagerUIAuthoringGenericEditWI; $SystemWorkItemIncidentEscalateIncidentCommandTask; 
                            $SystemWorkItemIncidentRequestUserInputCommandTask; $SystemWorkItemIncidentResolveIncidentCommandTask; 
                            $MicrosoftEnterpriseManagementServiceManagerUIConsoleTaskRefresh; $SystemWorkItemIncidentNewTask; $MicrosoftEnterpriseManagementServiceManagerUIConsoleTaskEditGridView,
                            $SystemKnowledgeArticleLinkToTask)

$ChangeRequestConsoleTasks = @($CloseChangeRequest; $ChangeRequestConfigureTask; $ResumeChangeRequest; $CreateChangeRequest, $PutChangeRequestOnHold, $CancelChangeRequest,
                                $CreateChangeRequestforConfigItem; $ReturnToActivity; $FailManualActivity; $CompleteManualActivity)

$ProblemConsoleTasks = @($ServiceManagerProblemManagementLibraryTaskClose; $ServiceManagerProblemManagementLibraryTaskResolve; $ServiceManagerProblemManagementLibraryTaskReactivate;
                            $ServiceManagerProblemManagementLibraryTaskLinkProblem; $ServiceManagerProblemManagementLibraryTaskChangeStatus; $ServiceManagerProblemManagementLibraryTaskCreate)


#Get Activity Views
$ActivityManagementViewsManualActivitiesActive = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesActive
$ActivityManagementViewsReviewActivitiesActive = Get-SCSMView -Name ActivityManagement.Views.ReviewActivitiesActive
$ActivityManagementViewsManualActivitiesUnassigned = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesUnassigned
$ActivityManagementViewsReviewActivitiesAssignedToMe = Get-SCSMView -Name ActivityManagement.Views.ReviewActivitiesAssignedToMe
$ActivityManagementViewsAllManualActivities = Get-SCSMView -Name ActivityManagement.Views.AllManualActivities
$ActivityManagementViewsAllReviewActivities = Get-SCSMView -Name ActivityManagement.Views.AllReviewActivities
$ActivityManagementViewsManualActivitiesCancelled = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesCancelled
$ActivityManagementViewsReviewActivitiesCancelled = Get-SCSMView -Name ActivityManagement.Views.ReviewActivitiesCancelled
$ActivityManagementViewsManualActivitiesCompleted = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesCompleted
$ActivityManagementViewsReviewActivitiesRejected = Get-SCSMView -Name ActivityManagement.Views.ReviewActivitiesRejected
$ActivityManagementViewsManualActivitiesFailed = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesFailed
$ActivityManagementViewsManualActivitiesAssignedToMe = Get-SCSMView -Name ActivityManagement.Views.ManualActivitiesAssignedToMe
$ActivityManagementViewsReviewActivitiesApproved = Get-SCSMView -Name ActivityManagement.Views.ReviewActivitiesApproved

#Get Change Request Views
$ChangeManagementViewsChangeRequestsClosed = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsClosed
$ChangeManagementViewsChangeRequestsCancelled = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsCancelled
$ChangeManagementViewsChangeRequestsRejected = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsRejected
$ChangeManagementViewsAllChangeRequests = Get-SCSMView -Name ChangeManagement.Views.AllChangeRequests
$ChangeManagementViewsChangeRequestsAssignedToMe = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsAssignedToMe
$ChangeManagementViewsChangeRequestsInReview = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsInReview
$ChangeManagementViewsChangeRequestsOnHold = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsOnHold
$ChangeManagementViewsChangeRequestsFailed = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsFailed
$ChangeManagementViewsChangeRequestsCompleted = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsCompleted
$ChangeManagementViewsChangeRequestsManualActivityInProgress = Get-SCSMView -Name ChangeManagement.Views.ChangeRequestsManualActivityInProgress

#Get Incident Views
$SystemWorkItemIncidentAllIncidentsView = Get-SCSMView -Name System.WorkItem.Incident.AllIncidents.View
$SystemWorkItemIncidentDCMView = Get-SCSMView -Name System.WorkItem.Incident.DCM.View
$SystemWorkItemIncidentEmailView = Get-SCSMView -Name System.WorkItem.Incident.Email.View
$SystemWorkItemIncidentActiveView = Get-SCSMView -Name System.WorkItem.Incident.Active.View
$SystemWorkItemIncidentSCOMView = Get-SCSMView -Name System.WorkItem.Incident.SCOM.View
$SystemWorkItemIncidentPortalView = Get-SCSMView -Name System.WorkItem.Incident.Portal.View
$SystemWorkItemIncidentActiveUnassignedView = Get-SCSMView -Name System.WorkItem.Incident.Active.Unassigned.View
$SystemWorkItemIncidentEscalatedView = Get-SCSMView -Name System.WorkItem.Incident.Escalated.View
$SystemWorkItemIncidentAssignedToMeView = Get-SCSMView -Name System.WorkItem.Incident.AssignedToMe.View
$SystemWorkItemIncidentOverDueView = Get-SCSMView -Name System.WorkItem.Incident.OverDue.View
$SystemWorkItemIncidentPendingView = Get-SCSMView -Name System.WorkItem.Incident.Pending.View

#Get Problem Views
$ServiceManagerProblemManagementConfigurationViewActiveKnownErrors = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.ActiveKnownErrors
$ServiceManagerProblemManagementConfigurationViewActiveProblem = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.ActiveProblem
$ServiceManagerProblemManagementConfigurationViewClosed = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.Closed
$ServiceManagerProblemManagementConfigurationViewAssignedToMe = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.AssignedToMe
$ServiceManagerProblemManagementConfigurationViewNeedingReview = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.NeedingReview
$ServiceManagerProblemManagementConfigurationViewResolved = Get-SCSMView -Name ServiceManager.ProblemManagement.Configuration.View.Resolved

if($Verbose)
{
    Write-Host "Got $ActivityManagementViewsManualActivitiesActive"
    Write-Host "Got $ActivityManagementViewsReviewActivitiesActive"
    Write-Host "Got $ActivityManagementViewsManualActivitiesUnassigned"
    Write-Host "Got $ActivityManagementViewsReviewActivitiesAssignedToMe"
    Write-Host "Got $ActivityManagementViewsAllManualActivities"
    Write-Host "Got $ActivityManagementViewsAllReviewActivities"
    Write-Host "Got $ActivityManagementViewsManualActivitiesCancelled"
    Write-Host "Got $ActivityManagementViewsReviewActivitiesCancelled"
    Write-Host "Got $ActivityManagementViewsManualActivitiesCompleted"
    Write-Host "Got $ActivityManagementViewsReviewActivitiesRejected"
    Write-Host "Got $ActivityManagementViewsManualActivitiesFailed"
    Write-Host "Got $ActivityManagementViewsManualActivitiesAssignedToMe"
    Write-Host "Got $ActivityManagementViewsReviewActivitiesApproved"
    Write-Host "Got $ChangeManagementViewsChangeRequestsClosed"
    Write-Host "Got $ChangeManagementViewsChangeRequestsCancelled"
    Write-Host "Got $ChangeManagementViewsChangeRequestsRejected"
    Write-Host "Got $ChangeManagementViewsAllChangeRequests"
    Write-Host "Got $ChangeManagementViewsChangeRequestsAssignedToMe"
    Write-Host "Got $ChangeManagementViewsChangeRequestsInReview"
    Write-Host "Got $ChangeManagementViewsChangeRequestsOnHold"
    Write-Host "Got $ChangeManagementViewsChangeRequestsFailed"
    Write-Host "Got $ChangeManagementViewsChangeRequestsCompleted"
    Write-Host "Got $ChangeManagementViewsChangeRequestsManualActivityInProgress"
    Write-Host "Got $SystemWorkItemIncidentAllIncidentsView"
    Write-Host "Got $SystemWorkItemIncidentDCMView"
    Write-Host "Got $SystemWorkItemIncidentEmailView"
    Write-Host "Got $SystemWorkItemIncidentActiveView"
    Write-Host "Got $SystemWorkItemIncidentSCOMView"
    Write-Host "Got $SystemWorkItemIncidentPortalView"
    Write-Host "Got $SystemWorkItemIncidentActiveUnassignedView"
    Write-Host "Got $SystemWorkItemIncidentEscalatedView"
    Write-Host "Got $SystemWorkItemIncidentAssignedToMeView"
    Write-Host "Got $SystemWorkItemIncidentOverDueView"
    Write-Host "Got $SystemWorkItemIncidentPendingView"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewActiveKnownErrors"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewActiveProblem"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewClosed"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewAssignedToMe"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewNeedingReview"
    Write-Host "Got $ServiceManagerProblemManagementConfigurationViewResolved"
}

$SupportGroupParentEnum = Get-SCSMEnumeration -Name $parentenumname


#Add Management Pack References
#$SystemWorkItemInicdentLibraryManagementPackReference = Get-SCManagementPack -Name System.WorkItem.Incident.Library$ | New-SCSMManagementPackReference -Alias "System_WorkItem_Incident_Library"
#$SystemWorkItemLibraryMangementPackReference = Get-SCManagementPack -Name System.WorkItem.Library$ | New-SCSMManagementPackReference -Alias "System_WorkItem_Library"

#$IncidentViewManagementPackReferences = @()

if($SupportGroups.Count -gt 0)
{
    $count = $SupportGroups.Count
}
else
{
    $count = 1
}

$i = 0

foreach($SupportGroupDisplayName in $SupportGroups)
{
    $i++
    
    Write-Progress -Activity "Creating support group configuration..." -Status "Working on $SupportGroupDisplayName ($i of $count)" -PercentComplete (($i/$count)*100)
    
    $SupportGroupName = MakeMPElementIDSafeName $SupportGroupDisplayName $EnumElement
    $SupportGroupManagementPackDisplayName = $SupportGroupDisplayName
    $SupportGroupManagementPackName = MakeMPElementIDSafeName $SupportGroupManagementPackDisplayName $ManagementPack
    $SupportGroupQueueIncidentDisplayName = $SupportGroupDisplayName + $IncidentSuffix
    $SupportGroupQueueIncidentName = MakeMPElementIDSafeName $SupportGroupQueueIncidentDisplayName $QueueElement
    $SupportGroupQueueChangeRequestDisplayName = $SupportGroupDisplayName + $ChangeRequestSuffix
    $SupportGroupQueueChangeRequestName = MakeMPElementIDSafeName $SupportGroupQueueChangeRequestDisplayName $QueueElement
    $SupportGroupQueueProblemDisplayName = $SupportGroupDisplayName + $ProblemSuffix
    $SupportGroupQueueProblemName = MakeMPElementIDSafeName $SupportGroupQueueProblemDisplayName $QueueElement
    $SupportGroupIncidentResolverUserRoleDisplayName = $SupportGroupDisplayName + $IncidentSuffix + $UserRoleIncidentResolverNameSuffix
    $SupportGroupChangeRequestManagerUserRoleDisplayName = $SupportGroupDisplayName + $ChangeRequestSuffix + $UserRoleChangeRequestManagerNameSuffix
    $SupportGroupProblemAnalystUserRoleDisplayName = $SupportGroupDisplayName + $ProblemSuffix + $UserRoleProblemAnalystNameSuffix
    $SupportGroupADGroupName = $ADUserGroupNamePrefix + $SupportGroupDisplayName
    $SupportGroupADGroupNameNotificationRecipient = $ADUserGroupNameNotificationRecipientPrefix + $SupportGroupDisplayName + $ADUserGroupNameNotificationRecipientSuffix
    #Notification Subscription Display Names
    $NotificationSubscriptionIncidentCreateDisplayName = $SupportGroupDisplayName + $IncidentSuffix + $NotificationSubscriptionCreateSuffix
    $NotificationSubscriptionIncidentUpdateDisplayName = $SupportGroupDisplayName + $IncidentSuffix + $NotificationSubscriptionUpdateSuffix
    $NotificationSubscriptionChangeRequestCreateDisplayName = $SupportGroupDisplayName + $ChangeRequestSuffix + $NotificationSubscriptionCreateSuffix
    $NotificationSubscriptionChangeRequestUpdateDisplayName = $SupportGroupDisplayName + $ChangeRequestSuffix + $NotificationSubscriptionUpdateSuffix
    $NotificationSubscriptionProblemCreateDisplayName = $SupportGroupDisplayName + $ProblemSuffix + $NotificationSubscriptionCreateSuffix
    $NotificationSubscriptionProblemUpdateDisplayName = $SupportGroupDisplayName + $ProblemSuffix + $NotificationSubscriptionUpdateSuffix
    
    if($Verbose)
    {
        Write-Host "Support Group Display Name = $SupportGroupDisplayName"
        Write-Host "Support Group Internal Name = $SupportGroupName"
        Write-Host "Management Pack Display Name = $SupportGroupManagementPackDisplayName"
        Write-Host "Management Pack Internal Name = $SupportGroupManagementPackName"
        Write-Host "Incident Queue Display Name = $SupportGroupQueueIncidentDisplayName"
        Write-Host "Incident Queue Internal Name = $SupportGroupQueueIncidentName"
        Write-Host "Change Request Queue Display Name = $SupportGroupQueueChangeRequestDisplayName"
        Write-Host "Change Request Queue Internal Name = $SupportGroupQueueChangeRequestName"
        Write-Host "Problem Queue Display Name = $SupportGroupQueueProblemDisplayName"
        Write-Host "Problem Queue Internal Name = $SupportGroupQueueProblemName"
        Write-Host "Incident Resolver User Role Display Name = $SupportGroupIncidentResolverUserRoleDisplayName"
        Write-Host "Change Request Manager User Role Display Name = $SupportGroupChangeRequestManagerUserRoleDisplayName"
        Write-Host "Problem Analyst User Role Display Name = $SupportGroupProblemAnalystUserRoleDisplayName"       
        Write-Host "Support Group AD Username = $SupportGroupADGroupName"
        Write-Host "Notifiation Recipient AD Username = $SupportGroupADGroupNameNotificationRecipient"
        Write-Host "Notification Subscription Incident Create Display Name = $NotificationSubscriptionIncidentCreateDisplayName"
        Write-Host "Notification Subscription Incident Update Display Name = $NotificationSubscriptionIncidentUpdateDisplayName"
        Write-Host "Notification Subscription Change Request Create Display Name = $NotificationSubscriptionChangeRequestCreateDisplayName"
        Write-Host "Notification Subscription Change Request Update Display Name = $NotificationSubscriptionChangeRequestUpdateDisplayName"
        Write-Host "Notification Subscription Problem Create Display Name = $NotificationSubscriptionProblemCreateDisplayName"
        Write-Host "Notification Subscription Problem Update Display Name = $NotificationSubscriptionProblemUpdateDisplayName"
    }

    $SupportGroupADUserGroupObject = Get-SCSMObject -Class (Get-SCSMClass -Name System.Domain.User) -Filter "UserName = $SupportGroupADGroupName"
    if($Verbose){"Got $SupportGroupADUserGroupObject"}
    
    $NotificationRecipientADUserGroupObject = Get-SCSMObject -Class (Get-SCSMClass  -Name System.Domain.User) -Filter "UserName = $SupportGroupADGroupNameNotificationRecipient"
    if($Verbose){"Got $NotificationRecipientADUserGroupOBject"}
    
    $SupportGroupManagementPack = New-SCManagementPack -ManagementPackName $SupportGroupManagementPackName -FriendlyName $SupportGroupManagementPackDisplayName -PassThru
    if($Verbose){"Created $SupportGroupManagementPack"}

    #Create Enums
    $SupportGroupEnumeration = Add-SCSMEnumeration -Parent $SupportGroupParentEnum -Name $SupportGroupName -DisplayName $SupportGroupDisplayName -ManagementPack $SupportGroupManagementPack -Ordinal $i -PassThru
    $SupportGroupEnumerationId = $SupportGroupEnumeration.Id
    if($Verbose){"Created $SupportGroupEnumeration"}
    
    #Create Queues
    
    #Incident Queue
    $IncidentQueueClass = New-SCQueue -Name $SupportGroupQueueIncidentDisplayName -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -Filter "TierQueue =  '$SupportGroupEnumerationId'" -PassThru
    $IncidentQueue = Get-SCSMObject -Class $IncidentQueueClass
    if($Verbose){"Created Incident Queue"}

    #Change Request Queue
    $ChangeRequestQueueClass = New-SCQueue -Name $SupportGroupQueueChangeRequestDisplayName -ManagementPack $SupportGroupManagementPack -Class $ChangeRequestClass -Filter "SupportGroup =  '$SupportGroupEnumerationId'" -PassThru
    $ChangeRequestQueue = Get-SCSMObject -Class $ChangeRequestQueueClass
    if($Verbose){"Created Change Request Queue"}
    
    #Problem Queue
    $ProblemQueueClass = New-SCQueue -Name $SupportGroupQueueProblemDisplayName -ManagementPack $SupportGroupManagementPack -Class $ProblemClass -Filter "SupportGroup =  '$SupportGroupEnumerationId'" -PassThru
    $ProblemQueue = Get-SCSMObject -Class $ProblemQueueClass
    if($Verbose){"Created Problem Queue"}
    
    #Create Views
    
    #Incidents where Status = “Active” and assigned to the support group
    $IncidentStatusActiveViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusActiveId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"
    
$IncidentStatusActiveView = New-SCSMView -PassThru -Folder $IncidentManagementFolder -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -DisplayName "$SupportGroupDisplayName Active Incidents" -Columns $IncidentColumns -Criteria $IncidentStatusActiveViewCriteria -Image $IncidentViewActiveImage -Projection $IncidentTypeProjection
if($Verbose){"Created Incident Status Active View"}

#Incidents where Status = “Pending” and assigned to the support group
$IncidentStatusPendingViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusPendingId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$IncidentStatusPendingView = New-SCSMView -PassThru -Folder $IncidentManagementFolder -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -DisplayName "$SupportGroupDisplayName Pending Incidents" -Columns $IncidentColumns -Criteria $IncidentStatusPendingViewCriteria -Image $IncidentViewPendingImage -Projection $IncidentTypeProjection
if($Verbose){"Created Incident Status Pending View"}

    #Incidents where Status = “User responded by email” and assigned to the support group
    #TODO: Need the GUID for 'User responded by email"
    
    #Incidents where Status = “Resolved” and assigned to the support group
$IncidentStatusResolvedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusResolvedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$IncidentStatusResolvedView = New-SCSMView -PassThru -Folder $IncidentManagementFolder -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -DisplayName "$SupportGroupDisplayName Resolved Incidents" -Columns $IncidentColumns -Criteria $IncidentStatusResolvedViewCriteria -Image $IncidentViewResolvedImage -Projection $IncidentTypeProjection
if($Verbose){"Created Incident Status Resolved View"}

#Incidents where Status = “Closed” and assigned to the support group  
$IncidentStatusClosedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusResolvedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$IncidentStatusClosedView = New-SCSMView -PassThru -Folder $IncidentManagementFolder -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -DisplayName "$SupportGroupDisplayName Closed Incidents" -Columns $IncidentColumns -Criteria $IncidentStatusClosedViewCriteria -Image $IncidentViewClosedImage -Projection $IncidentTypeProjection
if($Verbose){"Created Incident Status Closed View"}

    #Active Incidents where Status <> Resolved AND Status <> Closed AND assigned to is null AND Support group = the support group
$ActiveIncidentUnassignedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <UnaryExpression>
                    <ValueExpression>
                      <GenericProperty Path=`"`$Context/Path[Relationship='System_WorkItem_Library!System.WorkItemAssignedToUser']`$`">Id</GenericProperty>
                    </ValueExpression>
                    <Operator>IsNull</Operator>
                  </UnaryExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>NotEqual</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusResolvedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>NotEqual</Operator>
                    <ValueExpressionRight>
                      <Value>{$IncidentStatusClosedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$IncidentActiveUnassignedView = New-SCSMView -PassThru -Folder $IncidentManagementFolder -ManagementPack $SupportGroupManagementPack -Class $IncidentClass -DisplayName "$SupportGroupDisplayName Unassigned Active Incidents" -Columns $IncidentColumns -Criteria $ActiveIncidentUnassignedViewCriteria -Image $IncidentViewActiveImage -Projection $IncidentTypeProjection
if($Verbose){"Created Incident Active Unassigned View"}



    #Change Requests where Status = In Progress and assigned to the support group
    #**** NOTE ***** This criteria assumes that there is an extended property on the change request class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ChangeRequestStatusInProgressViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_ChangeRequest_Library!System.WorkItem.ChangeRequest']/SupportGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                 </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_ChangeRequest_Library!System.WorkItem.ChangeRequest']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ChangeRequestStatusInProgressId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ChangeRequestStatusInProgressView = New-SCSMView -PassThru -Folder $ChangeManagementFolder -ManagementPack $SupportGroupManagementPack -Class $ChangeRequestClass -DisplayName "$SupportGroupDisplayName In Progress Change Requests" -Columns $ChangeRequestColumns -Criteria $ChangeRequestStatusInProgressViewCriteria -Image $ChangeRequestViewInProgresImage 
if($Verbose){"Created Change Request Status In Progress View"}

    #Change Requests where Status = Closed and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the change request class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ChangeRequestStatusClosedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_ChangeRequest_Library!System.WorkItem.ChangeRequest']/SupportGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_ChangeRequest_Library!System.WorkItem.ChangeRequest']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ChangeRequestStatusInProgressId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ChangeRequestStatusClosedView = New-SCSMView -PassThru -Folder $ChangeManagementFolder -ManagementPack $SupportGroupManagementPack -Class $ChangeRequestClass -DisplayName "$SupportGroupDisplayName Closed Change Requests" -Columns $ChangeRequestColumns -Criteria $ChangeRequestStatusClosedViewCriteria -Image $ChangeRequestViewClosedImage
if($Verbose){"Created Change Request Status Closed View"}

    #Problems where Status = Active and assigned to the support group
    #**** NOTE ***** This criteria assumes that there is an extended property on the problem class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ProblemStatusActiveViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Problem_Library!System.WorkItem.Problem']/SupportGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Problem_Library!System.WorkItem.Problem']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ProblemStatusActiveId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ProblemStatusActiveView = New-SCSMView -PassThru -Folder $ProblemManagementFolder -ManagementPack $SupportGroupManagementPack -Class $ProblemClass -DisplayName "$SupportGroupDisplayName Active Problems" -Columns $ProblemColumns -Criteria $ProblemStatusActiveViewCriteria -Image $ProblemViewActiveImage
if($Verbose){"Created Problem Status Active View"}

    #Problems where Status = Closed and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the problem class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ProblemStatusClosedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Problem_Library!System.WorkItem.Problem']/SupportGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Problem_Library!System.WorkItem.Problem']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ProblemStatusClosedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ProblemStatusClosedView = New-SCSMView -PassThru -Folder $ProblemManagementFolder -ManagementPack $SupportGroupManagementPack -Class $ProblemClass -DisplayName "$SupportGroupDisplayName Closed Problems" -Columns $ProblemColumns -Criteria $ProblemStatusClosedViewCriteria -Image $ProblemViewClosedImage
if($Verbose){"Created Problem Status Closed View"}

    #Manual Activities where Status = In Progress and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the manual activity class with the name 'ResolverGroup' that is bound to IncidentTierQueuesEnum
$ManualActivityInProgressViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                   <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/ResolverGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
               <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ActivityStatusInProgressId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
         </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ManualActivityStatusInProgressView = New-SCSMView -PassThru -Folder $ActivitiesFolder -ManagementPack $SupportGroupManagementPack -Class $ManualActivityClass -DisplayName "$SupportGroupDisplayName In Progress Manual Activities" -Columns $ManualActivityColumns -Criteria $ManualActivityInProgressViewCriteria -Image $ManualActivityInProgressViewImage
if($Verbose){"Created Manual Activity Status In Progress View"}

    #Manual Activities where Status = In Progress and assigned to is null
    #**** NOTE ***** This criteria assumes that there is an extended property on the manual activity class with the name 'ResolverGroup' that is bound to IncidentTierQueuesEnum
$ManualActivityInProgressUnassignedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/ResolverGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <UnaryExpression>
                    <ValueExpression>
                      <GenericProperty Path=`"`$Context/Path[Relationship='System_WorkItem_Library!System.WorkItemAssignedToUser']`$`">Id</GenericProperty>
                    </ValueExpression>
                    <Operator>IsNull</Operator>
                  </UnaryExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ActivityStatusInProgressId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ManualActivityStatusInProgressUnassignedView = New-SCSMView -PassThru -Folder $ActivitiesFolder -ManagementPack $SupportGroupManagementPack -Class $ManualActivityClass -DisplayName "$SupportGroupDisplayName Unassigned In Progress Manual Activities" -Columns $ManualActivityColumns -Criteria $ManualActivityInProgressUnassignedViewCriteria -Image $ManualActivityInProgressViewImage -Projection $ManualActivityTypeProjection
if($Verbose){"Created Manual Activity Status In Progress Unassigned View"}
    
    #Manual Activities where Status = Closed and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the manual activity class with the name 'ResolverGroup' that is bound to IncidentTierQueuesEnum
$ManualActivityCompletedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/ResolverGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ManualActivity']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ActivityStatusCompletedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ManualActivityStatusCompletedView = New-SCSMView -PassThru -Folder $ActivitiesFolder -ManagementPack $SupportGroupManagementPack -Class $ManualActivityClass -DisplayName "$SupportGroupDisplayName Completed Manual Activities" -Columns $ManualActivityColumns -Criteria $ManualActivityCompletedViewCriteria -Image $ManualActivityCompletedViewImage
if($Verbose){"Created Manual Activity Status Completed View"}

    #Review Activities where Status = In Progress and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the review activity class with the name 'ResolverGroup' that is bound to IncidentTierQueuesEnum
$ReviewActivityInProgressViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ReviewActivity']/ResolverGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ReviewActivity']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ActivityStatusInProgressId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ReviewActivityStatusInProgressView = New-SCSMView -PassThru -Folder $ActivitiesFolder -ManagementPack $SupportGroupManagementPack -Class $ReviewActivityClass -DisplayName "$SupportGroupDisplayName In Progress Review Activities" -Columns $ReviewActivityColumns -Criteria $ReviewActivityInProgressViewCriteria -Image $ReviewActivityInProgressViewImage
if($Verbose){"Created Review Activity Status In Progress View"}

    #Review Activities where Status = Closed and assigned to the support group  
    #**** NOTE ***** This criteria assumes that there is an extended property on the review activity class with the name 'ResolverGroup' that is bound to IncidentTierQueuesEnum
$ReviewActivityCompletedViewCriteria = "
<Criteria>
  <QueryCriteria xmlns=`"http://tempuri.org/Criteria.xsd`" Adapter=`"omsdk://Adapters/Criteria`">
    <Criteria>
      <FreeformCriteria>
        <Freeform>
          <Criteria xmlns=`"http://Microsoft.EnterpriseManagement.Core.Criteria/`">
            <Expression>
              <And>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ReviewActivity']/ResolverGroup`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$SupportGroupEnumerationId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
                <Expression>
                  <SimpleExpression>
                    <ValueExpressionLeft>
                      <Property>`$Context/Property[Type='System_WorkItem_Activity_Library!System.WorkItem.Activity.ReviewActivity']/Status`$</Property>
                    </ValueExpressionLeft>
                    <Operator>Equal</Operator>
                    <ValueExpressionRight>
                      <Value>{$ActivityStatusCompletedId}</Value>
                    </ValueExpressionRight>
                  </SimpleExpression>
                </Expression>
              </And>
            </Expression>
          </Criteria>
        </Freeform>
      </FreeformCriteria>
    </Criteria>
  </QueryCriteria>
</Criteria>"

$ReviewActivityStatusCompletedView = New-SCSMView -PassThru -Folder $ActivitiesFolder -ManagementPack $SupportGroupManagementPack -Class $ReviewActivityClass -DisplayName "$SupportGroupDisplayName Completed Review Activities" -Columns $ReviewActivityColumns -Criteria $ReviewActivityCompletedViewCriteria -Image $ReviewActivityCompletedViewImage
if($Verbose){"Created Review Activity Status In Progress View"}

$IncidentViews = @($IncidentStatusActiveView, $IncidentStatusPendingView, $IncidentStatusResolvedView, $IncidentStatusClosedView, $SystemWorkItemIncidentAllIncidentsView,
                    $SystemWorkItemIncidentDCMView, $SystemWorkItemIncidentEmailView, $SystemWorkItemIncidentActiveView, $SystemWorkItemIncidentSCOMView, $SystemWorkItemIncidentPortalView,
                    $SystemWorkItemIncidentActiveUnassignedView, $SystemWorkItemIncidentEscalatedView, $SystemWorkItemIncidentAssignedToMeView, $SystemWorkItemIncidentOverDueView, 
                    $SystemWorkItemIncidentPendingView, $IncidentActiveUnassignedView)
$ChangeRequestViews = @($ChangeRequestStatusInProgressView, $ChangeRequestStatusClosedView, $ManualActivityStatusInProgressView, $ManualActivityStatusInProgressUnassignedView, 
                        $ManualActivityStatusCompletedView, $ReviewActivityStatusInProgressView, $ReviewActivityStatusCompletedView, $ActivityManagementViewsManualActivitiesActive,
                        $ActivityManagementViewsReviewActivitiesActive, $ActivityManagementViewsManualActivitiesUnassigned, $ActivityManagementViewsReviewActivitiesAssignedToMe,
                        $ActivityManagementViewsAllManualActivities, $ActivityManagementViewsAllReviewActivities, $ActivityManagementViewsManualActivitiesCancelled, $ActivityManagementViewsReviewActivitiesCancelled,
                       $ActivityManagementViewsManualActivitiesCompleted, $ActivityManagementViewsReviewActivitiesRejected, $ActivityManagementViewsManualActivitiesFailed, 
                        $ActivityManagementViewsManualActivitiesAssignedToMe, $ActivityManagementViewsReviewActivitiesApproved, $ChangeManagementViewsChangeRequestsClosed, 
                        $ChangeManagementViewsChangeRequestsCancelled, $ChangeManagementViewsChangeRequestsRejected, $ChangeManagementViewsAllChangeRequests, $ChangeManagementViewsChangeRequestsAssignedToMe,
                        $ChangeManagementViewsChangeRequestsInReview, $ChangeManagementViewsChangeRequestsOnHold, $ChangeManagementViewsChangeRequestsFailed, $ChangeManagementViewsChangeRequestsCompleted,
                        $ChangeManagementViewsChangeRequestsManualActivityInProgress)
$ProblemViews = @($ProblemStatusActiveView, $ProblemStatusClosedView, $ServiceManagerProblemManagementConfigurationViewActiveKnownErrors, $ServiceManagerProblemManagementConfigurationViewActiveProblem,
                   $ServiceManagerProblemManagementConfigurationViewClosed, $ServiceManagerProblemManagementConfigurationViewAssignedToMe, $ServiceManagerProblemManagementConfigurationViewNeedingReview,
                   $ServiceManagerProblemManagementConfigurationViewResolved)    
    
    #Create Subscriptions
$IncidentSubscriptionCriteria = "
  <Criteria>
    <Expression>
      <SimpleExpression>
        <ValueExpression>
          <Property State=`"Post`">`$Context/Property[Type='System_WorkItem_Incident_Library!System.WorkItem.Incident']/TierQueue`$</Property>
        </ValueExpression>
        <Operator>Equal</Operator>
        <ValueExpression>
          <Value>{$SupportGroupEnumerationId}</Value>
        </ValueExpression>
      </SimpleExpression>
    </Expression>
  </Criteria>"

    New-SCSMNotificationSubscription -Class $IncidentClass -DisplayName $NotificationSubscriptionIncidentCreateDisplayName  -NotificationTemplate $IncidentNotificationTemplate -Criteria $IncidentSubscriptionCriteria -OperationType "Add" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack
    if($Verbose){Write-Host "Created New Incident Subscription"}
    New-SCSMNotificationSubscription -Class $IncidentClass -DisplayName $NotificationSubscriptionIncidentUpdateDisplayName  -NotificationTemplate $IncidentNotificationTemplate -Criteria $IncidentSubscriptionCriteria -OperationType "Update" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack
    if($Verbose){Write-Host "Created Updated Incident Subscription"}
    
#**** NOTE ***** This criteria assumes that there is an extended property on the change request class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ChangeRequestSubscriptionCriteria = "
  <Criteria>
    <Expression>
      <SimpleExpression>
        <ValueExpression>
          <Property State=`"Post`">`$Context/Property[Type='System_WorkItem_ChangeRequest_Library!System.WorkItem.ChangeRequest']/SupportGroup`$</Property>
        </ValueExpression>
        <Operator>Equal</Operator>
        <ValueExpression>
          <Value>{$SupportGroupEnumerationId}</Value>
        </ValueExpression>
      </SimpleExpression>
    </Expression>
  </Criteria>"
    
    New-SCSMNotificationSubscription -Class $ChangeRequestClass -DisplayName $NotificationSubscriptionChangeRequestCreateDisplayName  -NotificationTemplate $ChangeRequestNotificationTemplate -Criteria $ChangeRequestSubscriptionCriteria -OperationType "Add" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack
    if($Verbose){Write-Host "Created New Change Request Subscription"}
    New-SCSMNotificationSubscription -Class $ChangeRequestClass -DisplayName $NotificationSubscriptionChangeRequestUpdateDisplayName  -NotificationTemplate $ChangeRequestNotificationTemplate -Criteria $ChangeRequestSubscriptionCriteria -OperationType "Update" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack
    if($Verbose){Write-Host "Created Updated Change Request Subscription"}
    
#**** NOTE ***** This criteria assumes that there is an extended property on the problem class with the name 'SupportGroup' that is bound to IncidentTierQueuesEnum
$ProblemSubscriptionCriteria = "
  <Criteria>
    <Expression>
      <SimpleExpression>
        <ValueExpression>
          <Property State=`"Post`">`$Context/Property[Type='System_WorkItem_Problem_Library!System.WorkItem.Problem']/SupportGroup`$</Property>
        </ValueExpression>
        <Operator>Equal</Operator>
        <ValueExpression>
          <Value>{$SupportGroupEnumerationId}</Value>
        </ValueExpression>
      </SimpleExpression>
    </Expression>
  </Criteria>"
    
    New-SCSMNotificationSubscription -Class $ProblemClass -DisplayName $NotificationSubscriptionProblemCreateDisplayName  -NotificationTemplate $ProblemNotificationTemplate -Criteria $ProblemSubscriptionCriteria -OperationType "Add" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack
    if($Verbose){Write-Host "Created New Problem Subscription"}
    New-SCSMNotificationSubscription -Class $ProblemClass -DisplayName $NotificationSubscriptionProblemUpdateDisplayName  -NotificationTemplate $ProblemNotificationTemplate -Criteria $ProblemSubscriptionCriteria -OperationType "Update" -Recipients $NotificationRecipientADUserGroupObject -ManagementPack $SupportGroupManagementPack        
    if($Verbose){Write-Host "Created Updated Problem Subscription"}
    
    #Create UserRoles
    
    #Incident Resolver User Role
    New-SCSMUserRole -DisplayName $SupportGroupIncidentResolverUserRoleDisplayName -Profile $IncidentResolverUserRoleProfile -Objects $IncidentQueue -SCSMUsers $SupportGroupADUserGroupObject -Views $IncidentViews -ConsoleTasks $IncidentConsoleTasks -AllTemplates
    "Incident Resolver User Role Created - $SupportGroupIncidentResolverUserRoleDisplayName"
    
    #Change Manager User Role
    New-SCSMUserRole -DisplayName $SupportGroupChangeRequestManagerUserRoleDisplayName -Profile $ChangeManagerUserRoleProfile -Objects $ChangeRequestQueue -SCSMUsers $SupportGroupADUserGroupObject -Views $ChangeRequestViews -ConsoleTasks $ChangeRequestConsoleTasks -AllTemplates
    "Change Manager User Role Created - $SupportGroupChangeRequestManagerUserRoleDisplayName" 
    
    #Problem Analyst User Role
    New-SCSMUserRole -DisplayName $SupportGroupProblemAnalystUserRoleDisplayName -Profile $ProblemAnalystUserRoleProfile -Objects $ProblemQueue -SCSMUsers $SupportGroupADUserGroupObject -View $ProblemViews -ConsoleTasks $ProblemConsoleTasks -AllTemplates
    "Problem Analyst User Role Created - $SupportGroupProblemAnalystUserRoleDisplayName"
}

