
Import-Module SMLets

$Roles = Get-SCSMUserRole
ForEach ($Role in $Roles)
{
    Write-Host "=================================================="
    Write-Host $Role.DisplayName "(" $Role.ProfileDisplayName ")"
    Write-Host $Role.Description
    Write-Host "=================================================="
    Write-Host "USERS"
    ForEach ($User in $Role.Users)
    {
        Write-Host "  " $User
    }
    Write-Host " "
    Write-Host "VIEWS"
    ForEach ($View in $Role.Views)
    {
        Write-Host "  " $View.DisplayName
    }
    
    Write-Host " "
    Write-Host "OBJECT SCOPES"
    ForEach ($Object in $Role.Objects)
    {
        Write-Host "  " $Object.DisplayName
    }
    
    Write-Host " "
    Write-Host "TEMPLATES"
    ForEach ($Template in $Role.Templates)
    {
        Write-Host "  " $Template.DisplayName
    }
    
    Write-Host " "
    Write-Host "CLASSES"
    ForEach ($Class in $Role.Classes)
    {
        Write-Host "  " $Class.DisplayName
    }
    
    Write-Host " "
    Write-Host "CONSOLE TASKS"
    ForEach ($CredentialTask in $Role.CredentialTasks)
    {
        $T = Get-SCSMConsoleTask $CredentialTask.First
        Write-Host " " $T.DisplayName
    }

    Write-Host " "
    Write-Host "RUNTIME TASKS"    
    ForEach ($NonCredentialTask in $Role.NonCredentialTasks)
    {
        $T = Get-SCSMTask $NonCredentialTask.First
        Write-Host " " $T.DisplayName
    }
}


