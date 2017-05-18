param ( $RuleName = ".*" , [switch]$status )

$asm = [reflection.assembly]::LoadWithPartialName("Microsoft.EnterpriseManagement.Core")
if ( ! $asm ) { throw "Could not load SM CORE dll, exiting" }
$EMGTYPE = "microsoft.enterprisemanagement.enterprisemanagementgroup"
if ( $emg -isnot $EMGTYPE ) { $emg = new-object $EMGTYPE localhost }
if ( ! $emg.IsConnected ) { $emg.Reconnect() }
        #?{$_.GetCategories()}|
$rules = $emg.ManagementPacks.GetManagementPacks()|
        %{$_.getrules()}|
        ?{$_.DisplayName -match $ruleName}|
        %{ $_ | add-member NoteProperty ManagementPack $_.GetManagementPack() -pass }
$rules | %{
    if ( $status )
    {
        $emg.Subscription.GetSubscriptionStatusById($_.id) |
            add-member -pass NoteProperty Rule $_
    }
    else
    {
        [bool]$IsVisible = @($_ | %{ $_.GetCategories()|%{ get-scsmenumeration -id $_.value.id}}|?{$_.name -match "WorkflowSubscriptions"}).Count
        $_ | add-member -pass ScriptMethod GetStatus {
            $this.managementgroup.Subscription.GetSubscriptionStatusById($this.id) |
            add-member -pass NoteProperty Rule $this | 
            add-member -pass ScriptMethod GetLog {
                ([xml]$this.output).dataitem.workflowinstances.workflowinstance.trackingrecords.activitytrackingrecord | 
                    %{ 
                        $_.psobject.typenames[0] = "ActivityTrackingRecord"
                        $time = [datetime]$_.EventDateTime
                        $_ | add-member -pass NoteProperty DateTime $time
                    }
                }
            } |
            add-member -pass ScriptMethod GetLog {
                $offset = $args[0]
                write-host -for red "offset: $offset"
                $status = $this.GetStatus()
                if ( "" -ne $offset )
                {
                    [xml]$output = $status[$offset].output
                    $output.dataitem.workflowinstances.workflowinstance.trackingrecords.activitytrackingrecord
                }
                else
                {
                    $status| %{ 
                        [xml]$output = $_.output
                        $output.dataitem.workflowinstances.workflowinstance.trackingrecords.activitytrackingrecord 
                        }
                }
                } |
        add-member -pass NoteProperty IsVisible $IsVisible |
        }
    }
