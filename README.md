# SCSM PowerShell Cmdlets 


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
   [Using SMLets Beta 3 Post #1 – Using Get-SCSMObject, Get-SCSMClass to Dump Data from SCSM](http://blogs.technet.com/b/servicemanager/archive/2011/04/21/using-smlets-beta-3-post-1-using-get-scsmobject-get-scsmclass-to-dump-data-from-scsm.aspx)  
   [Using SMLets Beta 3 Post #2–Using Get-SCSMEnumeration, Get-SCSMRelationshipObject, Get-SCSMRelationshipClass to Automatically Resolve Incidents When All Child Activities Are Completed](http://blogs.technet.com/b/servicemanager/archive/2011/04/21/using-smlets-beta-3-post-2-using-get-scsmenumeration-get-scsmrelationshipobject-get-scsmrelationshipclass-to-automatically-resolve-incidents-when-all-child-activities-are-completed.aspx)  
   [Using SMLets Beta 3 Post #4–Using New-SCSMObject to Create Objects](http://blogs.technet.com/b/servicemanager/archive/2011/05/03/using-smlets-beta-3-post-4-using-new-scsmobject-to-create-objects.aspx)  
   [Using SMLets Beta 3 Post #6–Getting the Owner of a Service](http://blogs.technet.com/b/servicemanager/archive/2011/05/24/using-smlets-beta-3-post-6-getting-the-owner-of-a-service.aspx)  
   [Using SMLets Beta 3 Post #7–Deleting Any Object in the UI](http://blogs.technet.com/b/servicemanager/archive/2011/05/25/using-smlets-beta-3-post-7-deleting-any-object-in-the-ui.aspx)  
   [Using SMLets Beta 3 Post #8–Getting the GUID of an Enumeration](http://blogs.technet.com/b/servicemanager/archive/2011/06/24/using-smlets-beta-3-post-8-getting-the-guid-of-an-enumeration.aspx)  
   [Using SMLets Beta 3 Post #9–Deleting Objects](http://blogs.technet.com/b/servicemanager/archive/2011/07/13/using-smlets-beta-3-post-9-deleting-objects.aspx)  
   [Using SMLets Beta 3 Post #10–Getting a User’s Manager and a Manager’s Reports](http://blogs.technet.com/b/servicemanager/archive/2011/12/03/using-smlets-beta-3-post-10-getting-a-user-s-manager-and-a-manager-s-reports.aspx)  
   [Using SMLets Beta 3 Post #11–Getting a List of All the Classes in a Management Pack](http://blogs.technet.com/b/servicemanager/archive/2012/01/04/using-smlets-beta-3-post-11-getting-a-list-of-all-the-classes-in-a-management-pack.aspx)  

