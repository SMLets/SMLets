param ( $projection )

function get-objecthash
{
    param ( $class )
    $PropertyNames = $class | get-smproperty | %{$_.name}
    $ml = $PropertyNames | %{ $ml = 0 } { if ( $_.Length -gt $ml) {$ml = $_.Length } } {$ml }
    
   "__CLASS  = '" + $Class.Name + "'`n"
   "   __OBJECT = @{`n"
   $PropertyNames |%{ "       {0,-$ml} = `n" -f $_ }
   "       }`n"
}

function Get-Components
{
    param ( $projection )
    $projection.ComponentCollection|%{
        $Alias = $_.Alias 
        $Global:Component = $_
        if ( $Component.TargetConstraint )
        {
            $global:TargetType = $component.TargetConstraint
        }
        else
        {
            $global:TargetType = get-scsmclass -id $Component.Relationship.Target.Type.id
        }
        #write-host -for red "::::: " $TargetType
        $TName = $TargetType.Name
        $TPropertyNames = $TargetType | get-smproperty | %{$_.name}
        if ( [bool]($component.Relationship.GetBaseTypes()|?{$_.name -match "Hosting"}))
        {
             $v = "Get-RandomItemFromList `$InstanceCollection['${TName}']"
        }
        elseif ( [bool]($component.Relationship.GetBaseTypes()|?{$_.name -match "Membership"}))
        {
             $global:tto = (Get-ObjectHash $TargetType)
             #$v = "@{{  `n   {0} }}" -f $tto -join ""  # = "@{{ {0} }}" -f ($tto -join "")
             $v = "@{ `n    $tto }"
        }
        elseif ( [bool]($_.Relationship.GetBaseTypes()|?{$_.name -match "Reference"}))
        {
             $v = "Get-RandomItemFromList `$InstanceCollection['${TName}']"
        }
        else
        {
            $v = (Get-ObjectHash $TargetType)
        }
        ("    {0,-26} = " -f $Alias ) + $v
    }
}

# collect the various types of instances that this project has
function Get-InstanceCollections
{
    param ( $projection )
    '$InstanceCollection = @{}'
    $list = @()
    foreach($component in $projection.ComponentCollection )
    {
        #write-host -for cyan $component.alias
        #if ( ! $component ) { continue }
        if ( $Component.TargetConstraint )
        {
            $TargetType = $component.TargetConstraint
            #write-host -for magenta $TargetType
        }
        else
        {
            $TargetType = $component.TargetType
            #$TargetType = get-scsmclass -id $Component.Relationship.TargetType.id
            #write-host -for green $TargetType.Name
            #Write-Host -for yellow (get-scsmclass -id $component.relationship.target.Type.id).Name
        }
        #elseif ( $Component.Relationship.TargetType.id )
        #{
        #    $TargetType = get-scsmclass -id $Component.Relationship.TargetType.id
        #}

        if ( [bool]($component.Relationship.GetBaseTypes()|?{$_.name -match "Hosting"}))
        {
            #write-host -for red HOSTING $Component.Relationship.Name
             $list += $TargetType.Name
        }
        elseif ( [bool]($component.Relationship.GetBaseTypes()|?{$_.name -match "Membership"}))
        {
            #write-host -for red MEMBERSHIP $Component.Relationship.Name
        }
        elseif ( [bool]($component.Relationship.GetBaseTypes()|?{$_.name -match "Reference"}))
        {
            #write-host -for red REFERENCE $Component.Relationship.Name
             $list += $TargetType.Name
        }
    } 
    $list = $list | sort -uniq | %{ "${_}" }
    '$list = "' + ($list -join """,`n   """) + '"'
    '$list | %{ $InstanceCollection["$_"] = get-scsmobject "^${_}$" }'
    '$missing = $list | ?{ ! $InstanceCollection["$_"] }'
    'if ( $missing ) { write-host -for red "The following classes have no instances:" ; $missing | %{ write-host -for red "  $_" } }'
    'throw "Cannot continue"'
}
#
# MAIN
#
". ./Common.ps1"

if ( $projection -is [string] )
{
    $projection = get-scsmtypeprojection $projection
}
$SeedType = $projection.TargetType
Get-InstanceCollections $projection

'$projectionHash = @{ '
'    ' + (get-objectHash $seedType)
Get-Components $projection

   "}`n"
   '$projection | new-scsmobjectprojection $projection $projectionHash'
