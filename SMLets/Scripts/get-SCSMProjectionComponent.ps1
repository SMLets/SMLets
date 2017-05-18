[CmdletBinding()]
param (
    [Parameter(Mandatory=$true,ValueFromPipeline=$true)]$projection
    )
Process
{
    if ( $projection.__base -isnot "Microsoft.EnterpriseManagement.Configuration.ManagementPackTypeProjection" )
    {
        throw "$projection is not a projection"
    }
    ,$projection.ComponentCollection
}
