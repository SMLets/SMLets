$mp = Get-SCSMManagementPack SOTESTMP
if ( ! $mp )
{
    new-scsmmanagementpack -managementpackname SOTESTMP
    $mp = Get-SCSMManagementPack SOTESTMP
    if ( ! $mp ) { throw "could not create MP 'SOTESTMP'" }
}
$user = get-scsmuser | select -first 1
$now = get-date
$title = "wacky2: $now"
$soArgs = @{
    Title               = $title
    CostInformation     = "Cost Information $now"
    CostInformationLink = "http://www.microsoft.com"
    BriefDescription    = "Brief Description $now"
    Overview            = "Overview $now"
    ManagementPack      = $mp 
    DisplayName         = $title
    Notes               = "Notes $now"
    SLAInformation      = "SLAInfo $now"
    SLAInformationLink  = "http://www.bing.com/"
    comment             = "COMMENT: $title"
    owner               = $user 
    PublishedBy         = $user
    PublishDate         = $now
    Category            = "General"
    Status              = "Published"
    verbose             = $true
    RequestOffering     = get-scsmclass requestoffering | get-scsmobject -filter "Title -eq 'my SR'"
    }
New-SCSMServiceOffering @soArgs


