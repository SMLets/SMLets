param ( $batchid, $computername = "localhost" )
$EMGT = "Microsoft.EnterpriseManagement.EnterpriseManagementGroup"
$EMG = new-object $EMGT $computername
if ( $batchid )
{
    $emg.TaskRuntime.GetTaskResultsByBatchId($batchid)
}
else
{
    $emg.TaskRuntime.GetTaskResults()
}
