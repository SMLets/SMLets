# Test the filter parameter to get-scsmobject. It should handle 
# property -eq
# property -gt
# property -lt
# property -ne
# property -like
# property -isnull
# property -isnotnull
# the same for GenericProperty 
# property1 -eq value -and property2 -eq value2
# property1 -gt value -and property2 -gt value2
# property1 -lt value -and property2 -lt value2
# property1 -ne value -and property2 -ne value2
# property1 -like value -and property2 -like value2
# property1 -isnull -and property2 -isnull
# property1 -isnotnull -and property2 -isnotnull
# property1 -eq value -or property2 -eq value2
# property1 -gt value -or property2 -gt value2
# property1 -lt value -or property2 -lt value2
# property1 -ne value -or property2 -ne value2
# property1 -like value -or property2 -like value2
# property1 -isnull -or property2 -isnull
# property1 -isnotnull -or property2 -isnotnull
#
# we'll build the test to handle just a few of these
#
BEGIN
{
    # the definition of Out-TestLog
    . ./Common.ps1
    $TESTNAME = $MyInvocation.MyCommand
    $classname = "System.GlobalSetting.ProblemSettings"
    $instance = get-scsmobject -class (Get-SCSMClass -name $classname) -max 1
    $propertyNames = $instance|gm -MemberType noteproperty | %{$_.name}
    $STARTTIME = [datetime]::Now
}

END
{

    try
    {
        $pName = "MaxAttachments"
        $v = $instance.$pName
        $o = get-scsmobject -class (Get-SCSMClass -Name $classname) -filter "$pName = '$v'"
        if ( $o.$pname -eq $v )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}a" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}a" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}a" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName = "MaxAttachments"
        $v = $instance.$pName
        $o = get-scsmobject -class (Get-SCSMClass -name $classname) -filter "$pName -eq '$v'"
        if ( $o.$pname -eq $v )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}b" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}b" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}b" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName = "MaxAttachments"
        $v = $instance.$pName
        $nv = $v - 1
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName -gt '$nv'"
        if ( $o.$pname -gt $nv )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}c" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}c" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}c" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName = "MaxAttachments"
        $v = $instance.$pName
        $nv = $v + 1
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName -lt '$nv'"
        if ( $o.$pname -lt $nv )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}d" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}d" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}d" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName1 = "MaxAttachments"
        $pName2 = "MaxAttachmentSize"
        $v1 = $instance.$pName1
        $v2 = $instance.$pName2
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName1 = '$v1' -and $pName2 = '$v2'"
        if ( $o.$pname1 -eq $v1 -and $o.$pname2 -eq $v2)
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}e" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}e" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}e" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName1 = "MaxAttachments"
        $pName2 = "MaxAttachmentSize"
        $v1 = $instance.$pName1
        $v2 = $instance.$pName2
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName1 = '$v1' -or $pName2 = '$v2'"
        if ( $o.$pname1 -eq $v1 -and $o.$pname2 -eq $v2)
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}f" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}f" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}f" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName = "MaxAttachments"
        $v = $instance.$pName
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName -isnotnull"
        if ( $o.$pname )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}g" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}g" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}g" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

    try
    {
        $pName = "priorityminvalue"
        $o = get-scsmobject -Class (Get-SCSMClass -Name $classname) -filter "$pName -isnull"
        if ( ! $o.$pname )
        { 
            Out-TestLog ("PASS: " + [datetime]::Now + ":${TESTNAME}h" ) 
        }
        else
        {
            Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}h" ) 
            $o | out-string -str | %{ out-testlog ("   DETAIL: " + $_ ) }
        }
    }
    catch 
    {
        Out-TestLog ("FAIL: " + [datetime]::Now + ":${TESTNAME}h" )
        $error | %{ Out-TestLog ("   DETAIL: " + $_ ) }
        return 1
    }

}
