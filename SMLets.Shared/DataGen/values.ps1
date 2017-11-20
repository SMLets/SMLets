
# this is a 3 month load
# rate is 

$User20KCounts = @{
    Microsoft.AD.Group = 200
    Microsoft.AD.Printer = 800
    Microsoft.AD.User = 20000
    Microsoft.SystemCenter.BusinessService = 30
    Microsoft.SystemCenter.ConfigurationManager.DeployedComputer = 20000
    Microsoft.Windows.Client.Computer = 18500
    Microsoft.Windows.Computer = 20000
    Microsoft.Windows.Server.Computer = 1200
    System.FileAttachment = 6000
    System.Knowledge.Article = 4000
    System.Reviewer = 20000
    System.SoftwareItem = 1000
    System.SoftwareUpdate = 1000
    System.WorkItem.Activity.ManualActivity = 20000
    System.WorkItem.Activity.ReviewActivity = 20000
    System.WorkItem.Activity.WorkflowTarget = 1
    System.WorkItem.BillableTime = 40000
    System.WorkItem.ChangeRequest = 17000
    System.WorkItem.Incident = 60000
    # The following should be generated
    #
    #
    #
    System.WorkItem.TroubleTicket.AnalystCommentLog = 300000
    System.WorkItem.TroubleTicket.UserCommentLog = 300000

    # Software Items per computer
    SIPerComputer = 30
    # Service Updates per computer
    SUPerComputer = 101

    # Comment logs per incident
    CmtLogsPerIncident = 10

    ## Change request volumes as a percentage
    ChangeStandard = .80
    ChangeMinor = .5
    ChangeEmergency = .10
    ChangeMajor = .5
    }

####
## DataGen process
## ---
## create ad entities
## StoreConfig ???
## create Knowledge
## create Software Items
## create software updates
## scenarios???
## incidents (datagen creates objects and relationships)
##      Id
##      DisplayName
##      Priority
##      Description
##      NeedsKnowledgeArticle
##      HasCreatedKnowledgeArticle
##      ContactMethod
##      ResolutionDescription
##      ResolvedDate
##      UrgencyLookup
##      Urgency
##      ImpactLookup
##      Impact
##      ClassificationLookup
##      Classification
##      ResolutionCategoryLookup
##      ResolutionCategory
##      StatusLookup
##      Status
##      SourceLookup
##      Source
##      LastModifiedSource
##      ActualStartDate
##      ScheduledEndDate
##      ScheduledStartDate
##      ActualEndDate
##      TierQueue
##      TargetResolutionTime
##      Escalated
##      Title
##      CreatedDate
##      ClosedDate
##      ScheduledDowntimeEndDate
##      ScheduledDowntimeStartDate
##      ActualDowntimeEndDate
##      ActualDowntimeStartDate
##      IsDowntime
##   set the following relationships:
##      System.WorkItem.IncidentPrimaryOwner" Type="Reference" Guid="c4b0b8d3-ec7c-97d2-88f6-8e3852c342b0" TargetTypeName="System.User" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ReviewActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ManualActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.PortalSoftwareDeploymentActivity" />
##      System.WorkItem.TroubleTicketHasNotificationLog" TargetTypeName="System.WorkItem.TroubleTicket.SipNotificationLog" />
##      System.WorkItem.TroubleTicketHasNotificationLog" TargetTypeName="System.WorkItem.TroubleTicket.SmtpNotificationLog" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ParallelActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.SequentialActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.DependentActivity" />

## create change requests
##   set the following properties:
##      Id
##      DisplayName
##      Description
##      PriorityLookup
##      Priority
##      ImpactLookup
##      Impact
##      ContactMethod
##      Reason
##      AreaLookup
##      Area
##      RequiredByDate
##      ScheduledEndDate
##      RiskAssessmentPlan
##      ImplementationResults
##      TestPlan
##      ScheduledStartDate
##      BackoutPlan
##      Notes
##      ImplementationPlan
##      StatusLookup
##      Status
##      RiskLookup
##      Risk
##      CategoryLookup
##      Category
##      ActualEndDate
##      TemplateId
##      ActualStartDate
##      PostImplementationReview
##      Title
##      CreatedDate
##      ScheduledDowntimeEndDate
##      ScheduledDowntimeStartDate
##      ActualDowntimeEndDate
##      ActualDowntimeStartDate
##      IsDowntime
##   and relationships
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ReviewActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ManualActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.PortalSoftwareDeploymentActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.ParallelActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.SequentialActivity" />
##      System.WorkItemContainsActivity" TargetTypeName="System.WorkItem.Activity.DependentActivity" />

## create activities
## create service maps
# Data
