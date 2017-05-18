using System;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
namespace SMLets
{
    public class SMLetsVersionInfo
    {
        public string TargetProduct = "_TARGETPRODUCT_";
        public string WorkingCopyRootPath = "_WORKINGCOPYROOTPATH_";
        public string URL = "_URL_";
        public string RepositoryRoot = "_REPOSITORYROOT_";
        public string RepositoryUUID = "_REPOSITORYUUID_";
        public string Revision = "_REVISION_";
        public string LastChangedAuthor = "_LASTCHANGEDAUTHOR_";
        public string LastChangedRev = "_LASTCHANGEDREV_";
        public string LastChangedDate = "_LASTCHANGEDDATE_";
        public bool   IsPrivate = _PRIVATE_;
        public string[] Changes = { "_CHANGES_" };
        public string SMCompiledVersion = "_SMCOREVERSION_";
        public string SMInstalledVersion;
#if ( _SERVICEMANAGER_R2_ )
        public bool SM2012 = true; 
#else
        public bool SM2012 = false; 
#endif
        private SMLetsVersionInfo() { ; }
        public SMLetsVersionInfo(SMCmdletBase cmdlet)
        {
            SMInstalledVersion = cmdlet._mg.Version.ToString();
        }
        public SMLetsVersionInfo(EnterpriseManagementGroup emg)
        {
            SMInstalledVersion = emg.Version.ToString();
        }

    }
    [Cmdlet(VerbsCommon.Get, "SMLetsVersion")]
    public class GetSMLetsVersionCommand : SMCmdletBase
    {
        protected override void ProcessRecord()
        {
            WriteObject(new SMLetsVersionInfo(this));
        }
    }
}
