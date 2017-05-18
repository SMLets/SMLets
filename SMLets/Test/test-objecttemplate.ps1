$template = get-scsmobjecttemplate -name HighPriorityIncidentTemplate

$high = (get-scsmenumeration System.WorkItem.TroubleTicket.UrgencyEnum.High).id

# find a projection which we can test against
$p = Get-SCSMObjectProjection system.workitem.incident.projectiontype -sort Status -filter "Urgency -ne '$high'" -max 1
$id = $p.id

# test whatif is really not updating instance
Set-SCSMObjectTemplate -Projection $p.__base -Template $template -whatif
start-sleep 3
$p = Get-SCSMObjectProjection System.WorkItem.Incident.ProjectionType -filter "Id -eq '$id'"
if ( $p.Urgency.DisplayName -eq "High" ) { "FAIL" } else { "PASS" }

# now really update the projection with the template
Set-SCSMObjectTemplate -Projection $p.__base -Template $template  
start-sleep 3
$p = Get-SCSMObjectProjection System.WorkItem.Incident.ProjectionType -filter "Id -eq '$id'"
$TestTierQueue = $p.TierQueue.DisplayName -eq "Tier 1"
$TestUrgency   = $p.Urgency.DisplayName -eq "High"
$TestImpact    = $p.Impact.DisplayName -eq "High"
if ( $TestTierQueue -and $TestUrgency -and $TestImpact ) { "PASS" } else { "FAIL" }
