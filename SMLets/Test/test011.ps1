BEGIN
{
    # get the Out-TestLog function
    . ./Common
    # import common functions (get-lorem)
    . ../DataGen/Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $name = "System.WorkItem.Incident"
    $STARTTIME = [datetime]::Now

    $CClass = get-scsmclass -name "microsoft.windows.computer$"
    $includeList = @(get-scsmobject -class $CClass -MaxCount 3)
    $excludeList = @(get-scsmobject -class (get-scsmclass -name system.printer) -max 2)
    $GroupName = (Get-Lorem 3) -replace " "
    $TESTFAILED = $FALSE
    $SLEEPSECONDS = 60
    $MSG = "Sleeping for $SLEEPSECONDS seconds for group calc execution" 
    if ( ! $includeList -or ! $excludeList )
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}001")
        Out-TestLog ("     DETAILS: IncludeList Count = " + $includeList.Count + " ExcludeList Count = " + $excludeList.Count)
        1
        exit
    }
}

END
{

    ### TEST 011a
    try
    {
        # Test with include and exclude where include and exclude are 
        # different classes but all objects within include and exclude 
        # are the same class
        # This doesn't actually import the management pack, so no reason to remove it
        # so only check the group configuration
        $GroupArgs = @{
            Include            = $includeList
            ManagementPackName = "TESTMP1"
            Name               = $GroupName
            Description        = "A description for you!" 
            Exclude            = $excludeList
            PassThru           = $true
            }
        $MP = new-scgroup @GroupArgs
        $INCLUDEXPATH = "ManagementPack/Monitoring/Discoveries/Discovery/DataSource/MembershipRules/MembershipRule/IncludeList/MonitoringObjectId"
        $EXCLUDEXPATH = "ManagementPack/Monitoring/Discoveries/Discovery/DataSource/MembershipRules/MembershipRule/ExcludeList/MonitoringObjectId"
        $groupIncludeMembers = ([xml]$MP.getxml()).SelectNodes($INCLUDEXPATH)|%{$_."#text"}|sort
        $groupExcludeMembers = ([xml]$MP.getxml()).SelectNodes($EXCLUDEXPATH)|%{$_."#text"}|sort
        
        # $groupIncludeMembers = $g.IncludeList |%{$_.id}|sort
        # $groupExcludeMembers = $g.ExcludeList |%{$_.id}|sort
        $includeListGuids = $includeList|%{$_.id}|sort
        $excludeListGuids = $excludeList|%{$_.id}|sort
        if ( (compare-object $groupIncludeMembers $includeListGuids) -or (Compare-Object $groupExcludeMembers $excludeListGuids))
        {
            $TESTFAILED = $true
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011a")
        }
        else
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}011a")
        }
    }
    catch
    {
        $TESTFAILED = $true
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011a")
        $error | %{ Out-TestLog ("   DETAIL: " + $_) }
    }
    # no MP imported - test complete

    ### TEST 011b
    try
    {
        ## The simplest group test
        $MANAGEMENTPACKNAME = "TESTMP2"
        $error.clear()
        # clear the include and exclude lists
        # we'll pass the include list in the pipeline
        $GroupArgs.Remove("Include")
        $GroupArgs.Remove("Exclude")
        $GroupArgs.ManagementPackName = $MANAGEMENTPACKNAME
        $GroupArgs.Import = $true
        $MP = $includeList | new-scgroup @GroupArgs
        for($i = 0; $i -le $SLEEPSECONDS; $i+=5)
        {
            Write-Progress -Activity $msg -Status $i -perc ( 100 * ($i/$SLEEPSECONDS))
            start-sleep 5
        }
        $Group = Get-SCGroup -DisplayName $GroupName
        $includeListGuids = $includeList | %{ $_.id } | sort
        $groupIncludeMembers = $Group.Members | %{ $_.id } | sort
        if ( Compare-Object $includeListGuids $groupIncludeMembers )
        {
            $TESTFAILED = $true
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011b")
        }
        else
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}011b")
        }
    }
    catch
    {
        $TESTFAILED = $true
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011b" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
    }
    finally
    {
        Get-SCManagementPack -Name $MANAGEMENTPACKNAME | Remove-SCManagementPack
    }

    #
    # Test 3 include are multiple classes 
    try
    {
        $MANAGEMENTPACKNAME = "TESTMP3"
        $GroupArgs.Include = @( $includelist; $excludeList )
        $GroupArgs.Import  = $true
        $GroupArgs.ManagementPackName = $MANAGEMENTPACKNAME
        $MP = new-scgroup @GroupArgs
        for($i = 0; $i -lt $SLEEPSECONDS; $i+=5)
        {
            Write-Progress -Activity $msg -Status $i -perc ( 100 * ($i/$SLEEPSECONDS))
            start-sleep 5
        }
        start-sleep $SLEEPSECONDS
        $Group = Get-SCGroup -DisplayName $GroupName
        $ListGuids = @( $includeList; $excludeList) | %{ $_.id } | sort
        $groupIncludeMembers = $Group.Members | %{ $_.id } | sort
        if ( (compare-object $groupIncludeMembers $ListGuids))
        {
            $TESTFAILED = $true
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011c")
            $groupIncludeMembers | %{ Out-TestLog ("   Details: includemember - $_" ) }
            $ListGuids | %{ Out-TestLog ("   Details: ListGuid - $_" )  }
        }
        else
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}011c")
        }
    }
    catch
    {
        $TESTFAILED = $true
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}011c")
        $error | %{ Out-TestLog ("   DETAIL: " + $_) }
    }
    finally
    {
        # ok - this MP was imported, so remove it
        Get-SCManagementPack -Name $MANAGEMENTPACKNAME | Remove-SCManagementPack
    }

    if ( $TESTFAILED ) { return 1 } else { return 0 }
}
