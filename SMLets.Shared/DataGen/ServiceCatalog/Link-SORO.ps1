$solist = Get-SCSMServiceOffering
$sohash = $solist | %{ $h = @{} } {
    $key = "{0},{1}" -f $_.category.displayname.trim(),$_.title.trim()
    $h[$key] = $_
    } { $h }
$rolist = get-scsmclass requestoffering | get-scsmobject
$relationships = @()
$global:missing = @()
foreach($ro in $rolist )
{
    if ( ! $ro.notes ) { "skipping " + $ro.title; continue }
    $key = $ro.notes.trim()
    if ( ! $key ) { "skipping " + $ro.Title; continue }
    if ( $sohash.ContainsKey($key) )
    {
        $so = $sohash[$key]
        Write-Progress -Act ("Adding "+$ro.Title) -Stat ("to " + $so.Title)
        Add-SCSMRequestOffering -RequestOffering  $ro -ServiceOffering $so 
        # "Linking {0} to {1}" -f $ro.title, $sohash[$key].Title
    }
    else
    {
        $global:missing += $ro
        "Could not find match for " + $ro.title
    }
}
    