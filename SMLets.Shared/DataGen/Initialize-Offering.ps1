param ( $count = 10 , $ComputerName = "localhost", [switch]$whatif, [switch]$Verbose)

BEGIN
{
	. ./common

    if ( ! (get-scsmclass System.RequestOffering))
    {
        Write-Host -For RED "System.RequestOffering Type is not present, cannot proceed"
        exit
    }


	#
	# CREATE SERVICE REQUEST  
	#
	$RANDOM = new-object System.Random
	$PTYPE = "System.WorkItem.ServiceRequestProjection"
	###
	### SETUP 
	### Retrieve stuff from the CMDB which will be used later
	###
	Write-Progress -Activity "Setting Up Environment" -Status "Getting Users"
	$Users = Get-scsmobject (get-scsmclass Microsoft.AD.User$) -MaxCount 60
	if ( $Users.Count -lt 10 ) {
	        Write-Error "Not enough users, go make some more"
        	exit
        }
        
    $IDD = new-object Microsoft.EnterpriseManagement.ConnectorFramework.IncrementalDiscoveryData
    $EMG = new-object Microsoft.EnterpriseManagement.EnterpriseManagementGroup $ComputerName
    # A USER
    $user = get-scsmobject (get-scsmclass ^System.User$) -filter "DisplayName -like 'james%'"
    $userList = get-scsmobject (get-scsmclass System.User$) -filter 'LastName -like "%"'
    trap { write-host "some error"; $error[0]; exit }
        
	Write-Progress -Activity "Setting Up Environment" -Status "Getting Enumerations"
	# ENUMERATIONS
	# the "." at the end of the enumeration is required to be sure we get the list
	$statusList = get-scsmchildenumeration -enum (get-scsmenumeration System.Offering.StatusEnum$)
  
	# CLASSES	
	$RelatedwIList = get-scsmclass System.Workitem | ?{$_.Name -match "m.Incident$|ChangeRequest$|Problem$|Activity$|ReleaseRecord$" -and ! $_.abstract}|get-scsmobject
	$relatedServiceList = get-scsmobject (get-scsmclass Microsoft.SystemCenter.BusinessService)
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Offerings Creation"
        
    $srTemplate =  Get-SCSMObjectTemplate "servicerequest"
    $desiredTemplate = $srTemplate.Identifier | Foreach-Object {$_.ToString()} 
}

END
{
	1..$count|%{
	$i = $_
	Write-Progress -Activity "Creating Request and Service Offerings" -Status $i -perc ([int]($i/$count * 100))

	$Status = Get-RandomItemFromList $statusList
    $Service = Get-RandomItemFromList $relatedServiceList

	#
	# CREATE REQUEST OFFERING 
	#

    $P1Type = "System.RequestOffering.Portal.ProjectionType"    

    $RequestOfferingClass = get-scsmclass System.RequestOffering
	$RequestOffering = @{
			BriefDescription     = Get-Lorem 8
		    Comment              = Get-Lorem 8
			DisplayName          = Get-Lorem 8
			Domain	             = "SMX"
		    ID                   = [guid]::NewGuid().ToString()
			Notes                = get-lorem 12
			Overview             = get-lorem 5
            PresentationMappingTemplate = "<Object ID='1|Microsoft.EnterpriseManagement.ServiceManager.Default|1.0.0.0|PresentationMappingTemplate41672418326a4a109a3a6ec801fb8007|3|RequestOffering/PresentationMappingTemplate'><References /><Data><PresentationMappingTemplate><Sources><Source Ordinal='1' Prompt='Required Question 1' ReadOnly='False' Optional='False'><Targets><Target Path='Description'/></Targets></Source><Source Ordinal='2' Prompt='Optional Question 1' ReadOnly='False' Optional='True'><Targets><Target Path='ContactMethod' /></Targets></Source><Source Ordinal='3' Prompt='Display Information' ReadOnly='True' Optional='False'><Targets><Target Path='Notes' /></Targets></Source></Sources></PresentationMappingTemplate></Data></Object>"
            PublishDate          = [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
			Status               = Get-RandomItemFromList $statusList
			Title                = get-lorem 4
            TargetTemplate       = $desiredTemplate
            #Path
			#EstimatedTimeToCompletion
			}
       
   $RequestOfferingInstance = new-scsmobject -Class $RequestOfferingClass -propertyhash $RequestOffering -nocommit 
   write-host -for red "Offering $i Status is $Status "
    
        # add the projection to the IncrementalDiscoveryDataPacket
        if ( $whatif )
        {
            "What if: Performing operation 'Intialize-ServiceRequest' on Target $RequestOfferingInstance"
        }
        else
        {
            Write-Verbose -verbose:$verbose $RequestOfferingInstance 
            $IDD.Add($RequestOfferingInstance)           
        }
   }
   
   #Write-Progress -Activity "Creating Projection" -Status "Committing Projections" -perc (($ChangeCount/$count)*100)

    if ( ! $whatif )
    {
        write-host -for red "Offerings committing"
        Write-Verbose -verbose:$verbose "Committing $count Offerings"
        # Commit the instances of request offering
        $IDD.Commit($EMG)

        # CLASSES
        $ROClass = get-scsmclass System.RelatedOffering
        $SOClass = Get-scsmClass System.ServiceOffering                

        # OBJECT LIST
    	$rolist = get-scsmclass System.RequestOffering | get-scsmobject -max $count
        $userList = get-scsmclass System.User | get-scsmobject -max $count
        $kaList = get-scsmclass System.Knowledge.Article | get-scsmobject 
        $serviceList = get-scsmclass Microsoft.SystemCenter.BusinessService | get-scsmobject 
        
        #ENUMERATION
        $categoryList = get-scsmchildenumeration -enum (get-scsmenumeration System.ServiceOffering.CategoryEnum$)
        
        #RELATIONSHIPS
    	$relationship = get-scsmrelationshipclass System.RelatedOfferingRelatesToOffering
        $relationship1 = get-scsmrelationshipclass System.OfferingPublishedBy
        $relationship2 = get-scsmrelationshipclass System.ServiceRelatesToServiceOffering   

        $rolist | %{ 
            $RequestOffering = $_
            #
            # Create the SERVICE OFFERING OBJECT
            #
        	$ServiceOffering = @{
        			BriefDescription	     = Get-Lorem 8
        			Category		         = "General"                   
        			CostInformation		     = Get-Lorem 2
        			CostInformationLink	     = "http://costinformationlink.com"
        			CultureName		         = "en-GB"
        			DisplayName	             = Get-Lorem 4
        			Domain			         = "SMX"
        			ID		                 = [guid]::NewGuid().ToString()
        			Notes			         = Get-Lorem 8
        			Overview		         = Get-Lorem 5
        			Path			         = Get-Lorem 6
        			PublishDate		         = [datetime]::Now.AddDays(-$RANDOM.Next(0,30))
        			SLAInformation		     = Get-Lorem 8
        			SLAInformationLink	     = "http://slainformationlink.com"
        			Status                   = Get-RandomItemFromList $statusList
        			Title                    = get-lorem 4
                    #Image = ""
                    #Comment = ""
        			}
            $ServiceOfferingInstance = new-scsmobject -Class $SOClass -propertyhash $serviceOffering -pass
            
        	#
        	#CREATE RELATED OFFERING OBJECT
        	#
        	$RelatedOffering = @{
        		DisplayName		     = Get-Lorem 8
        		Domain			     = "SMX"
        		ID	          	     = [guid]::NewGuid().ToString()
        		Path                 = Get-Lorem 8
        		Comment              = get-lorem 5
        	}
        	$RelatedOfferingInstance = new-scsmobject -class $ROClass -propertyhash $RelatedOffering -pass
            
        	new-object psobject -proper @{
    			Relationship = $relationship
    			Source = $RelatedOfferingInstance
    			Target = $RequestOffering
    			}
    		new-object psobject -proper @{
    			Relationship = $relationship
    			Source = $RelatedOfferingInstance
    			Target = $ServiceOfferingInstance
    			}
            new-object psobject -proper @{
    			Relationship = $relationship1
    			Source = $RequestOffering
    			Target = Get-RandomItemFromList $userList
    			}
            new-object psobject -proper @{
    			Relationship = $relationship1
    			Source = $ServiceOfferingInstance
    			Target = Get-RandomItemFromList $userList
    			}          
            new-object psobject -proper @{
    			Relationship = $relationship2
    			Source = Get-RandomItemFromList $serviceList
    			Target = $ServiceOfferingInstance
    			}      
        } | new-scsmrelationshipobject -bulk  
   }
}
