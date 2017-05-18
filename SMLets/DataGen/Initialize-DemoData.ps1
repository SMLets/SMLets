[CmdletBinding()]
param ( 
    [Parameter()]
    [ValidateSet("tiny","100","500","1k","10k","20k")]
    $size = "tiny",
    [Parameter()]
    [Switch]$whatif,
    # a parameter to just create one of each
    [Parameter()]
    [int]$count
    
    )

#
# This file contains various profile information for
# setting up the artifacts for the script based demo data generation
#
# 

$COMPUTERPROFILES = @{
    PHYSICALDISKS    = 1..2
    OPERATINGSYSTEM  = 1
    PROCESSOR        = 2..4
    DISKPARTITION    = 2..4
    LOGICALDISK      = 2..10
    NETWORKADAPTER   = 3..10
    }

# here are the ratios - based on a single computer
$RATIOS = @{
    COMPUTER        = 1
    USER            = 1     # 1 user for every computer
    INCIDENT        = 10    # 10 incidents for every computer
    CHANGEREQUEST   = .012  # 1 change requests for every 80 computers
    PROBLEM         = .005  # 1 for every 500 computers
    SERVICE         = .005  # 1 service for every 500 computerc
    SOFTWAREUPDATE  = 50    # 50 software updates per computer
    SOFTWAREITEM    = 20    # 20 software packages per computer
    RELEASERECORD   = .001  # 1 for every 1000 computers
    KNOWLEDGE       = .1    # 1 for every 10 computers
    GROUP           = .025  # 1 for every 40 computers
    QUEUE           = .001  # 1 for every 1000 computers
    PRINTER         = .01   # 1 for every 100 computers
    SERVICEREQUEST  = .01   # 1 for every 100 computers
    }

# be sure to always create at least this many
# in case the profile is one of the small ones
$MINIMUM = @{
    COMPUTER        = 10
    USER            = 10
    INCIDENT        = 10
    CHANGEREQUEST   = 5
    PROBLEM         = 5
    SERVICE         = 3
    SOFTWAREUPDATE  = 5
    SOFTWAREITEM    = 2
    RELEASERECORD   = 5
    KNOWLEDGE       = 20
    GROUP           = 4
    QUEUE           = 4
    PRINTER         = 2
    SERVICEREQUEST  = 10
    }

$MAXIMUM = @{
    SOFTWAREUPDATE = 75
    SOFTWAREITEM   = 50
    }

$SCRIPTMAP = @{
    COMPUTER        = "Initialize-ComputerProjection.ps1"
    USER            = "Initialize-User.ps1"
    INCIDENT        = "Initialize-Incident.ps1"
    CHANGEREQUEST   = "Initialize-ChangeRequest.ps1"
    PROBLEM         = "Initialize-Problem.ps1"
    SERVICE         = "Initialize-Service.ps1"
    SOFTWAREUPDATE  = "Initialize-SoftwareUpdate.ps1"
    SOFTWAREITEM    = "Initialize-SoftwareItem.ps1"
    RELEASERECORD   = "Initialize-ReleaseRecord.ps1"
    KNOWLEDGE       = "Initialize-KnowledgeArticle.ps1"
    GROUP           = "Initialize-Group.ps1"
    QUEUE           = "Initialize-Queue.ps1"
    PRINTER         = "Initialize-Printer.ps1"
    OFFERING        = "Initialize-Offering.ps1"
    SERVICEREQUEST  = "Initialize-ServiceRequest.ps1"
    SERVICEOFFERING = "Initialize-ServiceOffering.ps1"
    REQUESTOFFERING = "Initialize-RequestOffering.ps1"
    }

$MULTIPLIERS = @{
    "tiny" = 1
    100  = 100
    500  = 500
    "1k"   = 1000
    "10k"  = 10000
    "20K"  = 20000
    "50K"  = 50000
    }

#$ORDER = "USER", "SOFTWAREUPDATE", "SOFTWAREITEM", "COMPUTER", 
#    "KNOWLEDGE", "PRINTER", "GROUP", "QUEUE", "INCIDENT",
#    "CHANGEREQUEST", "PROBLEM", "SERVICE", "RELEASERECORD"
#
$ORDER = "USER", "SOFTWAREUPDATE", "SOFTWAREITEM", "COMPUTER", 
    "KNOWLEDGE", "PRINTER", "GROUP", "QUEUE",
    "PROBLEM", 
    "SERVICE", 
    "INCIDENT",
    "CHANGEREQUEST"

# HANDLE NEW V2 FEATURES
if ( (Get-ScsmManagementPack System.Library).Version -ge "7.5.8500.0" )
{
    $ORDER += "RELEASERECORD","SERVICEREQUEST" # no service offering/request offering yet,"SERVICEOFFERING","REQUESTOFFERING"
}

$x = $MULTIPLIERS[$size]
foreach($k in $ORDER)
{
    $v = $x * $RATIOS[$k]
    if ( $v -lt $MINIMUM[$k] ) { $v = $MINIMUM[$k] }
    if ( $MAXIMUM.ContainsKey($k) -and ($v -gt $MAXIMUM[$k]) ) { $v = $MAXIMUM[$k] }
    #"{0,4} {1}" -f $v,$k
    $script = $SCRIPTMAP[$k]
    if ( $count ) { $v = $count }
    Write-Host "Creating $v Instances for $K"
    & ./$script -count $v -whatif:$whatif
    # "{0} -count {1}" -f $SCRIPTMAP[$k],$v

}
