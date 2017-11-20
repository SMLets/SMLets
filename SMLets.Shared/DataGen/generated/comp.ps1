param ( [int]$count = 1, [switch]$whatif, [switch]$verbose )
BEGIN
{
    . ./Common
    $ulist = get-scsmobject "^System.User$" -filter "LastName -like %u%"

    $DName = $env:userdomain
}
END
{
    $current = 0
    $collection = @()
    while ( $current++ -lt $count )
    {
        $CName = "JWTCMP{0:0000}" -f $current
        $PName = "${CName}.${DName}.com"

        $collection += @{ 
            __CLASS  = 'Microsoft.Windows.Computer'
            __OBJECT = @{
                AssetStatus                     = "Deployed"
                DisplayName                     = $PNAME
                NetbiosComputerName             = $PNAME
                NetbiosDomainName               = $DNAME
                PrincipalName                   = $PNAME
                }
            PrimaryUser = Get-RandomItemFromList $ulist
            Custodian = Get-RandomItemFromList $ulist
            PhysicalComputer = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.Computer$)
            #OperatingSystem = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.OperatingSystem$)
            # NetworkAdapter = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.LogicalDevice$)
            # Processor = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.LogicalDevice$)
            # PhysicalDisk = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.LogicalDevice$)
            # LogicalDisk = Get-RandomItemFromList (Get-SCSMObject ^Microsoft.Windows.LogicalDevice$)
            #ImpactedWorkItem = Get-RandomItemFromList (Get-SCSMObject ^System.ConfigItem$)
            #RelatedWorkItem = Get-RandomItemFromList (Get-SCSMObject ^System.ConfigItem$)
            # FileAttachment = Get-RandomItemFromList (Get-SCSMObject ^System.FileAttachment$)
            #RelatedConfigItem = Get-RandomItemFromList (Get-SCSMObject ^System.ConfigItem$)
            #RelatedConfigItemSource = Get-RandomItemFromList (Get-SCSMObject ^System.ConfigItem$)
            # RelatedKnowledgeArticles = Get-RandomItemFromList (Get-SCSMObject ^System.Knowledge.Article$)
            
        } 
    }
    $collection | new-scsmobjectprojection Microsoft.Windows.Computer.ProjectionType -whatif:$whatif -verbose:$verbose
}


