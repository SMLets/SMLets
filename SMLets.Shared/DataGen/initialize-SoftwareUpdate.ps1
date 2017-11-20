param ( $count = 10, [switch]$whatif, [switch]$verbose )

. ./Common.ps1

$softwareUpdateClass = get-scsmclass System.SoftwareUpdate
$computerClass = get-scsmclass ^System.Computer$

# this is completely made up and has no real data behind it
#
$VendorList = 1..8 | %{ Get-Lorem 2 }
1..$count | %{ $current = 0 } {
    $uniqName = (Get-Lorem 2) + [guid]::NewGuid()
    Write-Progress -Activity "Creating SoftwareUpdate" -Status $UniqName -Perc ($current++/$count*100)
    @{
        AssetStatus  = "Deployed"
        DisplayName  = Get-Lorem 5
        Notes        = Get-Lorem 25
        ObjectStatus = "Active"
        Title        = $uniqName
        Vendor       = Get-RandomItemFromList $VendorList
    }
} | new-scsmobject $softwareUpdateClass -bulk -whatif:$whatif -verbose:$verbose

$softwareItemList = get-scsmobject $softwareUpdateClass
$computerList     = get-scsmobject $computerClass
$installstatuslist = get-scsmchildenumeration -enum (get-scsmenumeration installstatus$)
$relationship = get-scsmrelationshipclass System.DeviceHasSoftwareUpdateInstalled
$rr = $computerList | %{
    $computer = $_
    Write-Progress -Activity "Creating relationship for software updates" -Status $computer.PrincipalName
    1..($RANDOM.Next(5,15)) | %{
    new-object psobject -property @{
        Source = $computer
        Target = get-randomitemfromlist $softwareitemlist 
        Relationship = $relationship
        Properties = @{
            InstallStatus = get-randomitemfromlist $installstatuslist
            }
        }
    }
}
$rr | New-ScsmRelationshipObject -bulk -whatif:$whatif -verbose:$verbose
