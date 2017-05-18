param ( $date = 30, [switch]$whatif )
BEGIN
{
    if ( $date -is [datetime] )
    {
        if ( $date -gt [datetime]::Now )
        {
            throw "must have a date before now"
        }
    }
    else
    {
        if ( $date -is [int] )
        {
            $date = [datetime]::Now.AddDays(-$date)
        }
        else
        {
            throw "provide a date (or days)"
        }
    }
    "Setting date to $date"
    $cstr = "Server=localhost;Database=ServiceManager;Trusted_Connection=yes"
    $c = [data.sqlclient.sqlconnection]$cstr
    $c.open()
}
PROCESS
{
    # this has to be an instance
    # we should check
    if ( $_ -is [Microsoft.EnterpriseManagement.Common.EnterpriseManagementObject] )
    {
        $id = $_.get_id()
        $comm = $c.createcommand()
        $command = "Update BaseManagedEntity SET LastModified='" + $date + "' Where BaseManagedEntityId='$id'";
        $comm.CommandText = $command
        if ( $WhatIf )
        {
            $command
        }
        else
        {
            $r = $comm.ExecuteNonQuery()
            if ( $r -ne 1 ) { "Couldn't set BME" }
        }
    }
}

END
{
    $c.close()
}
