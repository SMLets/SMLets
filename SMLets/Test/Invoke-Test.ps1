#requires -version 2.0
[CmdletBinding(SupportsShouldProcess=$true)]
param (
    [string[]]$tests
    )
BEGIN
{
    # dot source the common stuff
    . ./common.ps1
    $ShowResults = $false
    # these can be functions, or scripts
    if ( $tests )
    {
        [string[]]$testprograms = $tests
    }
    else
    {
        [string[]]$testprograms = get-childitem test[0-9][0-9][0-9].ps1 -name
    }

    $TestResults = @()
    if ( test-path $LOGFILE ) { remove-item $LOGFILE }
    foreach ( $program in $testprograms ) 
    { 
        if ( $PSCmdlet.ShouldProcess($program))
        {
            $ShowResults = $true
            if ( & ./$program ) { $result = "FAIL" } else { $result = "PASS" }
            $testresults += new-object psobject -prop @{ 
                Name = $program
                Result = $result
                }
        }
    }
}
END
{
    if ( $ShowResults )
    {
        $TestResults
    }
}
