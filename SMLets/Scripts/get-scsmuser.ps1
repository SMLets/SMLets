[CmdletBinding(DefaultParameterSetName="username")]
param ( 
    [Parameter(ParameterSetName="username")]$username = '*', 
    [Parameter(ParameterSetName="first")]$first,
    [Parameter(ParameterSetName="last")]$last,
    [Parameter(ParameterSetName="filter")]$filter = $null
    )

if ( $username -ne $null )
{
    $filter = "UserName -like $username"
}
elseif ( $first -ne $null )
{
    $filter = "FirstName -like $first"
}
elseif ( $last -ne $null )
{
    $filter = "FirstName -like $first"
}
elseif ( $filter -ne $null )
{
    ;
}
Get-SCSMObject -class (get-scsmclass -name ^Microsoft.AD.User$) -filter $filter
