param ( $count = 100, [switch]$whatif, [switch]$verbose )

. ./Common.ps1

$SoftwareItem = get-scsmclass System.SoftwareItem
$computerClass = get-scsmclass ^System.Computer$

# Write-Progress -Activity "Querying WMI for Win32_Product" -Status "Get-WMIObject Win32_Product"
# WMI Takes too long to get the data, we'll simplify to use get-lorem
#$items = get-wmiobject win32_product | sort -uniq Name | Select-Object -first $count 
#$count = $items.count
#$items | %{ $current = 0 } { 
$PublisherList = 1..9 | %{ get-lorem 3; start-sleep -m 15 }
$ProductHash = @{}
$SoftwareItemCollection = @()
while($SoftwareItemCollection.Count -lt $count )
{
    $name = Get-Lorem 3
    $DisplayName = $name.trim(".") + " " + (Get-Lorem 3)
    $Version = Get-RandomVersion
    $Publisher = Get-RandomItemFromList $publisherList
    $key = "${name}${Publisher}${Version}"
    if ( $productHash.ContainsKey($key) )
    {
        continue
    }
    Write-Progress -Activity "Creating SoftwareItem" -Status $Name -Perc ($current++/$count*100)
    $SoftwareItemCollection += @{
        AssetStatus           = "Deployed"
        DisplayName           = $DisplayName
        IsVirtualApplication  = $false
        LocaleID              = 1033
        MajorVersion          = $Version.Major
        MinorVersion          = $Version.Minor
        Notes                 = Get-Lorem 12 -sent
        ObjectStatus          = "Active"
        ProductName           = $Name
        Publisher             = $Publisher
        VersionString         = "$Version"
    }
} 
$softwareItemCollection | new-scsmobject $SoftwareItem -bulk -whatif:$whatif -verbose:$verbose
$softwareList = get-scsmobject $softwareitem
$Relationship = get-scsmrelationshipclass ^System.DeviceHasSoftwareItemInstalled$
$SLcount = [int]($softwarelist.count * 2 / 3) + 1
$DirList = get-childitem $env:programfiles | ?{$_.psiscontainer}|%{$_.fullname}
$global:InstalledSoftwareItems = get-scsmobject $computerClass | %{
    $computer = $_
    Write-Progress -Activity "Creating SoftwareItem Relationship" -Status $computer
    get-randomlistfromlist $SoftwareList $SLCount | %{
        $softwareItem = $_
        new-object psobject -prop @{ 
            Relationship = $relationship
            Target = $softwareItem
            Source = $computer
            Properties = @{
                InstalledPath = Get-RandomItemFromList $DirList
                SerialNumber  = New-SN
                InstalledDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,365))
                }
            }
        }
    }
