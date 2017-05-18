$queueArgs = @{
    Class  = get-scsmclass System.WorkITem.Incident$
    filter = "DisplayName -like '*IR*'" 
    ManagementPackName = "TESTQMP001"
    Name = "Queue by Cmdlet"
    ManagementPackFriendlyName = "A friendly name for an MP"
    Description = "This is a Queue created by a cmdlet"
    PassThru = $true
    Verbose  = $true
    Import = $true
    }
new-SCQUEUE @queueArgs
$QueueArgs.Filter = "DisplayName -like '*IR*' -and Name -eq 'foo'"
new-SCQUEUE @queueArgs
$QueueArgs.Filter = "Status -eq 'Resolved'"
new-SCQUEUE @queueArgs

Get-SCManagementPack -Name TESTQMP001 | Remove-SCManagementPack