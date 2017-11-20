param ( $count = 5, [switch]$whatif, [switch]$verbose )
. ./Common.ps1
$computerClass = get-scsmclass System.Computer$ -verbose:$verbose
$computerList = get-scsmobject $computerClass -verbose:$verbose
$NewMPArgs = @{
    ManagementPackName = "AnMPForGroups"
    FriendlyName       = "DataGen Group Management Pack"
    PassThru           = $true
    Verbose            = $verbose
    }
Write-Progress -Act "Creating ManagementPack" -stat $newMPARgs.FriendlyName
$MP = new-scsmmanagementPack @NewMPArgs 
1..$count | %{
    $GN = get-lorem 2
    Write-Progress -Act "Creating Group" -stat $GN -perc ([int]($_/$count * 100))
    $GroupArgs = @{
        Name = $GN
        ManagementPack = $MP
        Whatif = $whatif
        Verbose = $verbose
        }
    Get-RandomListFromList $computerList ($RANDOM.Next(5,15)) | new-scgroup @GroupArgs
    }
