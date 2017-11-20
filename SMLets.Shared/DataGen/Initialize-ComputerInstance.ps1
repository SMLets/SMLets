param ( [int]$count = 100, [switch]$whatif, [switch]$verbose, [switch]$progress, $domain )


. ./Common.ps1

$class = get-scsmclass Microsoft.Windows.Computer$
$Status = get-scsmenumeration System.ConfigItem.AssetStatusEnum.Deployed
if ( $domain )
{
$DName = $domain
{
else
{
$DName = $env:userdomain
}
$ARGUMENTS = @{
    Class   = $class
    Whatif  = $whatif
    Verbose = $verbose
    }
$currentCount =0
1..$count | %{
    $CName = "Computer{0:0000}" -f $_
    $PName = "${CName}.${DName}.com"
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
    @{
        AssetStatus = $Status
        DisplayName = $PName
        IPAddress = get-randomIPAddress
        IsVirtualMachine = [bool]($RANDOM.Next(0,2))
        LastInventoryDate = [datetime]::Now.AddDays(-($Random.Next(30,365)))
        NetbiosComputerName = $PName
        NetbiosDomainName = $DName
        OffsetInMinuteFromGreenwichTime = $RANDOM.Next(0,24)*60
        PrincipalName = $PName
        }
} | new-scsmobject @ARGUMENTS

