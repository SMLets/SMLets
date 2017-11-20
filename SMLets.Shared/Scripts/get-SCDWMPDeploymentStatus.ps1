$mps = get-scmanagementpack # | ?{$_.sealed}
$mps | %{
    $mp = $_
    $status = ($mp.Store.Deployment.GetManagementPackDeploymentStatus([guid[]]$mp.id)).Values| 
        Select Status,ManagementPackDeploymentCompletionTime,ManagementPackImportTime
    new-object psobject -property @{
        ManagementPack = $mp
        ManagementPackName = $mp.Name
        Status = $status.Status
        CompletionTime = $status.ManagementPackDeploymentCompletionTime.ToLocalTime()
        ImportTime = $status.ManagementPackImportTime.ToLocalTime()
        } | %{$_.psobject.typenames.Insert(0,"SMLets.ManagementPackDeploymentData");$_}
    }
