
. ..\DataGen\Common.ps1
if ( ! (get-scsmmanagementpack -name SOTESTMP ))
{
    New-SCSMManagementPack -name SOTESTMP
}
$mp = Get-SCSMManagementPack SOTESTMP
$user = get-scsmuser | select -first 1
$now = get-date
$title = get-lorem 6
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
    }
New-SCSMServiceOffering @soArgs
$mySO = Get-SCSMServiceOffering -DisplayName $title

#### Now create a Request Offering

$enum = get-scsmenumeration ServiceRequestAreaEnum$
$qu = @()
$qu += @{ Prompt = "one"; TargetPath = "Title"; Type = "string"}
$qu += @{ Prompt = "two"; TargetPath = "Notes"; Type = "string"}
$qu += @{ Prompt = "thr"; TargetPath = "UserInput"; Type = "InlineList"; ListElements = "one","two","three" }
$qu += @{ Prompt = "fou"; TargetPath = "RequiredBy"; Type = "DateTime"}
$qu += @{ Prompt = "fiv"; TargetPath = "Area"; Type = "List" ; EnumerationList = $enum }
$qu += @{ Prompt = "six"; TargetPath = "ActualWork"; Type = "double" }
$qu += @{ Prompt = "sev"; TargetPath = "IsDowntime"; Type = "boolean" }

$qlist = @()
foreach ( $q in $qu )
{
    $qlist += new-scsmrequestofferingquestion @q
}

$user = get-scsmuser |select -first 1

$mp = get-scsmmanagementpack SOTESTMP
$rotitle = get-lorem 4
$roargs = @{ 
    BriefDescription          = get-lorem 6
    Comment                   = get-lorem 6
    DisplayName               = $roTitle
    EstimatedTimeToCompletion = 5
    ManagementPack            = $mp
    Notes                     = get-lorem 12
    Overview                  = get-lorem 12
    Owner                     = $user
    PublishDate               = get-date
    PublishedBy               = $user
    Questions                 = $qlist
    Status                    = "System.Offering.StatusEnum.Published"
    Title                     = $rotitle

    }

new-scsmrequestoffering @roArgs
$myRo = get-scsmclass requestoffering | get-scsmobject -filter "DisplayName -eq '$roTitle'"
add-SCSMRequestOffering $myRo $mySO.__base
"SO: $title"
"RO: $rotitle"

