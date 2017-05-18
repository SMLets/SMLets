#requires -version 2.0

param ( 
    [parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)]
    [string[]]$fullname 
    )

BEGIN
{
# GLOBAL REFERENCES
# $SMDIR
$NS = "Microsoft.EnterpriseManagement"
[reflection.assembly]::LoadWithPartialName("${NS}.Core")|out-null
$EMG = new-object "${NS}.EnterpriseManagementGroup" localhost
$MPSType = "${NS}.Configuration.IO.ManagementPackFileStore"
$MPType  = "${NS}.Configuration.ManagementPack"
}

PROCESS
{
    $fullname | %{ 
        $path = (resolve-path $_).path
        $FInfo = [io.fileinfo]$path
        $dir = $FInfo.DirectoryName
        $fil = $FInfo.FullName
        $error.clear()
        try
        {
            $MP = new-object $MPType $fil,$EMG
            $MP.Verify() 
        }
        catch { ; }
        if ( $error.count ) 
        { 
            $Verified = $false 
            $msgs = $error|%{$_.Exception;$_.Exception.InnerException}
            $ErrorMessage = $msgs -join "`n"
        } 
        else 
        { 
            $ErrorMessage = "No Errors" 
            $Verified = $true 
        }
        $PSO = new-object PSObject
        $PSO.psobject.typenames[0]=  "Microsoft.EnterpriseManagement.Configuration.ManagementPack.CustomVerification"
        $PSO | add-member NoteProperty Verified $Verified
        $PSO | add-member NoteProperty Name $MP.Name
        $PSO | add-member NoteProperty FullName $fil
        $PSO | add-member NoteProperty Error $error.clone()
        $PSO | add-member NoteProperty ErrorMessage $ErrorMessage
        $PSO
    }
}

<#
.SYNOPSIS
    Verify the integrity of a management pack
.DESCRIPTION
    The cmdlet attempts to create a management pack object based on the
    provided file and then calls the verify method to determine whether
    the management pack is valid.
.PARAMETER fullname
    The fullname of the management pack
.EXAMPLE
ls wf*.xml|test-ManagementPack
Verified Name             FullName
-------- ----             --------
True     WF.NoWorkflow    C:\Program Files\System Center Management Packs\wf.NoWorkflow.xml
True     WF.Simple        C:\Program Files\System Center Management Packs\wf.Simple.xml
True     WF.SingleCmdTask C:\Program Files\System Center Management Packs\wf.SingleCmdTask.xml
True     WF.SingleTask    C:\Program Files\System Center Management Packs\wf.SingleTask.xml

.INPUTS
    Output from get-childitem
    Any object which has a fullname property which represents a management pack
.OUTPUTS
    A custom object which contains the following:
        Verified - A boolean indicating whether the MP was verified
        Name     - The name of the management pack
        FullName - The path to the management pack
        Error    - Errors produced while verifying the management pack
.LINK
    Export-ManagementPack
    Get-ManagementPack
    Import-ManagementPack
    New-ManagementPack
    New-SealedManagementPack
    Remove-ManagementPack

#>
