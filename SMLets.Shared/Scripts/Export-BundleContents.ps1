[CmdletBinding(SupportsShouldProcess=$true)]
param ( 
    [Parameter(ValueFromPipelineByPropertyName=$true,Position=0,Mandatory=$true)][string[]]$fullname, 
    [Parameter()][switch]$mponly
    )
Begin
{
    # VARIABLES NEEDED BY SCRIPT
    $VerbosePreference = "continue"
    $MPTYPE   = "Microsoft.EnterpriseManagement.Configuration.ManagementPack"
    $MRESTYPE = "Microsoft.EnterpriseManagement.Configuration.ManagementPackResource"
    $OPEN     = [System.IO.FileMode]"Open"
    $READ     = [System.IO.FileAccess]"Read"

    # load the approprate assemblies
    $SMCORE = [reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")
    $PKGASM = [reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Packaging")
    add-pssnapin smcmdletsnapin -ea silentlycontinue
    # if we can't load the packaging assembly, we shouldn't continue
    if ( ! $PKGASM ) { throw "Can't load packaging dll" }
    $BUNDLEFACTORY = $PKGASM.GetType("Microsoft.EnterpriseManagement.Packaging.ManagementPackBundleFactory")
    $BUNDLEREADER = $BUNDLEFACTORY::CreateBundleReader()
    $EMG = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup LOCALHOST

    # Get some times that we need
    $TYPEOFMP    = $SMCORE.GetType($MPTYPE)
    $TYPEOFMPR   = $SMCORE.GetType($MRESTYPE)
 

    # create the function which will allow us to export the resources
    function Export-MPResource
    {
        param ( $directory, $filename, $stream )
        # just continue in the face of errors, we'll check the various
        # elements we need for nulls, etc in the script
        trap { continue }
        # we know that if HasNullStream is true, there's nothing that we can do
        # we know that if there's no filename property we can't create anything
        $outputFile = "${directory}/${filename}"
        $buffer = new-object byte[]($stream.length)
        $result = $stream.read($buffer,0,$buffer.length)
        $stream.close()
        # if the stream is empty, don't continue
        if ( $result -le 0) { return }
        # Check to be sure we should continue, this handles -whatif
        if($PSCmdlet.ShouldProcess("Create Resource '$outputFile'"))
        {
            # we have to check this because the filename may have a directory component
            # create the directory if needed. This should probably have some error
            # checking code
            $outputDir = split-path $outputFile
            if ( ! ( test-path $outputDir )) 
            { 
                new-item -type directory $outputDir | out-null 
            }
            if ( $verbose ) { Write-Verbose "Creating resource file $outputFile" }
            $fs = new-object io.filestream ("$outputFile"),OpenOrCreate
            $result = $fs.write($buffer,0,$buffer.length)
            $fs.close()
            $fs.dispose()    
            # tidy up
        }
    }
    
    # this function retrieves the filename
    function Get-MPResourceFileName ( $mp, $key )
    {
        # use reflection to get call the method
        $Method = $TYPEOFMP.GetMethod("GetResource")
        $genericMethod = $Method.MakeGenericMethod($TYPEOFMPR)
        $resource = $genericMethod.Invoke($mp,$key)
        $resource.FileName
    }

}
# for each object that is passed to the script
Process
{
    $paths = (resolve-path $fullname).path
    foreach($path in $paths)
    {
        # only act on files with the proper extension
        if ( ([io.fileinfo]$path).Extension -ne ".mpb" ) 
        { 
            write-error "`nERROR: $path must end with '.mpb'"
            continue
        }
        $mpb = $BUNDLEREADER.Read($path,$EMG)
        # if the mponly parameter is used, just return the management packs
        if ( $mponly ) 
        { 
            if ( $PSCmdlet.ShouldProcess("$fullname"))
            {
                $mpb.ManagementPacks 
            }
        }
        else
        {
            # for each management pack in the bundle, get the resources
            $mpb.ManagementPacks | %{
                $ManagementPack = $_
                $MPName = $ManagementPack.Name
                # use the export-SCSMManagementPack cmdlet to export the MP
                if ($PSCmdlet.ShouldProcess($ManagementPack.Name))
                {
                    if ( $verbose ) { Write-Verbose "Exporting MP '${MPName}'" }
                    $ManagementPack | export-SCSMManagementpack -targ $PWD
                    # now get the streams
                    $streams = $mpb.GetStreams($ManagementPack)
                    # only create the directory if there are actual streams that 
                    # we need to save off
                    if ( $streams.count -gt 0)
                    { 
                        if ( $verbose ) { Write-Verbose "   Exporting resources for '${MPName}'" }
                        # save the resources in the directory "$MPNAME_Resources"
                        $ResourceDir = "${MPName}_Resources"
                        if (!(test-path ${ResourceDir}))
                        { 
                            # create the directory if it's not there
                            new-item -type directory ${ResourceDir}|out-null
                        }
                        # we need the full path for the Export process which needs a path
                        $ResourceDirFullName = (Resolve-Path ${ResourceDir}).Path
                        # now go export the resources
                        $keys = $streams.Keys
                        foreach ( $key in $keys )
                        {
                            $ResourceFileName = Get-MPResourceFileName $ManagementPack $Key
                            Export-MPResource "$ResourceDirFullName" "$ResourceFileName" $streams.Item($key)
                        }
                    }
                }
            }  
        }
    }
}
<#
.SYNOPSIS
    Export the contents of a management pack bundle
.DESCRIPTION
    The cmdlet takes a management pack bundle file and exports the management 
    pack as well as the resources associated with the management pack. The
    management packs are placed in the current directory and the resources
    are placed in a directory named <managementpack>_Resources. Any directories
    needed by the resources are created. This means that if the resource path 
    indicated by the filename property of the resource contains a directory,
    that directory will be created as well.
.PARAMETER fullname
    The fullname of the management pack bundle file    
.PARAMETER mponly
    When this parameter is used, the cmdlet returns only the management pack
    objects which are contained within the management pack bundle file. It 
    does not extract the management packs nor the associated resources.
.PARAMETER whatif
    When -whatif is used, no contents are extracted.
.PARAMETER verbose
    When -verbose is used, the name of the management pack and resources
    are written as verbose statements.
.EXAMPLE
ls *.mpb | Export-BundleContents
.EXAMPLE
ls *.mpb | Export-BundleContents -mponly
.INPUTS
    A management pack bundle, this can be piped to the cmdlet or provided
    as a parameter value
.OUTPUTS
    When used with the 'mponly' parameter, management pack objects are
    emited. By default, no output is produced by this script.
.LINK
#>
