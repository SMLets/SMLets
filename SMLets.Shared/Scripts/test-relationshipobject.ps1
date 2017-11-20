$btr = get-scsmrelationshipclass System.WorkItem.BillableTimeHasWorkingUser
# Source is user
# target is Billable Time
$source = get-scsmobject -class (get-scsmclass -name System.User$) -filter "DisplayName -like 'Travis%'"
$target = get-scsmobject -class (get-scsmclass -name System.WorkItem.BillableTime) | select -first 1

#TODO: This doesnt seem to be working anymore... 
#error: New-SCSMRelationshipObject : Parameter set cannot be resolved using the specified named parameters.
new-scsmrelationshipobject -passthru -Relationship $btr -Source $source -Target $target