param ( $count = 5,  [switch]$whatif, [switch]$verbose )
. ./Common.ps1
$class = get-scsmclass System.WorkItem.Incident$
Get-SCSMManagementPack anmpforqueues | remove-scsmmanagementpack
$NewMPArgs = @{
    ManagementPackName = "AnMPForQueues"
    FriendlyName       = "DataGen Queue Management Pack"
    Verbose            = $verbose
    }
Write-Progress -Act "Creating ManagementPack" -stat $newMPARgs.FriendlyName
$statusList = get-scsmchildenumeration -enum (get-scsmenumeration incidentstatusenum$)
new-scsmmanagementPack @NewMPArgs
$MP = get-scsmmanagementpack AnMPForQueues
1..$count | %{
    $QN = get-lorem 2
    $status = get-randomitemfromlist $statusList
    Write-Progress -Act "Creating Queue" -stat $QN -perc ([int]($_/$count * 100))
    $QueueArgs = @{
        Name           = $QN
        Description    = get-lorem 3
        Class          = $class
        ManagementPack = $MP
        Filter         = "Status -eq $status"
        Whatif         = $whatif
        Verbose        = $verbose
        }
    new-scqueue @QueueArgs
    }
