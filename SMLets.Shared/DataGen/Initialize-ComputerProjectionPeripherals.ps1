param ( [switch]$whatif, [switch]$verbose, [switch]$nuke, [switch]$view , [switch]$raw)
. ./Common.ps1
# Use WMI to get mimic details about the peripherals needed
# by the ComputerProjection
# The list of devices:
#    PhysicalComputer
#    OperatingSystem
#    NetworkAdapter
#    Processor
#    PhysicalDisk
#    LogicalDisk
# 

# NUKE - blow away any of the object we'll create
if ( $nuke -or $view )
{
    $remove = "Microsoft.Windows.OperatingSystem",
    "Microsoft.Windows.Peripheral.NetworkAdapter",
    "Microsoft.Windows.Peripheral.Processor",
    "Microsoft.Windows.Peripheral.DiskPartition",
    "Microsoft.Windows.Peripheral.PhysicalDisk",
    "Microsoft.Windows.Peripheral.LogicalDisk" | 
        get-scsmclass | 
        get-scsmobject -filter "PrincipalName -like %Computer%" 
    if ( $view )
    {
        if ( $raw )
        {
            $remove
        }
        else
        {
        $remove | ft TypeName,DisplayName,PrincipalName -au
        }
    }
    else
    {
        if ( $remove ) 
        {
            $remove | remove-scsmobject -force 
        }
    }
    exit
}

trap { write-error $error[0]; exit }

$class  = "Microsoft.Windows.Computer"
$filter = "PrincipalName -like '%Computer%'"
$ARGUMENTS = @{ 
    WhatIf = $whatif 
    Verbose = $verbose
    Bulk = $true
    }

$KV = new-object "System.Collections.Generic.Dictionary[System.String,System.String]"
$KV.Add("Microsoft.Windows.OperatingSystem",          "Get-OperatingSystem")
$KV.Add("Microsoft.Windows.Peripheral.NetworkAdapter","Get-NetworkAdapter")
$KV.Add("Microsoft.Windows.Peripheral.Processor",     "Get-Processor")
$KV.Add("Microsoft.Windows.Peripheral.PhysicalDisk",  "Get-PhysicalDisk")
$KV.Add("Microsoft.Windows.Peripheral.LogicalDisk",   "Get-LogicalDisk")
$KV.Add("Microsoft.Windows.Peripheral.DiskPartition", "Get-Partition")

$global:HASHCOLLECTION = @{}
$KV.Keys |%{
    $key = $_
    $HASHCOLLECTION["$key"] += @(& $KV[$key] $PrincipalName)
}

$computerList = get-scsmobject $class -filter $filter
$HASHCOLLECTION.Keys | %{
    $key = $_
    write-host -for red $key
    $computerList | %{
        $PrincipalName = $_.PrincipalName
        write-host -for green "creating peripherals for $PrincipalName"
        $ARGUMENTS['Name'] = $key
        $HASHCOLLECTION[$key]| %{$_.PrincipalName = $PrincipalName; $_}|new-scsmobject @ARGUMENTS
        } 
}
