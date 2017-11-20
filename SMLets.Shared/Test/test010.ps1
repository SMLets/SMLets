BEGIN
{
    # set-psdebug -trace 2
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $name = "System.WorkItem.Incident"
    $STARTTIME = [datetime]::Now
    $IncidentTitle  = "Test010 - $STARTTIME"
    $Urgency        = "Medium"
    $Impact         = "High"
    $Status         = "Active"
    $Classification = "Software"
    $Source         = "System"
    $CreatedAfter   = $STARTTIME.AddHours(-1)
    $CreatedBefore  = $STARTTIME.AddHours(1)
    $TESTFAILED     = $false
}
END
{

    try
    {
        # SETUP
        $error.clear()
        $incidentClass = get-scsmclass ^System.WorkItem.Incident$
        $seed = new-scsmobject -pass $incidentClass @{ 
            Id = "IR{0}"; 
            Title = $IncidentTitle; 
            Urgency = $Urgency
            Impact = $Impact
            Status = $Status
            Classification = $Classification
            Source = $Source
            }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        # no reason to continue if this fails
        return 1
    }
    finally
    {
        set-psdebug -trace 0
    }


    # TEST a - by ID
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -id $Id )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010a" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010a" ) 
            out-testlog ("   DETAIL: No Incident found (ID) - $ID"  )
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010a" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST b - by Title
    try
    {
        $error.clear()
        $Title = $seed.Title
        if ( get-scsmincident -title $Title )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010b" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010b" ) 
            out-testlog ("   DETAIL: No Incident found (Title) - $Title"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010b" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }


    # TEST c - by Id and Urgency
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -Urgency $Urgency  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010c" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010c" ) 
            out-testlog ("   DETAIL: No Incident found (Urgency) - $Urgency"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010c" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST d - by Id and Status
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -Status $Status  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010d" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010d" ) 
            out-testlog ("   DETAIL: No Incident found (Status) - $Status"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010d" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }

    # TEST e - by Id and Impact
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -Impact $Impact  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010e" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010e" ) 
            out-testlog ("   DETAIL: No Incident found (Impact) - $Impact"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010e" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST f - by Id and Classification
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -Classification $Classification  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010f" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010f" ) 
            out-testlog ("   DETAIL: No Incident found (Classification) - $Classification"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010f" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST g - by Id and Source
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -Source $Source  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010g" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010g" ) 
            out-testlog ("   DETAIL: No Incident found (Source) - $Source"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010g" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST h - by Id and CreatedAfter
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -CreatedAfter $CreatedAfter  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010h" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010h" ) 
            out-testlog ("   DETAIL: No Incident found (CreatedAfter) - $CreatedAfter"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010h" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }

    # TEST i - by Id and CreatedBefore
    try
    {
        $error.clear()
        $ID = $seed.ID
        if ( get-scsmincident -ID $ID -CreatedBefore $CreatedBefore  )
        {
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}010i" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010i" ) 
            out-testlog ("   DETAIL: No Incident found (CreatedBefore) - $CreatedBefore"  )
            $TESTFAILED = $true
        }
    }
    catch
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}010i" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        $TESTFAILED = $true
    }
    finally
    {
        set-psdebug -trace 0
    }


    if ( $TESTFAILED )
    {
        return 1
    }
    else
    {
        return 0
    }

}
