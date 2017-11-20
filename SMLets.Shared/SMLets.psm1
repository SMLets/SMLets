#requires -version 2.0
# handy global variables
$GLOBAL:SMADLL   = ([appdomain]::CurrentDomain.getassemblies()|?{$_.location -match "System.Management.Automation.dll"}).location
$GLOBAL:SMDIR    = (Get-ItemProperty 'hklm:/software/microsoft/System Center/2010/Service Manager/Setup').InstallDirectory
$GLOBAL:SMSDKDIR = "${SMDIR}\SDK Binaries"
$GLOBAL:SMDLL    = "${SMSDKDIR}/Microsoft.EnterpriseManagement.Core.dll"
$GLOBAL:EMGTYPE  = "Microsoft.EnterpriseManagement.EnterpriseManagementGroup"
$GLOBAL:DATAGENDIR = "$psScriptRoot\DataGen"


<#
.SYNOPSIS
    Load an assembly into the current process space
.DESCRIPTION
    Provides a quick way of loading an assembly into the current process.
.PARAMETER Dll
    The assemply to load. This can be either a file name or full path
.EXAMPLE
    Import-Assembly "C:\Program Files\Microsoft System Center\Service Manager 2010\SDK Binaries\Microsoft.EnterpriseManagement.Core.dll"
.OUTPUTS
    No output
#>
function import-Assembly
{
    [CmdletBinding(SupportsShouldProcess=$true)]

    param ( 
        [parameter(Mandatory=$true,Position=0)]$dll ,
        [switch]$partial
        )
    end
    {
        $dllpath = (resolve-path $dll).path
        if ( $partial )
        {
            [reflection.assembly]::LoadWithPartialName($dll)
        }
        else
        {
            [reflection.assembly]::LoadFile($dllpath)
        }
    }
}
# load the Service Manager Core assembly
set-alias -scope global load import-assembly
load $SMDLL | out-null

<#
.SYNOPSIS
    Create a new EnterpriseManagementGroup object
.DESCRIPTION
    This cmdlet creates a connection to a Service Manager 2010 Data Access
    Service and returns the resultant EnterpriseManagementGroup object
.PARAMETER ComputerName
    The ComputerName to use when creating a connection to the 
    Service Manager 2010 Data Access Service
.EXAMPLE
    $emg = new-ManagementGroup
.OUTPUTS
a Microsoft.EnterpriseManagement.EnterpriseManagementGroup object
#>
function New-ManagementGroup
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param ( 
        [parameter(Position=0)][String]$ComputerName = "localhost" 
        ) 
    end
    {
        if($PSCmdlet.ShouldProcess($ComputerName))
        {
            new-object $EMGTYPE $ComputerName
        }
    }
}
set-alias -scope global new-mg New-ManagementGroup
<#
.SYNOPSIS
    Retrieve Service Manager 2010 class which have a specified property
.DESCRIPTION
    This cmdlet retrieves classes which have a property that equals the name 
    provided by the parameter 'name'
.PARAMETER Name
    The parameter name to retrieve from any class
    in the Service Manager 2010 system
.EXAMPLE
    get-property Domain
    retrieves ManagementPackClasses which contain the property name 'Domain'
.OUTPUTS
    A ManagementPackClass object which includes a property name that
    equals the name provided
#>
function get-SCSMproperty 
{
    [CmdletBinding()]
    param ( 
        [parameter(Position=0)][string] $name = ".*"
        ) 
    end
    {
        get-scsmclass|?{$_.getproperties()|?{$_.name -eq $name}}
    }
}

# moved to code
#function Get-SCSMClassProperty
#{
#    [CmdletBinding()]
#    param ( [string]$classname )
#    $class = get-scsmclass $classname
#    if ( $class -is [array] )
#    {
#        $class |%{$_.name}| write-host -for red
#        Throw "Too many classes, try again"
#    }
#    if ( ! $class )
#    {
#        throw "$classname not found, try again"
#    }
#    $EMOT = "Microsoft.EnterpriseManagement.Common.CreatableEnterpriseManagementObject"
#    try
#    {
#        if ( $Class.Abstract )
#        {
#            $Class.GetProperties()
#        }
#        else
#        {
#            $emo = new-object $EMOT $class.ManagementGroup,$class
#            $emo.getproperties()
#        }
#    }
#    catch
#    {
#        throw $error[0]
#    }
#}

function get-SCSMCommand
{
    [CmdletBinding()]
    param ( )
    end
    {
        get-command -module SMLets | sort-object CommandType,Noun
    }
}
# now add the PSScriptRoot to the path, this will ensure that any scripts
# in the module directory are accessible
if(!$env:path.Contains(";$psscriptroot;$psscriptroot\Scripts"))
{
	$env:path += ";$psscriptroot;$psscriptroot\Scripts"
}

set-alias -scope global Export-SCSMManagementPack Export-SCManagementPack
set-alias -scope global Get-SCSMManagementPack Get-SCManagementPack
set-alias -scope global Get-SCSMManagementPackElement Get-SCManagementPackElement
set-alias -scope global Import-SCSMManagementPack Import-SCManagementPack
set-alias -scope global New-SCSMManagementPack New-SCManagementPack
set-alias -scope global Remove-SCSMManagementPack Remove-SCManagementPack

$SMLetsTypesFile = join-path $PSScriptRoot SMLets.Types.ps1xml
update-typedata $SMLetsTypesFile -ErrorAction SilentlyContinue
$SMLetsFormatFile = join-path $PSScriptRoot SMLets.Format.ps1xml
update-formatdata $SMLetsFormatFile -ErrorAction SilentlyContinue
