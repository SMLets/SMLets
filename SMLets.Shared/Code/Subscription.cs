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
    [Cmdlet("Get","SCSMSubscription")]
    public class GetSMSubscriptionCommand : SMCmdletBase
    {
        private string _filter = "Name LIKE '%'";
        [Parameter(ValueFromPipeline=true)]
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
        protected override void ProcessRecord()
        {
            ManagementPackRuleCriteria criteria = new ManagementPackRuleCriteria(Filter);
            foreach (WorkflowSubscriptionBase ws in _mg.Subscription.GetSubscriptionsByCriteria(criteria))
            {
                WriteObject(ws);
            }

        }
    }
    
    [Cmdlet("Remove","SCSMSubscription", SupportsShouldProcess=true)]
    public class RemoveSMSubscriptionCommand : SMCmdletBase
    {
        private WorkflowSubscription _subscription;
        [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true)]
        public WorkflowSubscription Subscription
        {
            get { return _subscription; }
            set { _subscription = value; }
        }
        protected override void ProcessRecord()
        {
            try
            {
                if ( ShouldProcess(_subscription.Name))
                {
                    _subscription.ManagementGroup.Subscription.DeleteSubscription(_subscription);
                }
            }
            catch ( Exception e )
            {
                WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, _subscription));
            }

        }
    }

    [Cmdlet("New", "SCSMNotificationSubscription")]
    public class NewSMNotificationSubscriptionCommand : SMCmdletBase
    {
        private string _criteria = String.Empty;
        private string _displayname = String.Empty;
        private string _description = String.Empty;
        private OperationTypeEnum _operationtypeenum;
        private OperationType _operationtype;
        private ManagementPackClass _class;
        private EnterpriseManagementObject[] _recipients;
        private ManagementPack _managementpack;
        private ManagementPackObjectTemplate _template;
        private bool _enabled = true; 
        
        
        public enum OperationTypeEnum
        {
            Add,
            Update
            //Remove - note supported for now
        }

        [Parameter(ValueFromPipeline = false, Mandatory=false)]
        public string Criteria
        {
            get { return _criteria; }
            set { _criteria = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public OperationTypeEnum OperationType
        {
            get { return _operationtypeenum; }
            set { _operationtypeenum = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public ManagementPackClass Class
        {
            get { return _class; }
            set { _class = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public string DisplayName
        {
            get { return _displayname; }
            set { _displayname = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public EnterpriseManagementObject[] Recipients
        {
            get { return _recipients; }
            set { _recipients = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public ManagementPack ManagementPack
        {
            get { return _managementpack; }
            set { _managementpack = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public ManagementPackObjectTemplate NotificationTemplate
        {
            get { return _template; }
            set { _template = value; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            switch(_operationtypeenum)
            {
                case OperationTypeEnum.Add:
                    _operationtype = Microsoft.EnterpriseManagement.Subscriptions.OperationType.Add;
                    break;
                case OperationTypeEnum.Update:
                    _operationtype = Microsoft.EnterpriseManagement.Subscriptions.OperationType.Update;
                    break;
            }
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            //Create subscription and set configurable properties
            InstanceTypeSubscription instancetypeSubscription = new InstanceTypeSubscription(_operationtype,_class.Id, _criteria );   
            NotificationSubscription subscription = new NotificationSubscription(_displayname, _description, instancetypeSubscription);
            subscription.Enabled = _enabled;
            subscription.TemplateIds.Add(_template.Id);
            subscription.Name = SMHelpers.MakeMPElementSafeUniqueIdentifier("NotificationSubscription");
            
            //TODO: Do we need these or do they have defaults set?
            //subscription.MaximumRunningTimeSeconds = 7200;
            //subscription.RetryDelaySeconds = 60;
            //subscription.EnableBatchProcessing = true;
            
            //Add recipient users
            if(_recipients != null)
            {
                foreach(EnterpriseManagementObject recipient in _recipients)
                {
                    subscription.Recipients.Add(new NotificationSubscriptionRecipient(recipient.Id.ToString(), NotificationSubscriptionRecipientType.ToRecipient));
                }
            }

            _mg.Subscription.InsertSubscription(_managementpack.Id, subscription);
        }
    }
}
