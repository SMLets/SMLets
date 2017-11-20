
Get-SCSMRule| 
    ?{ $_.WriteActionCollection | ?{ $_.Configuration -match "SendNotificationsActivity"}}|
    %{ $_.psobject.typenames[0] = "ManagementPackRule#NotificationSubscription"; $_ }
#$coll | %{
#    $_.psobject.typenames[0] = "ManagementPackRule#NotificationSubscription"
#    $_ | Add-member -pass NoteProperty ManagementPack $_.GetManagementPack()
#    }
