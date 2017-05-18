$bt1 = Get-SCSMRelationshipClass billabletimehas
$bt2 = Get-SCSMRelationshipClass billabletime$
$btc = get-scsmclass system.workitem.billabletime
$users = get-scsmobject Microsoft.AD.User -filter "FirstName -like '%'"
$random = new-object random
get-scsmobject System.WorkItem.Incident | %{
    $wi = $_
    $u = $users[$random.Next(0,$users.count)]
    Write-Progress -Activity "Creating Billable Time" -Status $wi.Id

    for($itr = 0; $itr -lt 3; $itr++)
    {
        $id = [guid]::newGuid().ToString()
        # this requires an uncommited instance
        # when the relationship is commited, so will the instance
        $bti = new-scsmobject -NoCommit $btc -PropertyHashtable @{ 
            DisplayName = $id
            Id = $id
            TimeInMinutes = $random.Next(30,55);
            LastUpdated = [datetime]::Now 
            }
        New-SCSMRelationshipObject -Relationship $bt2 -Source $wi -Target $bti
        New-SCSMRelationshipObject -Relationship $bt1 -Source $bti -Target $u
        start-sleep -mil 2
    }
}

