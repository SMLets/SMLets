param ( $filter = $true )

if ( $filter -eq $true )
{
}
elseif ( $filter -match "(.*) (-like|-eq|-ne|) (.*)" )
{
    $filter = $filter -replace "%","*"
    $filter = '$_.' + $filter
}
else
{
    throw "bad filter $filter"
}
$sb = $executioncontext.InvokeCommand.NewScriptBlock($filter)
get-scsmobject -class (get-scsmclass -name System.ConfigItem) -filter "ObjectStatus -eq '47101e64-237f-12c8-e3f5-ec5a665412fb'" | ? $sb | %{
    $_.psobject.typenames[0] = "EnterpriseManagementObject#DeletedObject"
    $_ 
    }

