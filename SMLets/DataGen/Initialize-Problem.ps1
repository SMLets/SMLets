param ( $count = 10 , [switch]$whatif, [switch]$Verbose, [switch]$debug, [switch]$nobulk )

BEGIN
{
    . ./Common.ps1

    $RANDOM = new-object System.Random
    $PTYPE = "System.WorkItem.Problem.ProjectionType"
    ###
    ### SETUP 
    ### Retrieve stuff from the CMDB which will be used later
    ###
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Users"
    $Users = Get-scsmobject (get-scsmclass Microsoft.AD.User$)
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Config Items"
    $CIList = get-scsmobject (get-scsmclass System.ConfigItem$) -MaxCount 300
    Write-Progress -Activity "Setting Up Environment" -Status "Getting Enumerations"
    # ENUMERATIONS
    # the "." at the end of the enumeration is required to be sure we get the list
    $StatusList         = Get-SCSMEnumeration ProblemStatusEnum.
    $SourceList         = get-scsmenumeration ProblemSourceEnum.
    $ResolutionList     = Get-SCSMEnumeration ProblemResolutionEnum.
    $ClassificationList = Get-SCSMEnumeration ProblemClassificationEnum.
    $ImpactList         = Get-SCSMEnumeration System.WorkItem.TroubleTicket.ImpactEnum.
    $UrgencyList        = Get-SCSMEnumeration System.WorkItem.TroubleTicket.UrgencyEnum.
    
    Write-Progress -Activity "Setting Up Environment" -Status "Starting Problem Creation"
}

END
{

    $ProjectionArgs = @{
        Type    = $PType
        bulk    = ! $nobulk
        verbose = $verbose 
        whatif  = $whatif 
        debug   = $debug
        }
    1..$count|%{
        $i = $_
        Write-Progress -Activity "Creating Problem" -Status $i -perc ([int]($i/$count * 100))

        # This is the date 
        $CreatedDate = [datetime]::Now.AddDays(-$RANDOM.Next(30,90))

        $ACIs = Get-RandomListFromList $CIList 5 # $CIList[$rlist]
        $RCIs = Get-RandomListFromList $CIList 4 # $CIList[$rlist]

        $Impact         = Get-RandomItemFromList $ImpactList
        $Urgency        = Get-RandomItemFromList $UrgencyList
        $Priority       = Get-Priority $Urgency.DisplayName $Impact.DisplayName
        $Status         = Get-RandomItemFromList $StatusList
        # DEBUG::: $Status         = $StatusList | ?{$_.Name -match "Resolv"}
        $Source         = Get-RandomItemFromList $SourceList
        $Classification = Get-RandomItemFromList $ClassificationList
        $Resolution     = Get-RandomItemFromList $ResolutionList


        if     ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(4)  }
        elseif ( $Urgency -match "medium" ) { $TargetResolutionTime = $CreatedDate.AddHours(24) }
        elseif ( $Urgency -match "high" )   { $TargetResolutionTime = $CreatedDate.AddHours(72) }
        else                                { $TargetResolutionTime = $CreatedDate.AddHours(48) }

        $AffectedUser  = Get-RandomItemFromList $users
        $AssignedUser  = Get-RandomItemFromList $users
        $CreatedByUser = Get-RandomItemFromList $users
        $ResolvedBy    = Get-RandomItemFromList $users
        $ClosedBy      = Get-RandomItemFromList $users

        # by default this creates 5 
        # $global:FASTREAMS = new-FileAttachmentStream

        # CREATE THE SEED HASH TABLE
        $ProblemSeed = @{
                Id                   = "CustomProblem{0}"
                Urgency              = $Urgency
                Impact               = $Impact
                Status               = $Status
                Source               = $Source
                Classification       = $Classification
                Resolution           = $Resolution
                Priority             = $Priority
                # ClosedDate
                # ResolvedDate
                Title                = get-lorem 6
                Description          = get-lorem 22
                CreatedDate          = $CreatedDate
                KnownError           = [bool]($random.Next(0,2))
                RequiresMajorProblemReview = [bool]($random.Next(0,2))
            }

        # FINISH THE SEED HASH TABLE
        # set up for closed and resolved status
        if ( $status -match "Closed" -or $status -match "Resolved" )
        {
            $ProblemSeed['ResolutionDescription'] = get-lorem 22
            $ProblemSeed['ResolutionCategory']    = Get-RandomItemFromList $ResolutionList
            $ResolveDiff = ($TargetResolutionTime - $CreatedDate).TotalHours
            $TimeVariance = $RANDOM.Next(-$ResolveDiff,$ResolveDiff)
            $ResolvedDate = $TargetResolutionTime.AddHours($TimeVariance)
            $ProblemSeed['ResolvedDate'] = $ResolvedDate
            if ( $status -match "Closed" )
            {
                $ProblemSeed['ClosedDate'] = $ResolvedDate.AddHours($RANDOM.Next(0,2)*24)
            }
        }

        $p = @{
            __CLASS = "System.WorkItem.Problem"
            __OBJECT = $ProblemSeed
            # Now for the Aliases
            AssignedTo             = $AssignedUser
            CreatedBy            = $CreatedByUser
            AffectedConfigItems      = $ACIs
            }
        if ( $status -match "Resolved" ) { $p['ResolvedBy'] = $ResolvedBy }
        if ( $status -match "Closed" )   { $p['ResolvedBy'] = $ResolvedBy; $p['ClosedBy'] = $ClosedBy }

    $p } | new-SCSMOBjectProjection @ProjectionArgs
}
