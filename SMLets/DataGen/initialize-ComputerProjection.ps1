param ( [int]$count = 100, [switch]$whatif, [switch]$verbose, [switch]$progress, [string]$computerPrefix = "Computer", [string]$domain, [switch]$noBulk )

. ./Common.ps1

$userClass = get-scsmclass "^Microsoft.AD.User$" -verbose:$verbose
$userList = get-scsmobject $userClass -verbose:$verbose
$computerClass = get-scsmclass "^Microsoft.Windows.Computer$" -verbose:$verbose
$projectionType = get-scsmtypeprojection Microsoft.Windows.Computer.ProjectionType -verbose:$verbose
$Status = get-scsmenumeration System.ConfigItem.AssetStatusEnum.Deployed -verbose:$verbose
if ( $domain )
{
$DName = $domain
}
else
{
$DName = $env:userdomain
}
$ARGUMENTS = @{
    Type     = "Microsoft.Windows.Computer.ProjectionType"
    Whatif   = $whatif
    Bulk     = ! $nobulk
    Verbose  = $verbose
    }
$currentCount =0
2..$count | %{
    $CName = "${ComputerPrefix}{0:0000}" -f $_
    $PName = "${CName}.${DName}.net"
    if ( $progress )
    {
        $currentCount++
        $perc = [int](($currentCount/$count)*100)
        Write-Progress -Activity "Creating Instance" -Status $CName -Percent $perc
        if ( $currentCount -eq $count )
        {
            Write-Progress -Activity "Committing" -status "$count instances" -perc 100
        }
    }
    $seed = @{
        AssetStatus         = $Status
        DisplayName         = $PName
        IPAddress           = get-randomIPAddress
        IsVirtualMachine    = [bool]($RANDOM.Next(0,2))
        LastInventoryDate   = [datetime]::Now.AddDays(-($Random.Next(30,365)))
        NetbiosComputerName = $PName
        NetbiosDomainName   = $DName
        OffsetInMinuteFromGreenwichTime = $RANDOM.Next(0,24)*60
        PrincipalName       = $PName
        }
    $seedobj = new-scsmobject $computerClass -nocommit $seed -verbose:$verbose
    @{
        __CLASS          = "Microsoft.Windows.Computer"
        __OBJECT         = $seed
       # PhysicalComputer = 
       OperatingSystem  = Get-OperatingSystem | %{
           @{
           __CLASS  = "Microsoft.Windows.OperatingSystem"
           __OBJECT = [hashtable]$_
           }
       }
       NetworkAdapter   = Get-NetworkAdapter | %{
           @{
           __CLASS  = "Microsoft.Windows.Peripheral.NetworkAdapter"
           __OBJECT = [hashtable]$_
           }

       }
       Processor        = Get-Processor | %{
           @{
           __CLASS  = "Microsoft.Windows.Peripheral.Processor"
           __OBJECT = [hashtable]$_
           }
       }
       PhysicalDisk     = Get-PhysicalDisk | %{
           @{
           __CLASS  = "Microsoft.Windows.Peripheral.PhysicalDisk"
           __OBJECT = [hashtable]$_
           }
       }
       LogicalDisk      = Get-LogicalDisk | %{
           @{
           __CLASS  = "Microsoft.Windows.Peripheral.LogicalDisk"
           __OBJECT = [hashtable]$_
           }
        }
        PrimaryUser      = Get-RandomItemFromList $userList
        Custodian        = Get-RandomItemFromList $userList
    }
} | new-scsmobjectprojection @ARGUMENTS
