param  ( $taskname = '.*' )
$emg = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup localhost
$tasks = $emg.TaskConfiguration.GetTasks()|?{$_.name -match $taskname }
$tasks | %{
    $_.GetOverrideableParameters()
} | ft -group ParentElement Name,ParameterType,Selector -au

