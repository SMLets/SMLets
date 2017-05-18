using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Subscriptions;
using Microsoft.EnterpriseManagement.Monitoring;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System.Text.RegularExpressions;

namespace SMLets
{
    [Cmdlet(VerbsCommon.Get, "SCSMTask", DefaultParameterSetName = "Guid")]
    public class GetSMTaskCommand : ObjectCmdletHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter(ParameterSetName = "Guid", Position = 0)]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private ManagementPackTaskCriteria _criteria = null;
        [Parameter(ParameterSetName = "Criteria", Position = 0)]
        public ManagementPackTaskCriteria Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }
        protected override void ProcessRecord()
        {
            if (Id == Guid.Empty && Criteria == null)
            {
                foreach (ManagementPackTask t in _mg.TaskConfiguration.GetTasks())
                {
                    WriteObject(t);
                }
            }
            else if (Id != Guid.Empty)
            {
                WriteObject(_mg.TaskConfiguration.GetTask(Id));
            }
            // If someone provides us a filter, we'll use that instead of a criteria
            else if (Criteria != null)
            {
                foreach (ManagementPackTask t in _mg.TaskConfiguration.GetTasks(Criteria))
                {
                    WriteObject(t);
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMTaskResult", DefaultParameterSetName = "Guid")]
    public class GetSMTaskResultCommand : ObjectCmdletHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter(ParameterSetName = "Guid", Position = 0)]
        public Guid BatchId
        {
            get { return _id; }
            set { _id = value; }
        }
        private TaskResultCriteria _criteria = null;
        [Parameter(ParameterSetName = "Criteria", Position = 0)]
        public TaskResultCriteria Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        // since the TaskRuntime moved to the ServiceManagementGroup, we need to create one of those
        // from our current connection
        private ServiceManagementGroup smg;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            ServiceManagementConnectionSettings cSetting = new ServiceManagementConnectionSettings(_mg.ConnectionSettings.ServerName);
            cSetting.UserName = _mg.ConnectionSettings.UserName;
            cSetting.Domain = _mg.ConnectionSettings.Domain;
            cSetting.Password = _mg.ConnectionSettings.Password;
            smg = new ServiceManagementGroup(cSetting);
        }
        protected override void ProcessRecord()
        {
            if (BatchId == Guid.Empty && Criteria == null)
            {
                foreach (TaskResult r in smg.TaskRuntime.GetTaskResults())
                {
                    PSObject o = new PSObject(r);
                    try
                    {
                        XmlDocument x = new XmlDocument();
                        x.LoadXml(r.Output);
                        o.Members.Add(new PSNoteProperty("OutputXML", x));
                    }
                    catch
                    {
                        WriteVerbose("Cannot cast output to XML, ignoring");
                    }
                    WriteObject(o);
                }
            }
            else if (BatchId != Guid.Empty)
            {
                WriteObject(smg.TaskRuntime.GetTaskResultsByBatchId(BatchId));
            }
            // If someone provides us a filter, we'll use that instead of a criteria
            else if (Criteria != null)
            {
                foreach (TaskResult r in smg.TaskRuntime.GetTaskResults(Criteria))
                {
                    WriteObject(r);
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMRule", DefaultParameterSetName = "name")]
    public class GetSMRuleCommand : ObjectCmdletHelper
    {
        private string _name = ".*";
        [Parameter(Position = 0, ParameterSetName = "name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private Guid _id = Guid.Empty;
        [Parameter(Position = 0, ParameterSetName = "id")]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private Regex r = null;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Id == Guid.Empty)
            {
                r = new Regex(Name, RegexOptions.IgnoreCase);
            }
        }
        protected override void ProcessRecord()
        {
            if (Id != Guid.Empty)
            {
                ManagementPackRule rule = _mg.Monitoring.GetRule(Id);
                PSObject o = new PSObject(rule);
                o.Members.Add(new PSNoteProperty("ManagementPack", rule.GetManagementPack()));
                WriteObject(o);
            }
            else
            {
                foreach (ManagementPackRule rule in _mg.Monitoring.GetRules())
                {
                    if (r.Match(rule.Name).Success)
                    {
                        PSObject o = new PSObject(rule);
                        o.Members.Add(new PSNoteProperty("ManagementPack", rule.GetManagementPack()));
                        WriteObject(o);
                    }
                }
            }
        }

    }

}
