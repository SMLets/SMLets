$sos = get-scsmserviceoffering
$ros = get-scsmclass requestoffering | get-scsmobject
$myso = $sos|?{$_.title -match "wack"}
add-scsmrequestoffering $ros[0] $myso  -confirm

