
$ACTIVITYHASREVIEWERRELATIONSHIP   = Get-SCSMRelationshipClass System.ReviewActivityHasReviewer
$REVIEWERISUSERRELATIONSHIP        = Get-SCSMRelationshipClass System.ReviewerIsUser
$WORKITEMCREATEDBYUSERRELATIONSHIP = Get-SCSMRelationshipClass System.WorkitemCreatedByUser
$ASSIGNEDTOUSERRELATIONSHIP        = Get-SCSMRelationshipClass System.WorkItemAssignedToUser
$REVIEWERISUSERRELATIONSHIP        = Get-SCSMRelationshipClass System.ReviewerIsUser

$RANDOM = new-object System.Random

function Get-RandomVersion
{
    new-object System.Version ("{0}.{1}.{2}.{3}" -f $RANDOM.Next(1,8),$RANDOM.Next(0,15),$RANDOM.Next(20,100),$RANDOM.Next(20,100))
}
function get-RandomIPAddress
{
    BEGIN
    {
    $b = new-object byte[] 4
    }
    PROCESS
    {
    $RANDOM.NextBytes($b)
    "{0}.{1}.{2}.{3}" -f $b[0],$b[1],$b[2],$b[3]
    }
}

function new-SN
{
    BEGIN
    {
        $a1a = 1000; $a1b = 9999
        $a2a = 10; $a2b = 99
        $R = new-object System.Random
    }
    PROCESS
    {
        "{0}-{1}-{2}-{3}-{4}-{5}-{6}-{7}" -f $R.Next($a1a,$a1b),$R.Next($a1a,$a1b),$R.next($a1a,$a1b),$R.next($a1a,$a1b),$R.next($a1a,$a1b),$R.next($a1a,$a1b),$R.next($a1a,$a1b),$R.next($a2a,$a2b)
    }
}

function get-phone
{
    param ( [string]$prefix, $count = 1)
    BEGIN
    {
        $l = 10
        $pn = new-object int[] $l
        if ( $prefix )
        {
            $prefix = $prefix -replace "-"
            $l -= ([string]$prefix).Length
        }
        $byte = new-object byte[] $l
        ([int[]][string[]]("$prefix".ToCharArray())).CopyTo($pn,0)
    }
    PROCESS
    {
        $current = 0
        while($current++ -lt $count)
        {
            $random.NextBytes($byte)
            $nn = ($byte|%{[int][string]("$_"[-1])})
            @($nn).CopyTo($pn,10-$l)
            $pn = $pn|%{$_}
            "{0}{1}{2}-{3}{4}{5}-{6}{7}{8}{9}" -f $pn
        }
    }
}

function new-FileAttachmentStream
{
    param ( $count = 3 )
    $OPEN = ([io.filemode]::Open)
    $RACC = ([io.fileaccess]::Read)
    $RSHR = ([io.fileshare]::read)
    for($i = 0; $i -lt $count; $i++)
    {
        $DOCUMENT = [io.path]::GetTempFileName()
        get-lorem 60 > $DOCUMENT
        new-object io.filestream $DOCUMENT,$OPEN,$RACC,$RSHR
    }
}

function Get-LogicalDisk
{
    param ( $PrincipalName )
    if ( @($__ldisks)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__ldisks = get-wmiobject win32_logicaldisk
    }
    $__ldisks | %{
        @{
            AssetStatus                  = "Deployed"
            Compressed                   = $_.Compressed
            Description                  = $_.Description
            DeviceID                     = $_.DeviceId
            DriveType                    = $_.DriveType
            FileSystem                   = $_.FileSystem
            FreeSpace                    = [int]($_.FreeSpace/1mb)
            Name                         = $_.Name
            Notes                        = get-lorem 12
            PrincipalName                = $PrincipalName
            QuotasDisabled               = $_.QuotasDisabled
            Size                         = [int]($_.Size/1mb)
            SupportsDiskQuota            = $_.SupportsDiskQuotas
            SupportsFileBasedCompression = $_.SupportsFileBasedCompression
            VolumeName                   = $_.VolumeSerialNumber
        }
    }
}

function Get-ComputerSystem
{
    param ( $PrincipalName )
    
    if ( @($__csystem)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__csystem = get-wmiobject win32_ComputerSystem
    }
    $__csystem | %{
        @{
            DisplayName        = $PrincipalName
            HardwareId         = [guid]::NewGuid().ToString()
            Manufacturer       = $_.Manufacturer
            Model              = $_.Model
            Notes              = get-lorem 6
            NumberOfProcessors = $_.NumberOfProcessors
            PlatformType       = $_.Description
            SerialNumber       = new-SN
            SMBIOS_UUID        = new-SN
            SMBIOSAssetTag     = new-SN
        }
    }
}
function Get-PhysicalDisk
{
    param ( $PrincipalName )
    
    if ( @($__pdisks)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__pdisks = get-wmiobject win32_diskdrive
    }
    $__pdisks | %{
        $totalSectors = $_.TotalSectors
        if ( $totalSectors -gt [int]::MaxValue ) { $totalSectors = [int]::MaxValue }
        @{
            AssetStatus        = "Deployed"
            Caption            = $_.Caption
            Description        = $_.Description
            DeviceID           = $_.DeviceId
            Index              = $_.Index
            InterfaceType      = $_.InterfaceType
            Manufacturer       = $_.Manufacturer
            MediaType          = $_.MediaType
            Model              = $_.Model
            Name               = $_.Name
            Notes              = Get-Lorem 12
            PNPDeviceId        = $_.PNPDeviceId
            PrincipalName      = $PrincipalName
            SCSIBus            = $_.SCSIBus
            SCSILogicalUnit    = $_.SCSILogicalUnit
            SCSIPort           = $_.SCSIPort
            SCSITargetID       = $_.SCSITargetId
            Size               = [int]($_.Size/1mb)
            TotalCylinders     = $_.TotalCylnders
            TotalHeads         = $_.TotalHeads
            TotalSectors       = $TotalSectors
            TotalTracks        = $_.TotalTracks
            TracksPerCylinder  = $_.TracksPerCylinder
            }
        }
}
function Get-Partition
{
    param ( $PrincipalName )
    if ( @($__part)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__part = get-wmiobject win32_diskpartition
    }
    $__part | %{
        @{
            BlockSize        = $_.BlockSize
            Bootable         = $_.Bootable
            Description      = $_.Description
            DeviceID         = $_.DeviceId
            DiskIndex        = $_.DiskIndex
            DisplayName      = $_.Caption
            Name             = $_.Caption
            Notes            = Get-Lorem 12
            PrimaryPartition = $_.PrimaryPartition
            PrincipalName    = $PrincipalName
            Size             = [int]($_.Size/1MB)
            Type             = $_.Type
        }
    }
}
function Get-Processor
{
    param ( $PrincipalName )
    # this should be SMS_Processor, but that may not be present
    # unless configuration manager client is installed
    # this will do the best it can
    if ( @($__Processors)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__Processors = get-wmiobject Win32_Processor 
    }
    $__Processors | %{
        $Processor = $_
        @{
            AssetStatus     = "Deployed"
            # BrandId         =
            # CPUKey          =
            DataWidth       = $Processor.DataWidth
            Description     = $Processor.Description
            DeviceID        = $Processor.DeviceID
            Family          = $Processor.Family
            # IsMobile        = $Processor.
            IsMulticore     = &{ if ( $Processor.NumberofCores -gt 1 ) { $true } else { $false } }
            Manufacturer    = $Processor.Manufacturer
            MaxClockSpeed   = $Processor.MaxClockSpeed
            Name            = $Processor.Name
            Notes           = get-lorem 12
            # PCache          = $Processor.
            PrincipalName   = $PrincipalName
            Revision        = $Processor.Revision
            Speed           = $Processor.CurrentClockSpeed
            Type            = $Processor.ProcessorType
            Version         = $Processor.Version
        } 
    }
}
function Get-NetworkAdapter
{
    param ( $PrincipalName )
    if ( @($__NAConf)[0] -isnot [System.Management.ManagementObject] )
    {
        #write-host -for red "get network conf"
        $script:__NAConf = get-wmiobject Win32_NetworkAdapterConfiguration 
    }
    if ( @($__NA)[0] -isnot [System.Management.ManagementObject])
    {
        #write-host -for red "get network adapt"
        $script:__NA = get-wmiobject win32_networkadapter
    }
    $__NAConf | %{
        $Config = $_
        $Index = $Config.Index
        $Adapt = $__NA | ?{$_.Index -eq $Index }
        if ( ! $Adapt ) { throw "whoops"  }
        # $Adapt  = get-wmiobject win32_networkadapter -filter "Index = '$Index'"
        @{
            AdapterType        = $Adapt.AdapterType
            AssetStatus        = "Deployed"
            DefaultIPGateway   = @($Config.DefaultIPGateway) -join ", "
            Description        = $Config.Description
            DeviceID           = $Adapt.DeviceId
            DHCPEnabled        = $Config.DHCPEnabled
            DHCPServer         = $Config.DHCPServer
            DNSDomain          = $Config.DNSDomain
            Index              = $Index
            IPAddress          = @($Config.IPAddress) -join ", "
            IPEnabled          = $Config.IPEnabled
            IPSubnet           = @($Config.IPSubnet) -join ", "
            MACAddress         = $Config.MACAddress
            Manufacturer       = $Adapt.Manufacturer
            MaxSpeed           = $Adapt.MaxSpeed
            Name               = $Adapt.Name
            Notes              = get-lorem 12
            PrincipalName      = $PrincipalName
            ProductName        = $Adapt.ProductName
            ServiceName        = $Config.ServiceName
        }
    }
}
function Get-OperatingSystem
{
    param ( $PrincipalName )
    if ( @($__CS)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__CS = @(get-wmiobject win32_computersystem)[0]
    }
    if ( @($__OS)[0] -isnot [System.Management.ManagementObject] )
    {
        $script:__OS = @(get-wmiobject win32_OperatingSystem)[0]
    }
    @{
        DisplayName           = $__OS.Caption
        AssetStatus           = "Deployed"
        BuildNumber           = $__OS.BuildNumber
        CountryCode           = $__OS.CountryCode
        CSDVersion            = $__OS.CSDVersion
        Description           = $__OS.Description
        InstallDate           = $__OS.InstallDate
        Locale                = $__OS.Location
        LogicalProcessors     = $__CS.NumberOfLogicalProcessors
        MajorVersion          = ([version]($__OS.Version)).Major
        Manufacturer          = $__OS.Manufacturer
        MinorVersion          = ([version]($__OS.Version)).Minor
        Notes                 = get-lorem 12
        OSLanguage            = $__OS.OSLanguage
        OSVersion             = $__OS.Version
        OSVersionDisplayName  = $__OS.Caption
        PhysicalMemory        = [int]($__CS.TotalPhysicalMemory/1mb)
        PrincipalName         = $PrincipalName
        ProductType           = $__OS.ProductType
        SerialNumber          = $__OS.SerialNumberb
        ServicePackVersion    = "{0}.{1}" -f $__OS.ServicePackMajorVersion,$__OS
        SystemDrive           = $__OS.SystemDrive
        WindowsDirectory      = $__OS.WindowsDirectory
    }
}

function Get-Priority
{
    param ( [string]$myurgency, [string]$myimpact )
    $hunt = "${myurgency}${myimpact}"
    $PriorityData = (get-scsmobject (get-scsmclass System.WorkItem.Incident.GeneralSetting)).PriorityMatrix
    if ( $PriorityData )
    {
        $matrix = [xml]$PriorityData
    }
    $Impact  = Get-SCSMEnumeration System.WorkItem.TroubleTicket.ImpactEnum.|sort-object Ordinal
    $Urgency = Get-SCSMEnumeration System.WorkItem.TroubleTicket.UrgencyEnum.|sort-object Ordinal
    $hash = @{}
    $count=1
    foreach($U in $Urgency)
    {
        foreach($I in $Impact)
        {
            $UN = $U.DisplayName; $IN = $I.DisplayName
            $UID = $U.ID; $IID = $I.ID
            $xpath = "Matrix/U[@Id='$UID']/I[@Id='$IID']/P"
            if ( $ProrityData )
            {
                $value = $Matrix.SelectSingleNode($xpath)."#text"
            }
            else
            {
                $Value = $count++
            }
            $hash["${UN}${IN}"] = $value
        }
    }
    $hash[$hunt]
}

function Get-State
{
$States = "AL","AK","AS","AZ","AR","CA","CO","CT","DE","DC","FM","FL","GA","GU","HI","ID","IL","IN",
          "IA","KS","KY","LA","ME","MH","MD","MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ","NM",
          "NY","NC","ND","MP","OH","OK","OR","PW","PA","PR","RI","SC","SD","TN","TX","UT","VT","VI",
          "VA","WA","WV","WI","WY"
Get-RandomItemFromList $States
}
function Get-RandomItemFromList
{
param ( [Parameter(Mandatory=$true,Position=0)]$list, [Parameter(Position=1)]$favoredItem )
    if ( $favoredItem -and ($random.next(0,3) -eq 1))
    {
        $favoredItem
    }
    else
    {
        $list[$RANDOM.Next(0,$list.Count)]
    }
}

function Get-RandomListFromList
{
param ( 
    [Parameter(Mandatory=$true,Position=0)]$list, 
    [Parameter(Mandatory=$true,Position=1)][int]$count
    )
$mylist = [Collections.ArrayList]$list
$RandomList =  @()
for($i = 0; $i -lt $count -and $mylist.Count -gt 0; $i++)
{
    $r = $RANDOM.Next(0,$mylist.Count)
    $RandomList += $mylist[$r]
    $mylist.RemoveAt($r)
}
$RandomList
}

function Get-Lorem
{
    param ( [int]$count = 11, [switch]$Start, [int]$length, [switch]$word, [switch]$sentence, [int]$scount = 1)
    #$s = $Start.ToString().ToLower()
    #$URL =  "http://www.lipsum.com/feed/xml?amount=${count}&what=words&start=${s}"
    #[xml]$x = ((new-object net.webclient).downloadstring($URL))
    #$x.feed.lipsum
    $words = "consetetur", "sadipscing", "elitr", "sed", "diam", "nonumy", "eirmod",
        "tempor", "invidunt", "ut", "labore", "et", "dolore", "magna", "aliquyam", "erat", "sed", "diam", "voluptua",
        "at", "vero", "eos", "et", "accusam", "et", "justo", "duo", "dolores", "et", "ea", "rebum", "stet", "clita",
        "kasd", "gubergren", "no", "sea", "takimata", "sanctus", "est", "lorem", "ipsum", "dolor", "sit", "amet",
        "lorem", "ipsum", "dolor", "sit", "amet", "consetetur", "sadipscing", "elitr", "sed", "diam", "nonumy", "eirmod",
        "tempor", "invidunt", "ut", "labore", "et", "dolore", "magna", "aliquyam", "erat", "sed", "diam", "voluptua",
        "at", "vero", "eos", "et", "accusam", "et", "justo", "duo", "dolores", "et", "ea", "rebum", "stet", "clita",
        "kasd", "gubergren", "no", "sea", "takimata", "sanctus", "est", "lorem", "ipsum", "dolor", "sit", "amet",
        "lorem", "ipsum", "dolor", "sit", "amet", "consetetur", "sadipscing", "elitr", "sed", "diam", "nonumy", "eirmod",
        "tempor", "invidunt", "ut", "labore", "et", "dolore", "magna", "aliquyam", "erat", "sed", "diam", "voluptua",
        "at", "vero", "eos", "et", "accusam", "et", "justo", "duo", "dolores", "et", "ea", "rebum", "stet", "clita",
        "kasd", "gubergren", "no", "sea", "takimata", "sanctus", "est", "lorem", "ipsum", "dolor", "sit", "amet", "duis",
        "autem", "vel", "eum", "iriure", "dolor", "in", "hendrerit", "in", "vulputate", "velit", "esse", "molestie",
        "consequat", "vel", "illum", "dolore", "eu", "feugiat", "nulla", "facilisis", "at", "vero", "eros", "et",
        "accumsan", "et", "iusto", "odio", "dignissim", "qui", "blandit", "praesent", "luptatum", "zzril", "delenit",
        "augue", "duis", "dolore", "te", "feugait", "nulla", "facilisi", "lorem", "ipsum", "dolor", "sit", "amet",
        "consectetuer", "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod", "tincidunt", "ut", "laoreet",
        "dolore", "magna", "aliquam", "erat", "volutpat", "ut", "wisi", "enim", "ad", "minim", "veniam", "quis",
        "nostrud", "exerci", "tation", "ullamcorper", "suscipit", "lobortis", "nisl", "ut", "aliquip", "ex", "ea",
        "commodo", "consequat", "duis", "autem", "vel", "eum", "iriure", "dolor", "in", "hendrerit", "in", "vulputate",
        "velit", "esse", "molestie", "consequat", "vel", "illum", "dolore", "eu", "feugiat", "nulla", "facilisis", "at",
        "vero", "eros", "et", "accumsan", "et", "iusto", "odio", "dignissim", "qui", "blandit", "praesent", "luptatum",
        "zzril", "delenit", "augue", "duis", "dolore", "te", "feugait", "nulla", "facilisi", "nam", "liber", "tempor",
        "cum", "soluta", "nobis", "eleifend", "option", "congue", "nihil", "imperdiet", "doming", "id", "quod", "mazim",
        "placerat", "facer", "possim", "assum", "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer", "adipiscing",
        "elit", "sed", "diam", "nonummy", "nibh", "euismod", "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam",
        "erat", "volutpat", "ut", "wisi", "enim", "ad", "minim", "veniam", "quis", "nostrud", "exerci", "tation",
        "ullamcorper", "suscipit", "lobortis", "nisl", "ut", "aliquip", "ex", "ea", "commodo", "consequat", "duis",
        "autem", "vel", "eum", "iriure", "dolor", "in", "hendrerit", "in", "vulputate", "velit", "esse", "molestie",
        "consequat", "vel", "illum", "dolore", "eu", "feugiat", "nulla", "facilisis", "at", "vero", "eos", "et", "accusam",
        "et", "justo", "duo", "dolores", "et", "ea", "rebum", "stet", "clita", "kasd", "gubergren", "no", "sea",
        "takimata", "sanctus", "est", "lorem", "ipsum", "dolor", "sit", "amet", "lorem", "ipsum", "dolor", "sit",
        "amet", "consetetur", "sadipscing", "elitr", "sed", "diam", "nonumy", "eirmod", "tempor", "invidunt", "ut",
        "labore", "et", "dolore", "magna", "aliquyam", "erat", "sed", "diam", "voluptua", "at", "vero", "eos", "et",
        "accusam", "et", "justo", "duo", "dolores", "et", "ea", "rebum", "stet", "clita", "kasd", "gubergren", "no",
        "sea", "takimata", "sanctus", "est", "lorem", "ipsum", "dolor", "sit", "amet", "lorem", "ipsum", "dolor", "sit",
        "amet", "consetetur", "sadipscing", "elitr", "at", "accusam", "aliquyam", "diam", "diam", "dolore", "dolores",
        "duo", "eirmod", "eos", "erat", "et", "nonumy", "sed", "tempor", "et", "et", "invidunt", "justo", "labore",
        "stet", "clita", "ea", "et", "gubergren", "kasd", "magna", "no", "rebum", "sanctus", "sea", "sed", "takimata",
        "ut", "vero", "voluptua", "est", "lorem", "ipsum", "dolor", "sit", "amet", "lorem", "ipsum", "dolor", "sit",
        "amet", "consetetur", "sadipscing", "elitr", "sed", "diam", "nonumy", "eirmod", "tempor", "invidunt", "ut",
        "labore", "et", "dolore", "magna", "aliquyam", "erat", "consetetur", "sadipscing", "elitr", "sed", "diam",
        "nonumy", "eirmod", "tempor", "invidunt", "ut", "labore", "et", "dolore", "magna", "aliquyam", "erat", "sed",
        "diam", "voluptua", "at", "vero", "eos", "et", "accusam", "et", "justo", "duo", "dolores", "et", "ea",
        "rebum", "stet", "clita", "kasd", "gubergren", "no", "sea", "takimata", "sanctus", "est", "lorem", "ipsum" 
     
    $random = new-object System.Random
    if ( $word )
    {
        start-sleep -mil 15
        $w = $words[$random.Next($words.length-1)]
        [char]::ToUpper($w[0]) + $w.substring(1)
        return
    }
    if ( $Start ) 
    { 
        $string = "lorem ipsum dolor sit amet "
        $total = $count - 5
    }
    else
    {
        $total = $count
    }
    if ( $length )
    {
        #write-host -for red "Calling for: $length"
        $nextword = $words[$random.Next($words.length - 1)] + " "
        while ( ($string.length + $nextword.length) -lt $length )
        {
            $string += $nextword
            $nextword = $words[$random.Next($words.length - 1)] + " "
        }
    }
    else
    {
    $NeedCap = $true
    1..$total | %{ $currentCount = 0; [int]$slen = $total / $scount } {
            $nextword = $words[$random.Next($words.length - 1)]
            if ( $NeedCap )
            {
                $nextword = [Globalization.CultureInfo]::CurrentCulture.TextInfo.ToTitleCase($nextword)
            }
            $string += $nextword
            $addDot = ++$currentCount % $slen
            if ( ($scount -ne 1) -and ( $addDot -eq 0)) 
            { 
                $string += ". " 
                $NeedCap = $true
            }
            else 
            { 
                $string += " " 
                $NeedCap = $false
            }
        } 
    }
    $string = ${string}.trim()
    if ( $sentence ) { $string += "." }
    [char]::ToUpper($string[0]) + $string.Substring(1)
}

function Get-InstanceHash
{
param ( 
    [parameter(Mandatory=$true)]$id,
    [parameter(Mandatory=$false)][int]$sequence,
    [parameter(Mandatory=$false)]$stage, 
    [parameter(Mandatory=$false)]$status 
    )
$h = @{
    Id = $id
    Title = get-lorem 5
    Description = Get-Lorem 24
}
if ( $sequence ) { $h['SequenceId'] = $sequence }
if ( $stage )    { $h['Stage']      = $stage    }
if ( $status )   { $h['Status']     = $status   }
$h
}

# build an instance on the fly
function new-instance
{
    param ( $Name , $class, [switch]$commit, [switch]$objectonly)
    if ( ! $class )
    {
        $class = get-scsmclass "^${name}$"
    }

    if (! $class ) { throw Could not find class }

    $RANDOM = new-object random
    $properties = $class | get-smproperty
    $hash = @{}
    foreach($property in $properties )
    {
        $name = $property.Name
        if ( $name -eq "DisplayName") { continue }
        switch -exact ( $property.type )
        {
            "richtext"  {

                break;
                }
            "String"   { 
                if ( $property.AutoIncrement )
                {
                    $hash[$name] = "${classname}{0}"
                }
                elseif ( $property.Key )
                {
                    $hash[$name] = [guid]::NewGuid().ToString()
                }
                else
                {
                    $min = $property.Minlength
                    if ( $Min -lt 10 ) { $min = 10 }
                    $hash[$name] = get-lorem -length ($RANDOM.Next($Min, $property.MaxLength))
                }
                break 
                }
            "datetime" { 
                $hash[$name] = [datetime]::Now.AddHours($RANDOM.NEXT(-180,-90))
                break 
                }
            "guid" {
                $hash[$name] = [guid]::NewGuid()
                break
                }
            "bool"  { 
                $hash[$name] = [bool]$RANDOM.Next(0,2)
                break 
                }
            "int" {
                $hash[$name] = $RANDOM.Next($property.MinValue,$property.MaxValue)
                break
                }
            "double" {
                $hash[$name] = $RANDOM.Next(-1mb,1mb)/1001.1
                break
                }
            "binary" {
                # this is usually a file handle - for our purposes, we'll not bother
                # $hash[$name] = $RANDOM.Next(-1mb,1mb)/1001.1
                if ( $property.Required -or $property.Key )
                {
                    throw "no data available for binary type"
                }
                break
                }
            default    {
                if ( $property.SystemType -match "Enum" )
                {
                    $elist = get-scsmchildenumeration -en (get-scsmenumeration -id $property.EnumType.id)
                    $hash[$property.name] = $elist[$RANDOM.NEXT(0,$elist.count)]
                }
                else
                {
                    write-host -for yellow $property.type not handled
                }
            }
        }
    }
    if ( $commit )
    {
        new-scsmobject -class $class $hash
    }
    if ( $objectonly )
    {
        new-scsmobject -class $class $hash -nocommit
    }
    else
    {
        $hash
    }
}
