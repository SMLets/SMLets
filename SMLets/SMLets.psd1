# Module manifest for CodePlex SMLets module
#
@{
    VariablesToExport        = @()
    CompanyName              = 'CodePlex'
    CLRVersion               = ''
    PowerShellHostVersion    = ''
    FileList                 = @()
    Author                   = 'Sundqvist, Truher, Wright, Gritsenko'
    Copyright                = 'Copyright 2015'
    AliasesToExport          = @()
    ModuleVersion            = '0.5.0.1'
    GUID                     = 'af1da698-e594-4527-bd99-93b0e0dcd94e'
    NestedModules            = join-path $psScriptRoot SMLets.Module.dll
    FunctionsToExport        = '*'
    ModuleToProcess          = 'SMLets.psm1'
    Description              = 'CodePlex Service Manager Cmdlets'
    CmdletsToExport          = '*'
    PowerShellHostName       = ''
    PowerShellVersion        = ''
    ModuleList               = @()
    DotNetFrameworkVersion   = ''
    RequiredAssemblies       = Join-Path $psScriptRoot SMLets.Module.dll
    ProcessorArchitecture    = ''
    PrivateData              = ''
}

