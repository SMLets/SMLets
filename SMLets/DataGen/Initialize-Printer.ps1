param ( $count = 10, [switch]$whatif, [switch]$verbose, [string]$prefix = "Printer",[switch]$nobulk )

. ./Common.ps1

$Printer = get-scsmclass Microsoft.AD.Printer -verbose:$verbose
$computerClass = get-scsmclass "^Microsoft.Windows.Computer$"
$computerList = get-scsmobject $computerClass -verbose:$verbose

$PARGS = @{
    Class   = $printer
    Bulk    = ! $nobulk
    Whatif  = $whatif
    Verbose = $verbose
    }

1..$count | %{  
    $computer = get-randomitemfromlist $computerList
    $printername = "{0}{1:0000}" -f $prefix,$_
    $DistinguishedName = "\\{0}\{1}" -f $computer.principalname,$printername
    Write-Progress -Activity "Creating Printer" -Status $printername -Perc ($current++/$count*100)
    
    @{
        AssetNumber                  = [guid]::newguid().ToString()
        Description                  = Get-Lorem 20
        DisplayName                  = Get-Lorem 4
        DistinguishedName            = $DistinguishedName
        Fullname                     = $printername
        PrinterName                  = $printername
        ServerName                   = $computer.DisplayName
        ShortServerName              = $computer.Name
        UNCName                      = $DistinguishedName
        WhenChanged                  = [datetime]::Now.AddDays(-($RANDOM.Next(60,360)))
    }
}  | new-scsmobject @PARGS
