$PriorityData = (get-scsmobject System.WorkItem.Incident.GeneralSetting).PriorityMatrix
if ( $PriorityData )
{
    $matrix = [xml]$PriorityData
}
$Impact  = Get-SCSMEnumeration System.WorkItem.TroubleTicket.ImpactEnum. | sort Ordinal
$Urgency = Get-SCSMEnumeration System.WorkItem.TroubleTicket.UrgencyEnum. | sort Ordinal
$count=1
foreach($U in $Urgency)
{
    $UOrdinal = $U.Ordinal
    foreach($I in $Impact)
    {
        $IOrdinal = $I.Ordinal
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
        new-object psobject | 
            add-member -pass NoteProperty UrgencyImpact "${UN}${IN}" |
            add-member -pass NoteProperty Value $value |
            add-member -pass NoteProperty Ordinal ($UOrdinal * $IOrdinal)
    }
}
