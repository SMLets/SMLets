# This is a set of tests for the fix of 
# 7730
# Add SupportGroup parameter to allow for setting of support group

$uniqString = (get-date).ToString()
$title = "title: ${uniqString}"
$supportGroup = "Tier 2"

$IncidentArgs = @{
    Title = "${title}"
    Description = "This is the description of an incident as of ${uniqString}"
    Impact = "Medium"
    Urgency = "Medium"
    Classification = "Printing"
    Status = (Get-SCSMEnumeration IncidentStatusEnum.Active.Pending).Id
    Source = "IncidentSourceEnum.System"
    SupportGroup = "$supportGroup"
    }

new-scsmincident @IncidentArgs

if ( (get-scsmincident -title $title).SupportGroup -eq $supportGroup )
{ 
    "PASS" 
}
else 
{ 
    "FAIL" 
}
