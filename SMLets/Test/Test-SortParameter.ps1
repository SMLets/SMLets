$PT = "System.WorkItem.Incident.View.ProjectionType"
$props = "CreatedDate","ID","DisplayName",{$_.object.get_id()}
set-alias gsop get-SCSMObjectProjection
set-alias os out-string
$count = 10

### PROJECTION
"PROJECTION"
$r = gsop $PT -max $count
if ( $r.Count -eq $count ) { "PASS - Count" } else { "FAIL - Count" }

$r = gsop $PT -sort tIMEaDDED    -max $count
if ( $r[0].TimeAdded -le $r[-1].TimeAdded ) { "PASS + tIMEaDDED" } else { "FAIL + tIMEaDDED" }
$r = gsop $PT -sort -TimeAdded   -max $count
if ( $r[0].TimeAdded -ge $r[-1].TimeAdded ) { "PASS - TimeAdded" } else { "FAIL - TimeAdded" }

$r = gsop $PT -sort DisplayName  -max $count
if ( $r[0].DisplayName -le $r[-1].DisplayName ) { "PASS + DisplayName" } else { "FAIL + DisplayName" }
$r = gsop $PT -sort -DisplayName -max $count
if ( $r[0].DisplayName -ge $r[-1].DisplayName ) { "PASS - DisplayName" } else { "FAIL - DisplayName" }

$r = gsop $PT -sort Id -max $count -filter "Id -like '*IR??'"
if ( $r[0].Id -le $r[-1].Id ) { "PASS + Id" } else { "FAIL + Id" }
$r = gsop $PT -sort "-Id" -max $count -filter "Id -like '*IR??'"
if ( $r[0].Id -ge $r[-1].Id ) { "PASS - Id" } else { "FAIL - Id" }

$r = gsop $PT -sort Priority 
if ( $r[0].Priority -le $r[-1].Priority ) { "PASS + Priority" } else { "FAIL + Priority" }
$r = gsop $PT -sort -Priority
if ( $r[0].Priority -ge $r[-1].Priority ) { "PASS - Priority" } else { "FAIL - Priority" }


$PT = get-scsmclass -name "System.WorkItem.Incident$"
$props = "CreatedDate","ID","DisplayName",{$_.object.get_id()}
set-alias gso get-SCSMObject

### INSTANCE
"INSTANCE"
$r = gso $PT -max $count
if ( $r.Count -eq $count ) { "PASS - Count" } else { "FAIL - Count" }

$r = gso $PT -sort tIMEaDDED    -max $count
if ( $r[0].TimeAdded -le $r[-1].TimeAdded ) { "PASS + tIMEaDDED" } else { "FAIL + tIMEaDDED" }
$r = gso $PT -sort -TimeAdded   -max $count
if ( $r[0].TimeAdded -ge $r[-1].TimeAdded ) { "PASS - TimeAdded" } else { "FAIL - TimeAdded" }

$r = gso $PT -sort DisplayName  -max $count
if ( $r[0].DisplayName -le $r[-1].DisplayName ) { "PASS + DisplayName" } else { "FAIL + DisplayName" }
$r = gso $PT -sort -DisplayName -max $count
if ( $r[0].DisplayName -ge $r[-1].DisplayName ) { "PASS - DisplayName" } else { "FAIL - DisplayName" }

$r = gso $PT -sort Id -max $count -filter "Name -like '*IR??'"
if ( $r[0].Id -le $r[-1].Id ) { "PASS + Name" } else { "FAIL + Name" }
$r = gso $PT -sort "-Id" -max $count -filter "Name -like '*IR??'"
if ( $r[0].Id -ge $r[-1].Id ) { "PASS - Name" } else { "FAIL - Name" }

$r = gso $PT -sort Priority 
if ( $r[0].Priority -le $r[-1].Priority ) { "PASS + Priority" } else { "FAIL + Priority" }
$r = gso $PT -sort -Priority
if ( $r[0].Priority -ge $r[-1].Priority ) { "PASS - Priority" } else { "FAIL - Priority" }


