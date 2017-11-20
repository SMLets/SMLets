param ( $projectionname = "Microsoft.Windows.Computer.ProjectionType" )
function Get-Components
{
    param ( $object, $indent )
    if ( ! $object.__base.Parent -and $object.__base.Name)
    {
        write-host -for yellow $object.__base.Name
        new-object psobject -prop @{
            Indent              = ("-" * $indent * 2)
            Alias               = $object.DisplayName
            ComponentCollection = $object.ComponentCollection
            Parent              = $null
            Path                = $null
            TargetConstraint    = $object.TargetConstraint
            TargetEndpoint      = $object.TargetEndPoint
            TargetType          = $object.TargetType
            TypeProjection      = $object.TypeProjection
            ParentName          = $null
            Relationship        = $null
            Seed                = $null
            }
     }
    if( $object.Alias )
    {
        write-host -for cyan ("{0} {1}" -f ("-" * $indent*2),$object.Alias)
        new-object psobject -prop @{
            Indent              = ("-" * $indent * 2)
            Alias               = $object.Alias
            ComponentCollection = $object.ComponentCollection
            Parent              = $object.Parent
            Path                = $object.Path
            TargetConstraint    = $object.TargetConstraint
            TargetEndpoint      = $object.TargetEndPoint
            TargetType          = $object.TargetType
            TypeProjection      = $object.TypeProjection
            ParentName          = $object.ParentName
            Relationship        = $object.Relationship
            Seed                = $object.Seed
            }
    }
    if ( $object.ComponentCollection.Count -gt 0 )
    {
        foreach($o in $object.ComponentCollection )
        {
            Get-Components $o ($indent+1)
        }
    }
}

$p = get-scsmtypeprojection $projectionname
Get-Components $p 0
$p.__base.CreateNavigator().outerxml|write-host

