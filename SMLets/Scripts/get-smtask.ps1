param ( $computername = "localhost" )
[reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")|out-null
$EMG = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $computername
$EMG.TaskConfiguration.GetTasks() 
