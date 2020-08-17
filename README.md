# SCSM PowerShell Cmdlets 

<b>Warning:</b> this project is not actively development. Only critical reported bugs will be fixed be the owner. Fill free to contribute.

### Project Description
This project provides cmdlets for System Center Service Manager 2012/2012 SP1/2012 R2/2016 which can be used to automate common tasks.

### Versions
Version of the SMlets builds on follwing pattern:

<b>major.minor.SMVersion.0</b>

where:
 - <b>major</b> and <b>minor</b> are version of the SMLets itself
 - <b>SMVersion</b> is version of the Service Manager for which this SMLets was compiled


#### 2012 vs 2016 version
The only difference between the version for 2012 and 2016 is target framework: 3.5 for SCSM 2012 and 4.5.1 for SCSM 2016

### Installation
SMLets cmdlets can be installed with two different ways:
1. From MSI binary (see the Release tab under GitHub). In this case it will be installed globally for the server.
2. From [PowerShell Gallery](https://www.powershellgallery.com/packages/SMLets). In this case cmdlet will be installed for current user only.

To install exact version from PowerShell Gallery please use <i>-RequiredVersion</i> parameter:

`Install-Module -Name SMLets -RequiredVersion 1.0.2012.0`

Current commands:
```
Add-SCSMEnumeration                     Add-SCSMRequestOffering
Export-SCManagementPack                 Get-DataWarehouseConfiguration
Get-SCDWDimensionTypes                  Get-SCDWFactTypes
Get-SCDWMeasureTypes                    Get-SCDWOutriggerTypes
Get-SCDWRelationshipFactTypes           Get-SCDWWarehouseModuleTypes
Get-SCGroup                             Get-SCManagementPack
Get-SCManagementPackElement             Get-SCQueue
Get-SCSMAnnouncement                    Get-SCSMCategory
Get-SCSMChildEnumeration                Get-SCSMClass
Get-SCSMConfigItem                      Get-SCSMConnectedUser
Get-SCSMConsoleTask                     Get-SCSMEnumeration
Get-SCSMFolder                          Get-SCSMFolderHierarchy
Get-SCSMForm                            Get-SCSMImage
Get-SCSMIncident                        Get-SCSMLanguagePackCulture
Get-SCSMManagementPackReference         Get-SCSMObject
Get-SCSMObjectHistory                   Get-SCSMObjectProjection
Get-SCSMObjectTemplate                  Get-SCSMPage
Get-SCSMPageSet                         Get-SCSMRelatedObject
Get-SCSMRelationshipClass               Get-SCSMRelationshipObject
Get-SCSMRequestOffering                 Get-SCSMRequestOfferingQuestion
Get-SCSMResource                        Get-SCSMRule
Get-SCSMRunAsAccount                    Get-SCSMServiceOffering
Get-SCSMSession                         Get-SCSMStringResource
Get-SCSMSubscription                    Get-SCSMTask
Get-SCSMTaskResult                      Get-SCSMTopLevelEnumeration
Get-SCSMTypeProjection                  Get-SCSMUserRole
Get-SCSMUserRoleProfile                 Get-SCSMView
Get-SCSMViewSetting                     Get-SCSMViewType
Get-SCSMWhoAmI                          Get-SMLetsVersion
Import-SCManagementPack                 New-SCGroup
New-SCManagementPack                    New-SCQueue
New-SCSealedManagementPack              New-SCSMAnnouncement
New-SCSMColumn                          New-SCSMFolder
New-SCSMIncident                        New-SCSMManagementPackReference
New-SCSMNotificationSubscription        New-SCSMObject
New-SCSMObjectProjection                New-SCSMObjectTemplate
New-SCSMRelationshipObject              New-SCSMRequestOffering
New-SCSMRequestOfferingQuestion         New-SCSMServiceOffering
New-SCSMServiceRequest                  New-SCSMSession
New-SCSMUserRole                        New-SCSMView
Remove-SCGroup                          Remove-SCManagementPack
Remove-SCQueue                          Remove-SCSMEnumeration
Remove-SCSMObject                       Remove-SCSMRelationshipObject
Remove-SCSMRequestOffering              Remove-SCSMServiceOffering
Remove-SCSMSession                      Remove-SCSMSubscription
Remove-SCSMUserRole                     Remove-SCSMView
Set-SCSMAnnouncement                    Set-SCSMIncident
Set-SCSMObject                          Set-SCSMObjectProjection
Set-SCSMObjectTemplate                  Set-SCSMRunAsAccount
Set-SCSMDefaultComputer
```

#### Small guide
   [Using SMLets Beta 3 Post #1 – Using Get-SCSMObject, Get-SCSMClass to Dump Data from SCSM](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-1-8211-using-get-scsmobject-get/ba-p/342940)  
   [Using SMLets Beta 3 Post #2–Using Get-SCSMEnumeration, Get-SCSMRelationshipObject, Get-SCSMRelationshipClass to Automatically Resolve Incidents When All Child Activities Are Completed](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-2-8211-using-get-scsmenumeration-get/ba-p/342944)  
   [Using SMLets Beta 3 Post #3–Using Set-SCSMObject to Bulk Update Properties on Objects](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-3-8211-using-set-scsmobject-to-bulk/ba-p/342952)  
   [Using SMLets Beta 3 Post #4–Using New-SCSMObject to Create Objects](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-4-8211-using-new-scsmobject-to-create/ba-p/343089?search-action-id=213564747959&search-result-uid=343089)  
   [Using SMLets Beta 3 Post #6–Getting the Owner of a Service](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-6-8211-getting-the-owner-of-a-service/ba-p/343341)  
   [Using SMLets Beta 3 Post #7–Deleting Any Object in the UI](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-7-8211-deleting-any-object-in-the-ui/ba-p/343347?search-action-id=213564909504&search-result-uid=343347)  
   [Using SMLets Beta 3 Post #8–Getting the GUID of an Enumeration](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-8-8211-getting-the-guid-of-an/ba-p/343605?search-action-id=213565033426&search-result-uid=343605)  
   [Using SMLets Beta 3 Post #9–Deleting Objects](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-9-8211-deleting-objects/ba-p/343661?search-action-id=213565042519&search-result-uid=343661)  
   [Using SMLets Beta 3 Post #10–Getting a User’s Manager and a Manager’s Reports](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-10-8211-getting-a-user-8217-s-manager/ba-p/344384?search-action-id=213565162623&search-result-uid=344384)  
   [Using SMLets Beta 3 Post #11–Getting a List of All the Classes in a Management Pack](https://techcommunity.microsoft.com/t5/system-center-blog/using-smlets-beta-3-post-11-8211-getting-a-list-of-all-the/ba-p/344442?search-action-id=213565171849&search-result-uid=344442)  

