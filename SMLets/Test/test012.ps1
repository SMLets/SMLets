BEGIN
{
    . ./Common.ps1
    $PT = "System.WorkItem.Incident.View.ProjectionType"
    $props = "CreatedDate","ID","DisplayName",{$_.object.get_id()}
    set-alias gsop get-SCSMObjectProjection
    set-alias os out-string
    $count = 10
}

END
{
    ### PROJECTION
    $r = gsop $PT -max $count
    if ( $r.Count -eq $count ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}Count") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}Count") }

    $r = gsop $PT -sort tIMEaDDED    -max $count
    if ( $r[0].TimeAdded -le $r[-1].TimeAdded ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}+tIMEaDDED") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}+tIMEaDDED") }
    $r = gsop $PT -sort -TimeAdded   -max $count
    if ( $r[0].TimeAdded -ge $r[-1].TimeAdded ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}-TimeAdded") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}-TimeAdded") }

    $r = gsop $PT -sort DisplayName  -max $count
    if ( $r[0].DisplayName -le $r[-1].DisplayName ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}+DisplayName") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}+DisplayName") }
    $r = gsop $PT -sort -DisplayName -max $count
    if ( $r[0].DisplayName -ge $r[-1].DisplayName ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}-DisplayName") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}-DisplayName") }

    $r = gsop $PT -sort Id -max $count -filter "Id -like '*IR??'"
    if ( $r[0].Id -le $r[-1].Id ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}+Id") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}+Id") }
    $r = gsop $PT -sort "-Id" -max $count -filter "Id -like '*IR??'"
    if ( $r[0].Id -ge $r[-1].Id ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}-Id") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}-Id") }

    $r = gsop $PT -sort Priority 
    if ( $r[0].Priority -le $r[-1].Priority ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}+Priority") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}+Priority") }
    $r = gsop $PT -sort -Priority
    if ( $r[0].Priority -ge $r[-1].Priority ) { Out-TestLog ("PASS: PROJECTION " + [datetime]::Now + ":${TestName}-Priority") } else { Out-TestLog ("FAIL: PROJECTION " + [datetime]::Now + "${TestName}-Priority") }


    $PT = get-scsmclass -name "System.WorkItem.Incident$"
    $props = "CreatedDate","ID","DisplayName",{$_.object.get_id()}
    set-alias gso get-SCSMObject

    ### INSTANCE
    $r = gso $PT -max $count
    if ( $r.Count -eq $count ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}Count") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}-Count") }

    $r = gso $PT -sort tIMEaDDED    -max $count
    if ( $r[0].TimeAdded -le $r[-1].TimeAdded ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}+tIMEaDDED") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}+tIMEaDDED") }
    $r = gso $PT -sort -TimeAdded   -max $count
    if ( $r[0].TimeAdded -ge $r[-1].TimeAdded ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}-TimeAdded") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}-TimeAdded") }

    $r = gso $PT -sort DisplayName  -max $count
    if ( $r[0].DisplayName -le $r[-1].DisplayName ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}+DisplayName") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}+DisplayName") }
    $r = gso $PT -sort -DisplayName -max $count
    if ( $r[0].DisplayName -ge $r[-1].DisplayName ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}-DisplayName") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}-DisplayName") }

    $r = gso $PT -sort Id -max $count -filter "Name -like '*IR??'"
    if ( $r[0].Id -le $r[-1].Id ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}+Name") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}+Name") }
    $r = gso $PT -sort "-Id" -max $count -filter "Name -like '*IR??'"
    if ( $r[0].Id -ge $r[-1].Id ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}-Name") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}-Name") }

    $r = gso $PT -sort Priority 
    if ( $r[0].Priority -le $r[-1].Priority ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}+Priority") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}+Priority") }
    $r = gso $PT -sort -Priority
    if ( $r[0].Priority -ge $r[-1].Priority ) { Out-TestLog ("PASS: INSTANCE " + [datetime]::Now + ":${TestName}-Priority") } else { Out-TestLog ("FAIL: INSTANCE " + [datetime]::Now + "${TestName}-Priority") }

}
