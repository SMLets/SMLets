using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Subscriptions;
using Microsoft.EnterpriseManagement.Configuration.IO;
using System.Text.RegularExpressions;

namespace SMLets
{   
    [Cmdlet(VerbsCommon.Get, "SCSMWhoAmI")]
    public class GetSCSMWhoAmICommand : ObjectCmdletHelper
    {
        private SwitchParameter _raw;
        [Parameter]
        public SwitchParameter Raw
        {
            get { return _raw; }
            set { _raw = value; }
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            string userName = _mg.GetUserName();
            if (Raw)
            {
                WriteObject(userName);
            }
            else
            {
                WriteObject(UserHelper.GetUserObjectFromString(_mg, userName, this));
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMConnectedUser")]
    public class GetSCSMConnectedUserCommand : ObjectCmdletHelper
    {
        private SwitchParameter _raw;
        [Parameter]
        public SwitchParameter Raw
        {
            get { return _raw; }
            set { _raw = value; }
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (string s in _mg.GetConnectedUserNames())
            {
                if (Raw)
                {
                    WriteObject(s);
                }
                else
                {
                    WriteObject(UserHelper.GetUserObjectFromString(_mg, s, this));
                }
            }
        }
    }

    #region SCSMSession cmdlets

    [Cmdlet("New","SCSMSession")]
    public class NewSCSMSession : SMCmdletBase
    {
        private SwitchParameter _passthru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passthru; }
            set { _passthru = value; }
        }

        protected override void BeginProcessing()
        {
            // A provided session always wins
            if (SCSMSession != null)
            {
                // Make sure that we have this session in our hash table
                ConnectionHelper.SetMG(SCSMSession);
                _mg = SCSMSession;
            }
            else // No session, go hunting
            {
                PSVariable DefaultComputer = SessionState.PSVariable.Get("SMDefaultComputer");
                if (DefaultComputer != null)
                {
                    _mg = ConnectionHelper.GetMG(DefaultComputer.Value.ToString(), this.Credential, this.ThreeLetterWindowsLanguageName);
                }
                else
                {
                    _mg = ConnectionHelper.GetMG(ComputerName,this.Credential, this.ThreeLetterWindowsLanguageName);
                }
            }
        }

        protected override void ProcessRecord()
        {
            if ( PassThru ) { WriteObject(_mg); }
        }
    }
    
    [Cmdlet("Get","SCSMSession")]
    public class GetSCSMSession : PSCmdlet
    {
        private string _computerName = ".*";
        [Parameter(Position=0,ValueFromPipeline=true)]
        public string ComputerName
        {
            get { return _computerName; }
            set { _computerName = value; }
        }
        private List<string> l = null;
        protected override void ProcessRecord()
        {
            l = ConnectionHelper.GetMGList(ComputerName);
            foreach(string n in l) 
            {
                WriteObject(ConnectionHelper.GetMG(n));
            }
        }
    }
    
    [Cmdlet("Remove","SCSMSession")]
    public class RemoveSCSMSession : PSCmdlet
    {
        private EnterpriseManagementGroup _emg;
        [Parameter(Mandatory=true,ValueFromPipeline=true,Position=0)]
        public EnterpriseManagementGroup EMG
        {
            get { return _emg; }
            set { _emg = value; }
        }
        protected override void ProcessRecord()
        {
            ConnectionHelper.RemoveMG(EMG.ConnectionSettings);
        }
    }
    
    #endregion
}
