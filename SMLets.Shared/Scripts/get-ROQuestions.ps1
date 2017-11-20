param (
    [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)]$RequestOffering
    )
PROCESS
{
    $RoObject = new-object psobject
    $RoObject | add-member NotePRoperty RequestOffering $RequestOffering
    $RoObject | add-member NoteProperty Questions @()
    $Template = [xml]$RequestOffering.PresentationMappingTemplate
    $Template.SelectNodes("Object/Data/PresentationMappingTemplate/Sources/Source")|?{
        $_.id -ne [guid]::Empty}|%{ 
        $Type = $_.ControlType.Split(".")[-1]
        $list = $null
        if ( $type -eq "InlineList" )
        {
            $list = $_.ControlConfiguration.Configuration.Details.ListValue|%{$_.DisplayName}
        }
        # hoo - this is a pain
        if ( $type -eq "List" )
        {
            $OTName = $REquestOffering.TargetTemplate.Split("|")[3]
            $OT = get-scsmobjecttemplate "^${OTName}$"           
            $Seed = (Get-SCSMTypeProjection -id $OT.TypeID.ID).TargetType
            $enum = $null
            if ( $Seed.TryGetProperty($_.Targets.Target.Path,[ref]$enum))
            {
                $list = get-scsmenumeration ("^" + $enum.Type + "$")
            }
        }
        $myo = new-object psobject -prop @{ 
            Ordinal = $_.ordinal
            Prompt = $_.prompt
            Optional = $_.optional
            Type = $_.ControlType.Split(".")[-1]
            Target = $_.Targets.Target.Path 
            } 
        if ( $list )
        {
            $myo | add-member NoteProperty List $list
        }
        $RoObject.Questions += $myo
    }
    $RoObject
}

