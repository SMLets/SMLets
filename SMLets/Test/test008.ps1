BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $MPNAME = "EnumTestMP"
    $MPFILENAME = "${MPNAME}.xml"
    $TESTNAME = $MyInvocation.MyCommand
    $name = "System.WorkItem.Incident"
    $STARTTIME = [datetime]::Now
    # CREATE THE MP HERE
    function New-EnumMP
    {
@"
<?xml version="1.0" encoding="utf-8"?><ManagementPack ContentReadable="true" SchemaVersion="{0}" OriginalSchemaVersion="1.1" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <Manifest>
    <Identity>
      <ID>${MPNAME}</ID>
      <Version>1.0.0.0</Version>
    </Identity>
    <Name>Enumeration Test MP</Name>
    <References>
"@ -f $SCHEMAVERSION
$MPList = "Microsoft.SystemCenter.InstanceGroup.Library",
    "System.WorkItem.Library",
    "Microsoft.SystemCenter.Library",
    "Microsoft.Windows.Library",
    "System.Library",
    "System.WorkItem.Activity.Library",
    "System.Notifications.Library"


foreach ( $mp in $MPList )
{
    $mpi = get-scsmmanagementpack "^${mp}$"
    "      <Reference Alias=""{0}"">" -f $mp.Replace(".","_")
    "        <ID>$mp</ID>"
    "        <Version>{0}</Version>" -f $mpi.Version
    "        <PublicKeyToken>{0}</PublicKeyToken>" -f $mpi.KeyToken
    "      </Reference>"
}
@"
    </References>
  </Manifest>
  <TypeDefinitions>
    <EntityTypes>
        <EnumerationTypes>
            <EnumerationValue ID="System.WorkItem.TroubleTicket.UrgencyEnum.MediumHigh" Accessibility="Public" Parent="System_WorkItem_Library!System.WorkItem.TroubleTicket.UrgencyEnum" Ordinal="8" />
        </EnumerationTypes>
    </EntityTypes>
  </TypeDefinitions>
</ManagementPack>
"@
    }
}
END
{
    $SYSMP = get-scsmmanagementPack System.Library
    $SCHEMAVERSION = "{0}.{1}" -f $SYSMP.SchemaVersion.Major,$SYSMP.SchemaVersion.Minor
    new-EnumMP |set-content -encoding ascii ${MPFILENAME}

    try
    {
        # set-psdebug -trace 2
        $error.clear()
        import-scsmmanagementpack ${MPFILENAME}
        $incidentClass = get-scsmclass ^System.WorkItem.Incident$
        $r = new-scsmobject -pass $incidentClass @{ Id = "IR{0}"; Title = "foobar"; Urgency = "MediumHigh"; Impact = "High"}
        $id = $r.id
        $mpToRemove = get-scsmmanagementpack ${MPNAME} 
        $mpToRemove | remove-scsmmanagementpack
        remove-item ${MPFILENAME}
        start-sleep 5
        $incident = get-scsmincident -id $id
        $g = new-object guid $incident.Urgency
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }
    finally
    {
        set-psdebug -trace 0
    }
    Out-TestLog ("PASS: " + [datetime]::Now + ":$TESTNAME")

    return 0

}
