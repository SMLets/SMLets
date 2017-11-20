param ( $filename = "ServiceCatalog.csv", $MPNAME = "ServiceOfferings", [switch]$clobber )

# BULK CREATE Service Offerings Categories, Service Offerings, Request Offerings
$global:streamCollection = @()
$OPEN = ([io.filemode]::Open)
$RACC = ([io.fileaccess]::Read)
$RSHR = ([io.fileshare]::read)
function New-IconStream
{
    param ( $file ) 
    $fullname = "${PWD}/${file}.jpg"
    new-bitmap -filename "$fullname"
    $str = new-object io.filestream "${fullname}",$OPEN,$RACC,$RSHR
    $global:streamCollection += $str
    $str
}
function Get-IconStream
{
    param ( $name )
    if ( test-path $name )
    {
        $fullname = (resolve-path $name).path
    }
    else
    {
    $fullname = (resolve-path "..\Portal Images\$name").path
    }
    $str = new-object io.filestream "${fullname}",$OPEN,$RACC,$RSHR
    $global:streamCollection += $str
    $str
}

$iconlist = get-childitem '..\Portal Images' -fil *.png
$IconRandom = new-object System.Random
function Get-RandomIcon
{
    $offset = $IconRandom.Next(0,$iconlist.Count)
    $iconlist[$offset].FullName
}

# fill in the missing cells of the spreadsheet
$newCategory = $true
$data = import-csv $filename |%{ 
    if ( $_.Category )  { $Category = $_.Category  } 
    else { $_.Category = $Category  }
    if ( $_."Service Offering" ) { $ServiceOffering = $_."Service Offering" }
    else { $_."Service Offering" = $ServiceOffering }
    if ( $_."SO Overview" ) { $SOOverview = $_."SO Overview" }
    else { $_."SO Overview" = $SOOverview }
    if ( $_."SO Description" ) { $SODescription = $_."SO Description" }
    else { $_."SO Description" = $SODescription }
    # if ( $_."SO Icon" ) { $SOIcon= $_."SO Icon" } else { $_."SO Icon" = $SOIcon}
    $_ 
}
$categories = $data | group category | %{$_.name }
$SOs = $data | group "Service Offering" | %{$_.name}
$SO_Cats = $data | group "Service Offering","Category"|%{$_.name}
$SOCATHASH = $data | %{ $h =@{}}{
    $k = "{0}, {1}" -f $_."Service Offering",$_.Category
    if ( ! $h.ContainsKey($k)) { $h[$k] = $_  } 
    } {$h}

# the MP to store it all

$MP = get-scsmmanagementpack "^${MPNAME}$"
if ( ! $mp )
{
    New-SCManagementPack -ManagementPackName $MPNAME -FriendlyName $MPNAME -DisplayName $MPNAME -version 7.5.1354.1
    $MP = get-scsmmanagementpack "^${MPNAME}$" 
}
else
{
    if ( $clobber )
    {
        $MP | Remove-SCSMManagementPack
        New-SCManagementPack -ManagementPackName $MPNAME -FriendlyName $MPNAME -DisplayName $MPNAME -version 7.5.1354.0
        $MP = get-scsmmanagementpack "^${MPNAME}$" 
    }
}

$SOParentCategoryEnum = Get-SCSMEnumeration System.ServiceOffering.CategoryEnum$
$ordinal = 10
$currentCat = 0
foreach($category in $categories)
{
    $name = $category -replace " ","."
    $catargs = @{
        Ordinal = $ordinal
        ManagementPack = $MP
        Name = $name
        DisplayName = $category
        Parent = $SOParentCategoryEnum
        WhatIf = $false
    } 
    $ordinal += 10
    if ( ! (get-scsmenumeration "^${name}$"))
    {
        $perc = ($currentCat++ / @($categories).count) * 100
        Write-Progress -Activity "Creating Category" -Status $catargs.DisplayName -perc $perc
        "Create {0}" -f $catargs.DisplayName 
        Add-SCSMEnumeration @catargs
    }
}

$SOProjectionType = Get-SCSMTypeProjection System.ServiceOffering.ProjectionType

$SOClass = get-scsmclass ServiceOffering
$IDTYPE = [Microsoft.EnterpriseManagement.Configuration.ExtensionIdentifier]

$ARGUMENTS = @{
    Type     = "System.ServiceOffering.ProjectionType"
    Whatif   = $false
    Bulk     = $true
    Verbose  = $false
    Debug    = $false
    }

$pubStatus = get-scsmenumeration system.offering.statusenum.published
$soCollection = @()
$current= 0
foreach ( $SC in $SO_Cats )
{

    $OName = [Guid]::NewGuid().ToString("N");
    $SODN,$CatDN = $SC.split(",")
    $SODN = $SODN.Trim()
    $CatDN = $CatDN.Trim()
    $SON = $SODN -replace "[()]" -replace " ","."
    $CatN = $CatDN -replace " ","."
    # "$SON => $SODN | $CatN => $CatDN"
    $mydata = $SOCATHASH[$SC]
    $Category = get-scsmenumeration "^${CatN}$"
    [string]$SOTYPEID = ${IDTYPE}::CreateTypeIdentifier("ServiceOffering")
    $SOName = "Offering.${OName}"
    $ID = ${IDTYPE}::CreateObjectIdentifier($MP, $SOName, $SOTYPEID)
    
    $ICON = $mydata."SO Icon"
    # random Icon generator
    if ( ! $ICON ){ $ICON = Get-RandomIcon } # else { write-host using icon $icon }

    $ownerName = $mydata."RO Owner"
    $Owner = get-scsmclass microsoft.ad.user$ | get-scsmobject -filter "UserName -eq '$OwnerName'"

    $seed = @{
        Category = $Category
        Status = $pubStatus
        ID = $ID.ToString()
        Title = $SODN
        DisplayName = $SODN
        Domain = $MP.Name
        PublishDate = [datetime]::Now
        Path = $OName
        Notes = "$Category"
        BriefDescription = $mydata."SO Description"
        Overview = $mydata."so overview"
        CultureName = [string]::Empty
        Image = Get-IconStream $ICON
        }    
    $soCollection += @{
        __CLASS = "System.ServiceOffering"
        __OBJECT = $seed
    #    PublishedBy = $Owner
    #   Owner = $Owner
    } 
    $perc = $current++ / $SO_Cats.Count * 100
    Write-Progress -Status "Adding Service Offering" -Activity "$SODN" -perc $perc
}
Write-Progress -Status "Commiting" -Activity "Service Offerings" -perc $perc
$soCollection| new-scsmobjectprojection @ARGUMENTS 
$streamCollection | %{ $_.close(); $_.dispose()}
