$enum = get-scsmenumeration ServiceRequestAreaEnum$
$qu = @()
$qu += @{ Prompt = "one"; TargetPath = "Title"; Type = "string"}
$qu += @{ Prompt = "two"; TargetPath = "Notes"; Type = "string"}
$qu += @{ Prompt = "thr"; TargetPath = "UserInput"; Type = "InlineList"; ListElements = "one","two","three" }
#$qu += @{ Prompt = "fou"; TargetPath = "RequiredBy"; Type = "DateTime"}
#$qu += @{ Prompt = "fiv"; TargetPath = "Area"; Type = "List" ; EnumerationList = $enum }

$qlist = @()
foreach ( $q in $qu )
{
    $qlist += new-scsmrequestofferingquestion @q
}

$user = get-scsmuser |select -first 1

$mp = get-scsmmanagementpack jwt
$roargs = @{ 
BriefDescription          = get-lorem 6
Comment                   = get-lorem 6
DisplayName               = get-lorem 3
EstimatedTimeToCompletion = 5
ManagementPack            = $mp
Notes                     = get-lorem 12
Overview                  = get-lorem 12
Owner                     = $user
PublishDate               = get-date
PublishedBy               = $user
Questions                 = $qlist
Status                    = "System.Offering.StatusEnum.Published"
Title                     = get-lorem 4
PassThru                  = $True
}

new-scsmrequestoffering @roArgs

