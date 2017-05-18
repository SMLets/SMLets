param ( [int]$count = 1, [switch]$whatif, [switch]$verbose, [switch]$nobulk )

. ./Common.ps1
$userList     = get-scsmobject (get-scsmclass Microsoft.AD.User$) -filter "LastName -like '%'"
$computerList = get-scsmobject (get-scsmclass System.Computer$) -max 60


$serviceGrouplist = get-scsmclass | ?{$_.getbasetype().name -eq "Microsoft.SystemCenter.ServiceDesigner.ServiceComponentGroup" -and ! $_.abstract}

$BigserviceGroupList = Get-RandomListFromList $serviceGroupList 3
$BigserviceGroupList += Get-RandomListFromList $serviceGroupList 4

$ServiceStatusList = get-scsmchildenumeration -en (get-scsmenumeration System.ServiceManagement.ServiceStatus$)
$ServicePriorityList = get-scsmchildenumeration -en (get-scsmenumeration System.ServiceManagement.ServicePriority$)
$ServiceClassificationList = get-scsmchildenumeration -en (get-scsmenumeration System.ServiceManagement.ServiceClassification$)

$ServiceProjectionType = "Microsoft.System.Service.FormProjectionType"

$ProjectionArgs = @{
    Type     = $ServiceProjectionType 
    whatif   = $whatif 
    verbose  = $verbose 
    bulk     = ! $nobulk
    # passthru must be TRUE
    passthru = $true
    }

$global:services = @(
    1..$count | %{
    $DisplayName = Get-Lorem 2
    Write-Progress -Status "Creating Service" -Activity $DisplayName -perc (($_/$count)*100)

    $Id = [guid]::NewGuid().toString()
    $Seed = @{
        ServiceId            = $Id
        Name                 = $Id
        OwnedByOrganization  = Get-Lorem 2
        Priority             = Get-RandomItemFromList $ServicePriorityList
        Status               = Get-RandomItemFromList $ServiceStatusList
        Classification       = Get-RandomItemFromList $ServiceClassificationList
        AvailabilitySchedule = "Always"
        DisplayName          = $DisplayName
        }
    $projectionHash = @{
        __CLASS = "Microsoft.SystemCenter.BusinessService"
        __OBJECT = $seed
        ServiceHasGroups = $BigServiceGroupList | %{
                $name = $_.name
                $dname = (Get-Lorem 3) + " " + $_.DisplayName
                    @{
                __CLASS = "$name"
                __OBJECT = @{
                    Id           = [guid]::NewGuid().ToString()
                    DisplayName  = $dname
                    Notes        = Get-Lorem 22
                    ObjectStatus = "Active"
                    AssetStatus  = "Deployed"
                    }
                }
            }
        ComponentBusinessCustomers = Get-RandomListFromList $userList 6
        ComponentImpactedByService = Get-RandomListFromList $userList 3
        ComponentServiceContacts   = Get-RandomListFromList $userList 2
        RelatedWorkItem            = get-scsmobject (get-scsmclass ^System.WorkItem.Incident$) -max 10
        # UsedBy                   = get-scsmobject (get-scsmclass System.ConfigItem) -max 10
        }
    $projectionHash
    } | new-scsmobjectprojection @ProjectionArgs

    )

$containsConfig = get-scsmrelationshipclass System.ConfigItemContainsConfigItem
$relationshipCollection = @()
if ( $whatif )
{
    Write-Host 'What if: Performing operation "New-SCSMRelationshipObject" on Target "IncrementalDiscoveryData"'
}
else
{
    foreach($service in $services)
    {
        foreach($serviceGroup in $service.Item("Target"))
        {
            $source = $serviceGroup.Object
            Get-RandomListFromList $computerList $RANDOM.Next(3,8) | %{
                    $relationshipCollection += new-object psobject -property @{
                    Target = $_
                    Source = $Source
                    Relationship = $containsConfig
                    }
                }
        }
    }
    Write-Progress -Status "Committing Relationships" -Activity "Bulk Operation" -perc 100
    $RelationshipArgs = @{
        whatif  = $whatif 
        verbose = $verbose 
        bulk    = ! $nobulk
        }
    $relationshipCollection | new-scsmrelationshipobject @RelationshipArgs
}
