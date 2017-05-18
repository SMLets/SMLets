# This is a set of tests for the fix of 
# 6798 
# Incident Classification (and probably other enums) are Being 
# Handled by Display Name in Switch Statement - Need to Fix to Use 
# Name instead of Display Name 
# The fix is applied to 
# New-SCSMIncident
# Get-SCSMIncident
# Set-SCSMIncident
# The fix is implemented in the GetEnum helper which now tries very
# hard to get the enumeration by:
# ID
# Name
# the last token of the Name of the enum
# DisplayName
# regex match of name
# regex match of displayname
#
# first thing, create a bunch of incidents being sure to set
# the various parameters which need the enum - those parameters are:
# Impact
# Urgency
# Status
# Classification
# Source

$uniqString = (get-date).ToString()
$title = "title: ${uniqString}"

$IncidentArgs = @{
    Title = "${title}"
    Description = "This is the description of an incident as of ${uniqString}"
    Impact = "Medium"
    Urgency = "Medium"
    Classification = "Printing"
    Status = (Get-SCSMEnumeration IncidentStatusEnum.Active.Pending).Id
    Source = "IncidentSourceEnum.System"
    }

new-scsmincident @IncidentArgs

$IncidentArgs = @{
    Impact = "Medium"
    Urgency = "Medium"
    Classification = "Printing"
    Status = (Get-SCSMEnumeration IncidentStatusEnum.Active.Pending).Id
    Source = "IncidentSourceEnum.System"
    }

$incident = get-scsmincident @IncidentArgs|?{ $_.Title.ToString() -eq $title }
if ( $incident ) { "PASS" }
else { "FAIL" }
