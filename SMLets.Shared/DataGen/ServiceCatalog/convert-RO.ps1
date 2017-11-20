param ( $filename = "ServiceCatalog.csv", $MPNAME = "RequestOfferings", [switch]$clobber, [switch]$test  )
$missingIcon = 0
$global:streamCollection = @()
$OPEN = ([io.filemode]::Open)
$RACC = ([io.fileaccess]::Read)
$RSHR = ([io.fileshare]::read)
function New-IconStream
{
    param ( $file ) 
    $fullname = "${PWD}/${file}.jpg"
    new-bitmap -filename "$fullname"
    $str = new-object io.filestream "${fullname}",$OPEN,$RACC,$RSHR
    $global:streamCollection += $str
    $str
}
function Get-IconStream
{
    param ( $name )
    if ( test-path $name )
    {
        $fullname = (resolve-path $name).path
    }
    else
    {
    $fullname = (resolve-path "..\Portal Images\$name").path
    }
    $str = new-object io.filestream "${fullname}",$OPEN,$RACC,$RSHR
    $global:streamCollection += $str
    $str
}

$iconlist = get-childitem '..\Portal Images' -fil *.png
$IconRandom = new-object System.Random
function Get-RandomIcon
{
    $offset = $IconRandom.Next(0,$iconlist.Count)
    $iconlist[$offset].FullName
}


function Convert-ROData
{
    param ( $CSVfile )
    import-csv $CSVfile| %{ 
    # get-content $CSVfile | convertfrom-csv | %{
    if ( $_.Category ) { $Category = $_.Category } else { $_.Category = $Category }
    if ( $_."Service Offering" ) { $SO = $_."Service Offering" } else { $_."Service Offering" = $SO }
    if ( $_.Question ) { $Question = $_.Question } else { $_.Question = $Question }
    if ( $_."Question Type" ) { $QuestionType = $_."Question Type" } else { $_."Question Type" = $QuestionType }
    if ( $_."RO Owner" ) { $Owner = $_."RO Owner" } else { $_."RO Owner" = $Owner }
    if ( $_."RO Description" ) { $RODescription = $_."RO Description" }else { $_."RO Description" = $RODescription }
    if ( $_."RO Icon" ) { $ROIcon= $_."RO Icon" } # else { Write-Host No Icon } # $_."RO Icon" = "" }

    $_.Question = $_.Question -replace ([char]65533),"'" -replace "&","&amp;" -replace "<","&gt;" -replace ">","&lt;" -replace '"',"&quot;" -replace "'","&apos;" –replace ([char]8211),"-"
    $_
    }
}

function ConvertTo-RO 
{
    param ( $data )
    $g = $data | group "Request Offering","Service Offering","Category"
    foreach ($group in $g)
    {
        $roname,$soname,$category = $group.name -split ","
        $roname = $roname.Trim()
        $soname = $soname.Trim()
        try
        {
        $category = $category.Trim()
        }
        catch
        {
            write-host "boo"
        }
        $r = @{ 
            ROName = $roname
            SOName = $soname
            Category = $category
            Icon = @($group.group)[0]."RO Icon"
            Q = @()
            }
        $qg = $group.group | group Question
        foreach ( $q in $qg )
        {
            $h = @{ 
                Prompt = $q.Name # -replace "^[0-9][0-9]\. " -replace "^[0-9A-Z] - "
                Type = $q.group[0]."Question Type"
                MAP = $q.group[0].Map.trim()
                Required = [bool]$q.group[0].Required
                }
            if ( $h.Type -eq "Simple List" )
            {
                $h['ListElements'] = $q.Group|%{$_.Answer -replace ([char]65533),"'" -replace "&","&amp;" -replace "<","&gt;" -replace ">","&lt;" -replace '"',"&quot;" -replace "'","&apos;" –replace ([char]8211),"-" }
            }
            if ( $h.Type -eq "MP Enum list" )
            {
                $h['Enumeration'] = $q.group[0].Answer.Trim()
            }
            $r['Q'] += new-object psobject -prop $h
        }
        new-object psobject -prop $r
    }

}
function Test-ROValues
{
    param ( 
        [Parameter(Mandatory=$true,Position=0)]$rodata, 
        [Parameter(ParameterSetName="good")][switch]$good, 
        [Parameter(ParameterSetName="bad")][switch]$bad, 
        [Parameter(ParameterSetName="both")][switch]$both 
        )
        $goodlist = $badlist = @()
        $SERVICEREQUESTCLASS = "System.WorkItem.ServiceRequest$"
        $names = get-scsmclass $SERVICEREQUESTCLASS | get-smproperty | %{ $_.name }
        
        foreach( $ro in $rodata )
        {
            $reason = @()
            $isGood = $true
            $badMap = $ro.Q | %{$_.map}|?{ $names -notcontains $_ }
            if ( $badMap )
            {
                $isGood = $false
                $reason += "Mapped property '" + ($badmap -join ",") + "' does not exist"
            }
            $dup = $ro.Q | group MAP |?{$_.count -gt 1 } | %{$_.name}
            if ( $dup ) 
            {
                $isGood = $false
                $reason += "Mapped property '" + ($dup -join ",") + "' used more than once"
            }
            $notype = $ro.Q | ?{! $_.Type.Trim() }
            if ( $notype )
            {
                $isGood = $false
                $reason += "Question type '" + ($noType -join ",") + "' not set"
            }
            $noMap = $ro.Q | ?{ ! $_.MAP }|%{$_.prompt }
            if ( $noMap )
            {
                $isGood = $false
                $reason += "question '" + ($noMap -join ",") + "' not mapped"
            }
            $nolist = $ro.Q | ?{ $_.Type -eq "Simple List" } | ?{ ! $_.ListElements }|%{$_.prompt}
            if ( $nolist )
            {
                $isGood = $false
                $reason += "Simple list  '" + ($nolist -join ",") + "'has no elements"
            }
            # now add to list
            if ( $isGood ) 
            { 
                $goodlist += $ro 
            }
            else 
            { 
                $badlist += $ro | add-member NoteProperty FailureReason $reason -pass
                Write-Error ("===> {0}-{1}-{2}" -f $ro.Category,$ro.SOName ,$ro.ROName)
                $reason|Write-Error
            }
            
        }
    Write-Progress -activity "Testing Values" -Status done
    if ( $both )
    {
        return $goodlist,$badlist
    }
    if ( $bad )
    {
        return $badlist
    }
    if ( $good )
    {
        return $goodlist
    }
}
function New-RO
{
    param ( $list, [switch]$whatif )
    
    $typemap = @{
        "Text" = "String"
        "Date" = "DateTime"
        "MP Enum List" = "list"
        "SimpleList" = "inlinelist"
        "Simple List" = "inlinelist"
        "integer" = "integer"
        "True False" = "Boolean"
        "Check Box" = "Boolean"
        "Attachement" = "FileAttachment"
        "Attachment" = "FileAttachment"
        "number" = "integer"
        }
    # the ROContainer for bulk operation at the end
    $current = 0
    $total = $list.count
    # NOTE: CUSTOMIZATION FOR MPSD SHOULD BE CHANGED
    $TEMPLATENAME = "Default Service Request"
    $ObjectTemplate = Get-scsmobjecttemplate -displayname $TEMPLATENAME
    foreach($g in $list)
    {
        $current++
        $qlist = @()
        $ROName = $g.roname.Trim()
        $Category = $g.Category.Trim()
        $SOName = $g.SOName.Trim()
        $message = "$Category : $SOName - $ROName"
        $ICON = $g.Icon
        # random Icon generator
        if ( ! $ICON )
        { 
            $ICON = Get-RandomIcon
            #write-host using random icon
            $script:missingIcon++
        }
        else
        {
            #write-host using icon $icon
        }
        $percent = $current / $total * 100
        Write-Progress -Activity Creating -Status "$message" -per $percent
        # write-host $message
        foreach($q in $g.Q)
        {
            $question = @{ 
                Prompt = $q.Prompt
                Type =  $typemap[$q.Type]
                TargetPath = $q.MAP 
                }
            if ( $q.Required ) { $question['Mandatory'] = $true }
            if ( $question.Type -eq "inlinelist" ) 
            { 
                if ( ! $q.ListElements.count )
                {
                    Write-Host -for Red ("problem with inline list: " + $q.Prompt)
                }
                else
                {
                    $question['ListElements'] = $q.ListElements 
                }
            }
            if ( $question.Type -eq "list" )
            {
                $enumName = $q.Enumeration
                $question['Enumeration'] = get-scsmenumeration "^${enumName}$"
                if ( ! $question['Enumeration'] )
                {
                    Write-Host -for Red ("could not find enumeration: " + $q.Enumeration)
                }
            }
            $qlist += $question
        }
        $rq = @()
        # create the questions
        $global:badQ = @()
        foreach ( $q in $qlist )
        {
            try
            {
                $rq += new-scsmrequestofferingquestion @q -ea stop
            }
            catch
            {
                $q | out-default  | write-host
                write-host -fore red ("skipping question: '{0}' in RO: '{1}'" -f $q.prompt, $roname)
                write-host -for red $error[0]
            }
        }
        $published = Get-SCSMEnumeration System.Offering.StatusEnum.Published
        # create the request offering


        $newROargs = @{
            ManagementPack = $mp
            Questions = $rq
            DisplayName = $ROName
            Status = $published
            Title = $ROName
            PublishDate = (get-date)
            Whatif = $whatif
            Notes = "${Category},${SOName}"
            NoCommit = $true
            Image = Get-IconStream $ICON
            TargetTemplate = $ObjectTemplate
            }
        # $IDD.Add((New-SCSMRequestOffering @newROargs))
        New-SCSMRequestOffering @newROargs
    }  
}
Write-Progress -status ManagementPack -Activity creating
# the MP to store it all
if ( $test ) { $clobber = $false }
$MP = get-scsmmanagementpack "^${MPNAME}$"
if ( ! $mp )
{
    New-SCManagementPack -ManagementPackName $MPNAME -FriendlyName $MPNAME -DisplayName $MPNAME -version 7.5.1335.1
    $MP = get-scsmmanagementpack "^${MPNAME}$" 
}
else
{
    if ( $clobber )
    {
        $MP | Remove-SCSMManagementPack
        New-SCManagementPack -ManagementPackName $MPNAME -FriendlyName $MPNAME -DisplayName $MPNAME
        $MP = get-scsmmanagementpack "^${MPNAME}$" 
    }
}

Write-Progress -Status Reading -Activity "SpreadSheet"
$rodata = Convert-ROData $filename

Write-Progress -Status Converting -Activity "RO Data"
$ro = ConvertTo-RO $rodata

Write-Progress -Status Testing -Activity "RO Data Validity"
$goodlist,$global:badlist = Test-ROValues $ro -both

$myRoList = new-ro $goodlist # $true # whatif
$ListToCommit = $myRoList | ?{ try { [xml]$_.object.adaptemo().presentationmappingtemplate } catch { ; } } 
# Now find the bad PresentationMappingTemplate
$global:BadPMT = new-object System.Collections.Arraylist
foreach($rr in $myRoList)
{
    try 
    { 
        $j = [xml]($rr.object.adaptemo()).presentationmappingtemplate 
    } 
    catch
    {
        [void]$badPMT.Add($rr)
    } 
}

$TotalRO = $listToCommit.Count
if ( ! $test )
{
$IDD = new-object Microsoft.EnterpriseManagement.ConnectorFramework.IncrementalDiscoveryData
$listToCommit | %{ 
    Write-Progress -Activity "Adding RO to commit package" -status $_.object.adaptemo().title 
    $IDD.Add($_) 
    }
$emg = [Microsoft.EnterpriseManagement.EnterpriseManagementGroup]$mp.Store
Write-Progress -Activity "Committing ROs" -Status "Please wait"
# $idd.Commit($emg)
}
$streamCollection | %{$_.close(); $_.dispose() }
"Total ROs: $TotalRO (missing icon: $missingIcon)"
