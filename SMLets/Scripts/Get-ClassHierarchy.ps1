param ( [Parameter(Position=0,Mandatory=$true)]$ClassName ) 
$selectedClass = get-scsmclass "^${classname}$"
$script:countOfClasses = 0;
function PrintClasses($class, $levelString) 
{ 
   $keyProperties = @($class.GetProperties(0) | ?{$_.Key -eq $true}); 
   $keyString = ""; 
  
   if ($keyProperties.Count -gt 0) 
   { 
       $keyString = "{" + [string]::Join(",", $keyProperties) + "}" 
   }
   if ($class.Abstract -eq $true) 
   { 
      write-host -foregroundcolor cyan $levelString $class.Name " " -nonewline 
      write-host -foregroundcolor red $keyString 
   } 
   else 
   { 
      write-host -foregroundcolor white $levelString $class.Name " " -nonewline 
      write-host -foregroundcolor red $keyString 
   }
   $script:countOfClasses++; 
    
   $derivedFromClass = @(); 
   $derivedFromClass += $class.GetDerivedTypes(); 
   
   foreach ($derivedClass in $derivedFromClass)  
   { 
      PrintClasses $derivedClass ($levelString + "  ") 
   } 
}
PrintClasses $selectedClass "  " 
write-host "=============================="; 
write-host "Number of classes: " $script:countOfClasses;

