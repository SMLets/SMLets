param ( [int]$count = 100, [switch]$whatif, [switch]$verbose, [string]$domain, [switch]$nobulk, [switch]$force )

. ./common.ps1
$class = get-scsmclass Microsoft.AD.User$
if ( $domain )
{
$DName = $domain
}
else
{
$DName = $env:userdomain
}
$userdb = get-scsmuser
if ( ($userdb.count -gt $count) -and ! $force )
{
    Write-Host "$count users already exist, use -force to add more"
    exit
}
$usernames = @{}
while ( $userNames.Keys.count -lt $count )
{
    $FName = Get-Lorem -word
    $LName  = Get-Lorem -word
    $usernames["${FName}:${LName}"]++
    Write-Progress -Activity "Generating unique users" -Stat "${FName} ${LName}" -per ( $usernames.keys.count / $count * 100 )
}
$UserArgs = @{
    bulk    = ! $nobulk
    class   = $class 
    whatif  = $whatif 
    verbose = $verbose
    }
$userNames.Keys | %{ $cur = 1; $count = $userNames.Keys.Count } { 

    $F,$L = $_.split(":")
    $n = "${F} ${L}"
    write-progress -Activity "Creating Users" -Status $n -perc ($cur++/$count * 100)
    @{
        UserName = "${F} ${L}"
        FirstName = $F
        LastName = $L
        Domain = $DName
        DisplayName = "${F} ${L}"
        DistinguishedName = "CN=${F} ${L},CN=Users,DC=${DName},DC=com"
        UPN = "${F}.${L}@${DName}.com"
        Department = Get-Lorem -word
        BusinessPhone = Get-Phone 
        BusinessPhone2 = Get-Phone 
        HomePhone = Get-Phone 
        HomePhone2 = Get-Phone 
        Fax = Get-Phone 
        Mobile = Get-Phone 
        City = Get-Lorem -word
        State = Get-State
        Zip = "{0:00000}" -f $RANDOM.Next(10000,98000)
    }
}  | new-scsmobject @UserArgs
