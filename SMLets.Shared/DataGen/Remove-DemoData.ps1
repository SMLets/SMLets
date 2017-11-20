param ( [switch]$whatif, [switch]$verbose )
"NOT READY YET"
EXIT
$classes = "^System.WorkItem.ChangeRequest$",
    "^Microsoft.AD.User$",
    "^System.SoftwareItem$",
    "^System.SoftwareUpdate$",
    "^Microsoft.AD.Printer$",
    "^Microsoft.Windows.Computer$",
    "^System.WorkItem.ReleaseRecord$",
    "^System.WorkItem.Incident$",
    "^Microsoft.SystemCenter.BusinessService$",
    "^System.WorkItem.Problem$",
    "^System.Knowledge.Article$",
    "^System.WorkItem.Problem$",
    "^System.WorkItem.Activity$"

$GetArgs = @{
    Whatif = $Whatif
    Verbose = $verbose
    }
$classes | %{
    if ( $_ -eq "Microsoft.AD.User" )
    {
        $GetArgsfilter = ""
    }
    else if ( $_ -eq "Microsoft.Windows.Computer" )
    {
    }
    get-scsmclass $_ | 
        get-scsmobject | 
        remove-scsmobject -for -whatif:$whatif -verbose:$verbose
    }
