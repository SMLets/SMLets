$v = "SELECT 'BME' as ElementName, COU",
"SELECT 'TME' as ElementName, COU",
"SELECT 'RelationshipBME' as Elem",
"SELECT 'EntityChangeLogBME' as E",
"SELECT 'RecursiveMembership' as",
"SELECT 'JobStatus' as ElementNam"

foreach($val in $v)
{
    $tt  = ($a |?{$_.query -match $val })
    $after  = ($a |?{$_.query -match $val }).Count
    $before = ($b |?{$_.query -match $val }).Count
    new-object psobject -prop @{
        ElementName = $tt.ElementName
        After = $after
        Before = $before
        }
}

