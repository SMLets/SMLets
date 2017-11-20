. ../DataGen/Common.ps1
$CClass = get-scsmclass -name "microsoft.windows.computer$"
$includeList = get-scsmobject -class $CClass -MaxCount 3
$excludeList = get-scsmobject -class (get-scsmclass -name system.printer)
$GroupName = (Get-Lorem 3) -replace " "
####

## The simplest group test
$includeList | new-scgroup -ManagementPackName TESTMP1 -Name $GroupName -import
for($i = 0; $i -lt 60; $i+=5)
{
    Write-Progress -Activity "Sleeping for 60 seconds for group calc execution" -Status $i -perc ( 100 * ($i/60))
    start-sleep 5
}
start-sleep 60
$global:Group = Get-SCGroup -DisplayName $GroupName
$includeListGuids = $includeList | %{ $_.id } | sort
$membersListGuids = $Group.Members.Count | %{ $_.id } | sort
if ( $Group.Members.Count -eq $includeList.Count )
{
    "PASS"
}
else
{
    "FAIL"
}
Get-SCManagementPack -Name TESTMP1 | Remove-SCManagementPack

exit

# Test with include and exclude where include and exclude are 
# different classes but all objects within include and exclude 
# are the same class
$GroupArgs = @{
    Include            = $includeList
    ManagementPackName = "TESTMP2"
    Name               = "mycompgroup1"
    Description        = "A description for you!" 
    Exclude            = $excludeList
    PassThru           = $true
    }
new-scgroup @GroupArgs


#
# Test 3 include and exclude are multiple classes and don't mix
$GroupArgs.Include = @( $includelist; $excludeList )
$GroupArgs.Exclude = get-scsmclass -name Microsoft.Windows.Peripheral |get-scsmobject -max 2
$GroupArgs.ManagementPackName = "TESTMP3"
new-scgroup @GroupArgs

$GroupArgs.Remove("Exclude")
$GroupArgs.Import = $true
$GroupArgs.Include = $includeList
$GroupArgs.ManagementPackName = "TESTMP4"
$GroupArgs.Verbose = $true
new-scgroup @GroupArgs


Get-SCManagementPack -Name TESTMP2 | Remove-SCManagementPack
Get-SCManagementPack -Name TESTMP3 | Remove-SCManagementPack
Get-SCManagementPack -Name TESTMP4 | Remove-SCManagementPack
