param ( 
    $taskname = "PSScriptWithArgs.Task", 
    [hashtable]$parametervalues, 
    $ComputerName = "localhost", 
    [string]$MPName = ".*",
    [switch]$Synchronous,
    [switch]$whatif 
    )
if ( $args -eq "-?" )
{
    "Invoke-SCSMTask -taskname <taskname> -parameters @{ Argument = 'value' }"
    exit
}
# this trap is here because if you attempt to run the script
# without first loading the Service Manager core assembly, bad things will happen
trap [System.Management.Automation.RuntimeException] { 
    if ( $error[0].FullyQualifiedErrorId -eq "TypeNotFound" )
    {
        Write-Host -for red "Service Manager Core Library may not be loaded"
        Write-Host -for red "To load the library, type the following:"
        Write-Host -for red '   [Reflection.Assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")'
    }
    else
    {
        "TRAP: Unknown error, exiting"
        "TRAP: " + $error[0].FullyQualifiedErrorId
    }
    exit 
    }

# some "constants" that we need
$DEFAULT  = [Microsoft.EnterpriseManagement.Common.ObjectQueryOptions]::Default
$EMOT     = [Microsoft.EnterpriseManagement.Common.EnterpriseManagementObject]
$PAIRTYPE = [Microsoft.EnterpriseManagement.Common.Pair``2]
$OVERPARM = [Microsoft.EnterpriseManagement.Configuration.ManagementPackOverrideableParameter]

# create a connection to the Service Manager server
$emg = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $ComputerName
$TaskToRun = $emg.TaskConfiguration.GetTasks()|?{$_.name -eq $taskName}
if ( $TaskToRun -is [array] )
{
    $HaveTask = $false
    if ( $MPName )
    {
        $SpecificTask = $TaskToRun|?{ $_.GetManagementPack().Name -eq $MPName }
        if ( $SpecificTask -is [Microsoft.EnterpriseManagement.Configuration.ManagementPackTask] )
        {
            $TaskToRun = $SpecificTask
            $HaveTask = $true
        }
    }
    if ( ! $HaveTask )
    {
        "Multiple instances of task $taskName exit, exiting"
        exit
    }
}
if ( ! $TaskToRun )
{
    "Task: '$taskname' could not be found"
    "Here are the available tasks:"
    $emg.TaskConfiguration.GetTasks()|ft Name,DisplayName -au
    exit
}
$IMGMT    = $emg.EntityObjects.GetType()
$TargetClass = $emg.EntityTypes.GetClass($TaskToRun.Target.Id)
# Now retrieve the Target for the task. This is needed so the task will execute
# we need to use some reflection in order to do this all from script.
# Check for singleton, if so call GetObject
if ( $TargetClass.Singleton )
{
    [type[]]$TYPES    = [system.guid],[Microsoft.EnterpriseManagement.Common.ObjectQueryOptions]
    $GetObjectMethod  = $IMGMT.GetMethod("GetObject",$TYPES)
    $GenericMethod    = $GetObjectMethod.MakeGenericMethod($EMOT)
    [array]$arguments = [guid]($TaskToRun.Target.Id),$DEFAULT
    $Target = $GenericMethod.invoke($emg.EntityObjects,$arguments) 
}
else
{
    # Since the class is not a singleton, we need to use GetObjectReader
    # this means that we may get more targets then we need, in this case we will
    # use the first object that we get
    # TODO: modify script to allow user to choose target
    [type[]]$TYPES    = [Microsoft.EnterpriseManagement.Configuration.ManagementPackClass],
                        [Microsoft.EnterpriseManagement.Common.ObjectQueryOptions]
    $GetObjectReader  = $IMGMT.GetMethod("GetObjectReader", $TYPES)
    $GenericMethod    = $GetObjectReader.MakeGenericMethod($EMOT)
    [array]$arguments = [Microsoft.EnterpriseManagement.Configuration.ManagementPackClass]($TargetClass),$DEFAULT
    $Results          = $GenericMethod.invoke($emg.EntityObjects,$arguments) 
    $Target = $results.GetData(0)
    # Communicate with the user so that if we did get more than one possible target
    # we give them a chance to cancel the task submission
    if ( $results.Count -gt 1 )
    {
        Write-Host -for red "Using Target: " + $target.DisplayName
        Read-Host "Press <Enter> to continue, or Cntl-C to interrupt"
    }
}
# if we didn't retrieve a target instance, we need to stop
if ( ! $Target ) 
{
    "Failed to retrieve target (id: " + $TaskToRun.Target.Id + ")"
    exit
}

# Now we need to add values for any configuration that may be needed
# this is where we can set any overrideable parameters that are defined in the task
$OverrideableParameters = $TaskToRun.GetOverrideableParameters()|%{$h=@{}}{$h.($_.name) = $_}{$h}
$Configuration = new-object Microsoft.EnterpriseManagement.Configuration.TaskConfiguration
# Curious PowerShell ideosyncracy:
# the hashtable that gets passed as a parameter has a key (which is null), we can get
# around this by building a new hashtable
$FixedHash = new-object hashtable $parametervalues
foreach($key in $FixedHash.keys)
{
    $ParameterToOverride = $OverrideableParameters[$key]
    if (  $ParameterToOverride )
    {
        # attempt to convert the value to the correct type
        # we know that a number of these will probably fail (such as enum type)
        $ValueType = $ParameterToOverRide.ParameterType.ToString() -as [type]
        # if we have a value, then we need to associate it with a Pair
        # which is then used with the Overrides
        if ( $ValueType )
        {
            $Val = $parameterValues[$key] -as $valueType
            # if we have value, we can attempt to add it to the overrides
            if ( $Val )
            {
                $MyTypePair = $OVERPARM,$ValueType
                $genericType = $PAIRTYPE.MakeGenericType($MyTypePair)
                $myOverride = [activator]::CreateInstance($genericType, $ParameterToOverRide,$Val)
                $Configuration.Overrides.Add($myOverride)
            }
            else
            {
                # we couldn't convert the value that was passed to the type we needed, so
                # we'll skip this value. Note that this may cause the task to fail because the
                # task arguments aren't being passed correctly, but we'll try anyway
                Write-Host -for red ("Cannot convert " + $parameterValues[$key] + " to $valueType")
                Write-Host -for red "skipping"
            }
        }
        else
        {
            # We couldn't figure out how to convert the type of the parameter to a .NET
            # type, so we'll skip this one. This may cause the task to fail because the
            # task arguments aren't being passed correctly, but we'll try anyway
            Write-Host -for red ("Cannot convert " + $ParameterToOverRide.Parametertype + " to .NET type")
            Write-Host -for red "skipping"
        }
    }
    else
    {
        # This means that a parameter was specified in the HashTable that isn't
        # an overrideable parameter. This can be skipped, because it can't be used
        Write-Host -for red "The parameter '$key' does not exist, skipping"
    }
}
# Now submit the task, unless -WhatIf has been specified
if ( $Whatif )
{
    Write-Host "WhatIf: Submitting Task $TaskToRun"
}
else
{
    # if execute synchronously, just call the ExecuteTask method.
    # it returns the results
    if ( $Synchronous )
    {
        $emg.TaskRuntime.ExecuteTask($Target, $TaskToRun, $Configuration )
    }
    else
    {
        # this is the task submission API
        $id = $emg.TaskRuntime.SubmitTask($Target, $TaskToRun, $Configuration )
        "TaskId: $id"
    }
}
