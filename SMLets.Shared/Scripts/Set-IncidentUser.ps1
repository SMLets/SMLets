#requires -version 2.0
[CmdletBinding(SupportsShouldProcess=$true)]
param (
    [Parameter()][string]$AssignedTo,
    [Parameter()][string]$Affected,
    [Parameter(ValueFromPipeline=$true)]$TheIncident
)
BEGIN
{
    $NS = "Microsoft.EnterpriseManagement"
    $EMGType = "${NS}.EnterpriseManagementGroup"
    $EMG = new-object ${EMGType} localhost
 
    $ASSIGNEDTORELATIONSHIP = $EMG.EntityTypes.GetRelationshipClasses()|?{$_.name -eq "System.WorkItemAssignedToUser"}
    $AFFECTEDRELATIONSHIP = $EMG.EntityTypes.GetRelationshipClasses()|?{$_.name -eq "System.WorkItemAffectedUser" }
    $DEFAULT = ("${NS}.Common.ObjectQueryOptions" -as "type")::Default
    $EMOT    = "${NS}.Common.EnterpriseManagementObject" -as "type"
    $EMOP = "EnterpriseManagementObjectProjection"
    $IPT = "System.WorkItem.Incident.ProjectionType"
    $CEMO = "${NS}.Common.CreatableEnterpriseManagementObject"
    $INCIDENTC = $EMG.EntityTypes.GetClasses() |?{$_.name -eq "System.WorkItem.Incident" }
    $COMMENTC = $EMG.EntityTypes.GetClasses()|?{$_.name -eq "System.WorkItem.TroubleTicket.AnalystCommentLog"}
    $USERC    = $EMG.EntityTypes.GetClasses()|?{$_.name -eq "System.User"}
    $PROJECTION = $EMG.EntityTypes.GetTypeProjections()| ?{$_.name -eq $IPT}
    $MPCLASSTYPE = "${NS}.Configuration.ManagementPackClass"
    $SYSTEMMP = $EMG.ManagementPacks.GetManagementPacks()|?{$_.name -eq "System.Library"}

    function Get-User
    {
 
        param ( 
            [parameter(Mandatory=$true,Position=0)][string]$displayname
            )
            $criteriaString = '
     <Criteria xmlns="http://Microsoft.EnterpriseManagement.Core.Criteria/">
      <Reference Id="System.Library" Version="{0}" PublicKeyToken="{1}" Alias="System" />
      <Expression>
        <SimpleExpression>
          <ValueExpressionLeft><Property>$Target/Property[Type=''System!System.Domain.User'']/DisplayName$</Property></ValueExpressionLeft>
          <Operator>Equal</Operator>
          <ValueExpressionRight><Value>{2}</Value></ValueExpressionRight>
        </SimpleExpression>
      </Expression>
    </Criteria>
    ' -f $SYSTEMMP.Version, $SYSTEMMP.KeyToken, $displayname
        $userclass = get-scsmclass System.Domain.User
        $CriteriaType = "Microsoft.EnterpriseManagement.Common.EnterpriseManagementObjectCriteria"
        $global:criteria = new-object $CriteriaType $criteriastring,$userclass,$EMG
        # write-host -for cyan $criteria
        $r = Get-SCSMObject -criteria $criteria 
#        $r = Get-SCSMObject System.Domain.User -filter "DisplayName -eq '$displayName'"
        # write-host -for cyan $r
        $r

    }
    }
    PROCESS
    {
        $incident = $TheIncident.__base

        if ( $AssignedTo )
        {
            $AssignedToUser = Get-User $AssignedTo
            $Incident.Add(($AssignedToUser -as $EMOT) ,$ASSIGNEDTORELATIONSHIP.Target)
            if ( $Incident.Item("AssignedWorkItem").Count -gt 1 )
            {
                $Incident.Item("AssignedWorkItem")[0].Remove()
            }
        }

        if ( $Affected )
        {
            $AffectedUser = Get-User $Affected
            $Incident.Add(($AffectedUser -as $EMOT) ,$AFFECTEDRELATIONSHIP.Target)
            if ( $Incident.Item("CreatedWorkItem").Count -gt 1 )
            {
                $Incident.Item("CreatedWorkItem")[0].Remove()
            }
        }

        if ( $PSCmdlet.ShouldProcess($TheIncident.DisplayName))
        {
            $Incident.Commit()
        }

    }
