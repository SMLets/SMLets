$MAINMODULEGUID = 'fd3061e1-e95e-4ef0-b982-0d33aedb26b3'
$MAINMODULE = @"
      <Component Id='MainModule' Guid='$MAINMODULEGUID' >
        <File Id='about_SMLets.help.txt' Name='about_SMLets.help.txt' DiskId='1' Source='../about_SMLets.help.txt' />
        <File Id='Microsoft.EnterpriseManagement.ServiceManager.Default.xml' Name='Microsoft.EnterpriseManagement.ServiceManager.Default.xml' DiskId='1' Source='../Microsoft.EnterpriseManagement.ServiceManager.Default.xml' />
        <File Id='ReadMe.txt' Name='ReadMe.txt' DiskId='1' Source='../ReadMe.txt' />
        <File Id='SMLets.Module.dll' Name='SMLets.Module.dll' DiskId='1' Source='../SMLets.Module.dll' />
        <File Id='SMLets.Module.dll_Help.xml' Name='SMLets.Module.dll-Help.xml' DiskId='1' Source='../SMLets.Module.dll-Help.xml' />
        <File Id='SMLets.Module.pdb' Name='SMLets.Module.pdb' DiskId='1' Source='../SMLets.Module.pdb' />
        <File Id='SMLets.psd1' Name='SMLets.psd1' DiskId='1' Source='../SMLets.psd1' />
        <File Id='SMLets.psm1' Name='SMLets.psm1' DiskId='1' Source='../SMLets.psm1' />
        <File Id='SMLets.Types.ps1xml' Name='SMLets.Types.ps1xml' DiskId='1' Source='../SMLets.Types.ps1xml' />
        <File Id='SMLets.Format.ps1xml' Name='SMLets.Format.ps1xml' DiskId='1' Source='../SMLets.Format.ps1xml' />
      </Component>
"@
$TESTDIRGUID = '9f8694cc-7040-4947-b9f2-660231a94a78'
$TESTDIR = ls ../test -name | %{
    "<File Id='test_{0}' Name='{1}' DiskId='1' Source='../test/{1}' />" -f ($_ -replace "-","_"),$_
    }
$DATAGENDIRGUID = 'fe656927-93fa-410d-a8cf-ce251871fcaf'
$DATAGENDIR = ls ../DataGen -name | %{
    "<File Id='datagen_{0}' Name='{1}' DiskId='1' Source='../DataGen/{1}' />" -f ($_ -replace "-","_"),$_
    }
$SCRIPTDIRGUID = 'd2a8969d-4950-4261-8329-ad5a81a2229f'
$SCRIPTDIR = ls ../Scripts -name | %{
    "<File Id='Scripts_{0}' Name='{1}' DiskId='1' Source='../Scripts/{1}' />" -f ($_ -replace "-","_"),$_
    }

"<Directory Id='Test' Name='Test'>"
" <Component Id='TestDir' Guid='$TESTDIRGUID'>"
$TESTDIR | %{ "  $_" }
" </Component>"
"</Directory>"


"<Directory Id='Scripts' Name='Scripts'>"
" <Component Id='ScriptsDir' Guid='$SCRIPTDIRGUID'>"
$SCRIPTDIR | %{ "  $_" }
" </Component>"
"</Directory>"

"<Directory Id='DataGen' Name='DataGen'>"
" <Component Id='DataGenDir' Guid='$DATAGENDIRGUID'>"
$DATAGENDIR | %{ "  $_" }
" </Component>"
"</Directory>"
