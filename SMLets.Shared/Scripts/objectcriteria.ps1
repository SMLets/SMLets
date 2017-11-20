$c = get-scsmclass ManualActivity$
$CriteriaT = "Microsoft.EnterpriseManagement.Common.EnterpriseManagementObjectCriteria"
$now = [datetime]::Now
new-object $CriteriaT "Name == 'zowee'",$c
new-object $CriteriaT "TimeAdded < '$now'",$c
new-object $CriteriaT "TimeAdded > '$now'",$c
new-object $CriteriaT "Name like '%EE%'",$c
new-object $CriteriaT "Name like '%6932' and ContactMethod like 'do%'",$c
new-object $CriteriaT "Status == 'Canceled'",$c
new-object $CriteriaT "Status == 'Canceled'",$c
new-object $CriteriaT "Priority == '{54a2b149-b338-a54e-9446-cb3b5a23586d}'",$c
