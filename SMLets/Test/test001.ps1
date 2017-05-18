# this test ensures that the cmdlets that should be here are here
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $STARTTIME = [datetime]::Now
}


END
{
    $Expected = "Add-SCSMEnumeration",
    "Add-SCSMRequestOffering",
	"Export-SCManagementPack",
	"Get-SCSMRelationshipClass",
	"Get-SCSMRelatedObject",
    "Get-SCSMResource",
	"Get-SCSMRelationshipObject",
	"Get-SCSMObject",
	"Get-SCSMIncident",
    "Get-SCSMObjectTemplate",
	"Get-SCSMObjectProjection",
	"Get-SCSMRule",
	"Get-SCSMTopLevelEnumeration",
    "Get-SCSMTaskResult",
	"Get-SCSMUserRole",
	"Get-SCSMTypeProjection",
	"Get-SCSMSession",
    "Get-SCSMRunAsAccount",
	"Get-SCSMTask",
	"Get-SCSMSubscription",
	"Get-SCDWRelationshipFactTypes",
    "Get-SCDWOutriggerTypes",
	"Get-SCGroup",
	"Get-SCDWWarehouseModuleTypes",
	"Get-SCDWDimensionTypes",
    "Get-DataWarehouseConfiguration",
	"Get-SCDWMeasureTypes",
	"Get-SCDWFactTypes",
	"Get-SCManagementPack",
    "Get-SCSMClass",
	"Get-SCSMChildEnumeration",
	"Get-SCSMEnumeration",
	"Get-SCSMConfigItem",
    "Get-SCQueue",
	"Get-SCSMManagementPackReference",
	"Get-SCManagementPackElement",
	"Get-SCSMCategory",
	"Get-SCSMAnnouncement",
    "Get-SCSMObjectHistory",
    "Get-SCSMRequestOffering",
    "Get-SCSMRequestOfferingQuestion",
    "Get-SCSMServiceOffering",
    "Import-SCManagementPack",
	"New-SCSMColumn",
	"New-SCSMObjectProjection",
	"New-SCSMObject",
	"New-SCSMRelationshipObject",
    "New-SCSMView",
	"New-SCSMSession",
	"New-SCSMIncident",
	"New-SCManagementPack",
    "New-SCGroup",
	"New-SCQueue",
	"New-SCSMAnnouncement",
	"New-SCSMManagementPackReference",
	"New-SCSealedManagementPack",
    "New-SCSMRequestOffering",
    "New-SCSMRequestOfferingQuestion",
    "New-SCSMServiceOffering",
    "New-SCSMServiceRequest",
    "Remove-SCSMRequestOffering",
    "Remove-SCSMServiceOffering",
    "Remove-SCSMRelationshipObject",
	"Remove-SCSMSession",
	"Remove-SCSMSubscription",
	"Remove-SCSMObject",
	"Remove-SCSMUserRole",
    "Remove-SCGroup",
	"Remove-SCManagementPack",
	"Remove-SCQueue",
	"Set-SCSMObjectProjection",
    "Set-SCSMObjectTemplate",
	"Set-SCSMRunAsAccount",
	"Set-SCSMAnnouncement",
	"Set-SCSMIncident",
    "Set-SCSMObject",
	"Get-SCSMConnectedUser", 
	"Get-SCSMConsoleTask",
    "Get-SCSMFolder", 
	"Get-SCSMFolderHierarchy", 
	"Get-SCSMForm", 
	"Get-SCSMImage",
    "Get-SCSMLanguagePackCulture", 
	"Get-SCSMPage", 
	"Get-SCSMPageSet", 
	"Get-SCSMStringResource",
    "Get-SCSMView", 
	"Get-SCSMViewSetting", 
	"Get-SCSMViewType", 
	"Get-SCSMWhoAmI", 
	"Remove-SCSMView", 
	"Remove-SCSMEnumeration",
	"Get-SCSMUserRoleProfile",
	"New-SCSMNotificationSubscription",
	"New-SCSMUserRole",
	"New-SCSMFolder",
    "Get-SMLetsVersion"
            
    $ModuleName = "SMLets"
    if ( ! (get-module -list $ModuleName))
    {
        Out-TestLog ("FAIL:" + [datetime]::Now + ":${TESTNAME}:Module $ModuleName not found")
        # don't continue if the module is not present
        return 1
    }
    if ( ! (get-module $ModuleName))
    {
        Out-TestLog ("FAIL:" + [datetime]::Now + ":${TESTNAME}:Module $ModuleName not imported")
        # don't continue if the module is not installed
        return 1
    }
    $cmdletlist = get-command -type cmdlet -module $ModuleName|sort name|%{$_.name}
    $results = compare-object $Expected $cmdletList
    if ( $results )
    {
        Out-TestLog ("FAIL:" + [datetime]::Now + ":${TESTNAME}:Cmdlet list not correct")
        $results | %{ Out-TestLog ("  DETAIL:" + [datetime]::Now + ":$TESTNAME:$_") }
        return 1
    }
    Out-TestLog ("PASS: " + [datetime]::Now + ":$TESTNAME")
    return 0
}
