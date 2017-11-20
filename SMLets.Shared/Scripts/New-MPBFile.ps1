# New-MPBFile.ps1
# this takes files (.mp or .xml) and creates a .mpb file
param (
    $mpFile = $( throw "Must have mpfile" ),
    [string]$mpbname = "testmpb",
    $computername = "localhost",
    [switch]$useFileStore,
    [switch]$debug
    )

# VARIABLES NEEDED BY SCRIPT
$VerbosePreference = "continue"
$SMDLL    = "Microsoft.EnterpriseManagement.Core"
$SMPKG    = "Microsoft.EnterpriseManagement.Packaging"
$MPTYPE   = "Microsoft.EnterpriseManagement.Configuration.ManagementPack"
$MRESTYPE = "Microsoft.EnterpriseManagement.Configuration.ManagementPackResource"
$SIGTYPE  = "Microsoft.EnterpriseManagement.Packaging.ManagementPackBundleStreamSignature"
$FACTYPE  = "Microsoft.EnterpriseManagement.Packaging.ManagementPackBundleFactory"
$EMGTYPE  = "Microsoft.EnterpriseManagement.EnterpriseManagementGroup"
$OPEN     = [System.IO.FileMode]"Open"
$READ     = [System.IO.FileAccess]"Read"

if ( $debug ) { $debugpreference = "Continue" }

# make sure the appropriate assemblies are loaded.
$SMCORE      = [reflection.assembly]::LoadWithPartialName($SMDLL)
$SMPACKAGING = [reflection.assembly]::LoadWithPartialName($SMPKG)
$EMPTY       = $SMCORE.GetType($SIGTYPE)::Empty
$TYPEOFMP    = $SMCORE.GetType($MPTYPE)
$TYPEOFMPR   = $SMCORE.GetType($MRESTYPE)
$BFACTORY    = $SMPACKAGING.GetType($FACTYPE)

# Functions
# Invoke-GenericMethod
# allows scripts to call generic methods.
# arguments
#   mytype - the type inspect for the needed method
#   mymethod - the method name
#   typearguments - an array of types used by MakeGenericMethod
#   object - the object against which invoke is called
#   parameters - any parameters needed by invoke
# it returns whatever is returned by invoke
function Invoke-GenericMethod
{
    param (
        [type]$mytype, 
        [string]$mymethod, 
        $TypeArguments, 
        $object, 
        [object[]]$parameters = $null 
        )
    $Method = $mytype.GetMethod($mymethod)
    $genericMethod = $Method.MakeGenericMethod($TypeArguments)
    $genericMethod.Invoke($object,$parameters)
}

# Get-Resources
# this function retrieves resources from the MP. Because our GetResources API
# uses generics, it's a bit tricky to call
# it returns a hash table of the stream, and the name for each resource
# it takes a Management Pack object
function Get-Resources
{
    param ( $mpObject )
    invoke-GenericMethod $TYPEOFMP "GetResources" $TYPEOFMPR $mpObject | %{  
        # check to see if we could find the file
        $filename = $_.filename
        $fullname = (resolve-path $_.FileName -ea SilentlyContinue).path
        if ( ! $fullname ) 
        { 
            write-host -for red "
    WARNING: 
    Cannot find resource: $filename
    Skipping this resource, your MPB will probably not import
    Make sure that the resources are in the same directory as the MP"
        }
        else
        {
            $stream = new-object io.filestream $fullname,$OPEN,$READ
            @{ Stream = $stream; Name = $_.Name }
        }
    }
}

# Start 
# Collect all the mps to add to the mpb!
$mpfileArray = @()
foreach ( $file in $mpFile )
{
    foreach ( $item in resolve-path $file )
    {
        if ( $item.path ) 
        { 
            $mpfileArray += $item.path
        }
        else
        {
            Write-Host -for red "ERROR: Cannot find file $item, skipping" 
        }
    }
}

# Check to see if we have any management packs, if not, exit.
if ( $mpFileArray.Count -eq 0 )
{
    Write-Host -for red "Error: No files to add"
    exit
}

if ( $useFileStore )
{
    # if we're using a filestore, we need to create it by getting the files 
    # from the local disk
    $EMG = new-object Microsoft.EnterpriseManagement.Configuration.IO.ManagementPackFileStore
    $EMG.AddDirectory(([io.path]::GetDirectoryName($myInvocation.mycommand.definition) + "\Library"))
    $mpFileArray|sort-object -uniq|%{ $EMG.AddDirectory([io.path]::GetDirectoryName($_)) }
    $EMG.Directories | %{ write-debug "Add to MPFileStore: $_" }
}
else
{
    # we need a connection to the server when we start creating
    # the management pack objects
    $EMG = new-object $EMGTYPE $computername
}
# In order to create .mpb, we need to create one
# we'll use the BundleFactory for this
$BUNDLE = $BFACTORY::CreateBundle()
# we'll keep a collection of all the resources that we open
$AllResources = @()
foreach($mpfilepath in $mpfileArray)
{
    # This should handle creating mpb from a local file store. 
    # For now, just create the mp object using the EnterpriseManagementGroup
    $theMP = new-object $MPTYPE $mpfilepath,$EMG
    Write-Verbose ("Adding MP: " + $theMP.Name)
    $BUNDLE.AddManagementPack($theMP) 
    # Add the resources if any are associated with the MP
    $Resources = Get-Resources $theMP
    # Add the resources for this MP to the collection
    $AllResources += $Resources
    if ( $Resources )
    {
        $Resources  | %{ 
            Write-Verbose ("Adding stream: " + $_.Name)
            $BUNDLE.AddResourceStream($theMP,$_.Name,$_.Stream,$EMPTY) 
        }
    }
}

# WRITE THE mpb
# First we need a BundleWriter
$bundleWriter = $BFACTORY::CreateBundleWriter(${PWD})
# then we can write out the .mpb
$mpbfullpath = $bundleWriter.Write($BUNDLE,$mpbname)
write-verbose "wrote mpb: $mpbfullpath"
# Cleanup the resources
if ( $AllResources )
{
    $AllResources | %{ if ( $_.Stream ) { $_.Stream.Close(); $_.Stream.Dispose() } }
}
