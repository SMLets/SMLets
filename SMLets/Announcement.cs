using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.Collections;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;

namespace SMLets
{
    [Cmdlet(VerbsCommon.New, "SCSMAnnouncement", SupportsShouldProcess = true)]
    public class AddAnnouncement : SMCmdletBase
    {
       
        #region Parameters
        private String _DisplayName = null;
        private String _Body = null;
        private String _Priority = null;
        private DateTime _ExpirationDate;
        
        [Parameter(Position = 0,
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The title of the annoucncement.")]
        [ValidateNotNullOrEmpty]
        public string DisplayName
        {
            get{return _DisplayName;}
            set{_DisplayName = value;}
        }
        
        [Parameter(Position = 1,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The body of the announcement")]
        [ValidateNotNullOrEmpty]
        public string Body
        {
            get{return _Body;}
            set{_Body = value;}
        }

        [Parameter(Position = 2,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The priority of the announcement.  Must be exactly 'Low', 'Medium', or 'Critical'.")]
        [ValidateNotNullOrEmpty]
        [ValidateSet("Low","Medium","Critical")]
        public string Priority
        {
            get { return _Priority; }
            set { _Priority = value; }
        }

        [Parameter(Position = 3,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The expiration date of the announcement.  Pass a datetime object.  Convert to UTC time first.  Required.")]
        [ValidateNotNullOrEmpty]
        public DateTime ExpirationDate
        {
            get { return _ExpirationDate; }
            set { _ExpirationDate = value; }
        }

        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }


        #endregion

        protected override void ProcessRecord()
        {
            try
            {
                ManagementPackClass clsAnnouncement = SMHelpers.GetManagementPackClass(ClassTypes.System_Announcement_Item, SMHelpers.GetManagementPack(ManagementPacks.System_AdminItem_Library, _mg), _mg);
                ManagementPackEnumeration enumPriority = null;
                switch (_Priority)
                {
                    case "Low":
                        enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Low, _mg);
                        break;
                    case "Critical":
                        enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Critical, _mg);
                        break;
                    case "Medium":
                        enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Medium, _mg);
                        break;
                    default:
                        enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Medium, _mg);
                        break;
                }

                CreatableEnterpriseManagementObject emo = new CreatableEnterpriseManagementObject(_mg, clsAnnouncement);

                emo[clsAnnouncement, "Id"].Value = System.Guid.NewGuid().ToString();
                if(_DisplayName != null)
                    emo[clsAnnouncement, "DisplayName"].Value = _DisplayName;
                    emo[clsAnnouncement, "Title"].Value = _DisplayName;
                
                if(_Body != null)
                    emo[clsAnnouncement, "Body"].Value = _Body;
                emo[clsAnnouncement, "Priority"].Value = enumPriority.Id;
                emo[clsAnnouncement, "ExpirationDate"].Value = _ExpirationDate;

                emo.Commit();
                if ( _passThru )
                {
                    WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, _mg.EntityObjects.GetObject<EnterpriseManagementObject>(emo.Id, ObjectQueryOptions.Default)));
                }

            }
            catch (Exception)
            {
            }
        }

    }

    [Cmdlet(VerbsCommon.Get, "SCSMAnnouncement", SupportsShouldProcess = true)]
    public class GetAnnouncement : SMCmdletBase
    {

        protected override void ProcessRecord()
        {
            try
            {
                ManagementPackClass clsAnnouncement = SMHelpers.GetManagementPackClass(ClassTypes.System_Announcement_Item, SMHelpers.GetManagementPack(ManagementPacks.System_AdminItem_Library, _mg), _mg);
                foreach(EnterpriseManagementObject emo in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(clsAnnouncement,ObjectQueryOptions.Default))
                {
                    WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, emo));
                }
            }
            catch (Exception)
            {
            }
        }
    }

    [Cmdlet(VerbsCommon.Set, "SCSMAnnouncement", SupportsShouldProcess = true)]
    public class SetAnnouncement : SMCmdletBase
    {

        #region Parameters
        private String _DisplayName = null;
        private String _Body = null;
        private String _Priority = null;
        private DateTime? _ExpirationDate = null;
        private String _InternalID = null;

        [Parameter(Position = 0, HelpMessage = "The title of the announcement.")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        [Parameter(Position = 1, HelpMessage = "The body of the announcement")]
        public string Body
        {
            get { return _Body; }
            set { _Body = value; }
        }

        [Parameter(Position = 2, HelpMessage = "The priority of the announcement.  Must be exactly 'Low', 'Medium', or 'Critical'.")]
        [ValidateSet("Low","Medium","Critical")]
        public string Priority
        {
            get { return _Priority; }
            set { _Priority = value; }
        }

        [Parameter(Position = 3, HelpMessage = "The expiration date of the announcement.  Pass a datetime object.  Convert to UTC time first.")]
        public DateTime? ExpirationDate
        {
            get { return _ExpirationDate; }
            set { _ExpirationDate = value; }
        }

        [Parameter(Position = 4,
        Mandatory = true,
        ParameterSetName = "ById",
        HelpMessage = "The internal ID (GUID) of the announcement.")]
        public String InternalID
        {
            get { return _InternalID; }
            set { _InternalID = value; }
        }

        private EnterpriseManagementObject _announcement = null;
        [Parameter(ValueFromPipeline=true,
            Mandatory=true,
            ParameterSetName = "ByObject")]
        public EnterpriseManagementObject Announcement
        {
            get { return _announcement; }
            set { _announcement = value; }
        }

        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }

        #endregion
    

        private ManagementPackEnumeration enumPriority = null;
        private ManagementPackClass clsAnnouncement = null;
        protected override void BeginProcessing()
        {
            // the base.BeginProcesing must come first as it sets up the enterprisemanagementgroup!
            base.BeginProcessing();
            ManagementPackClass clsAnnouncement = SMHelpers.GetManagementPackClass(ClassTypes.System_Announcement_Item, SMHelpers.GetManagementPack(ManagementPacks.System_AdminItem_Library, _mg), _mg);
            // Compare the string case insenstively
            if ( _Priority != null)
            {
                if ( String.Compare(_Priority, "low", true ) == 0 )
                {
                    enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Low, _mg);
                }
                else if ( String.Compare(_Priority, "critical", true ) == 0 )
                {
                    enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Critical, _mg);
                }
                else if ( String.Compare(_Priority, "medium", true ) == 0 )
                {
                    enumPriority = SMHelpers.GetEnum(Enumerations.System_Announcement_PriorityEnum_Medium, _mg);
                }
                else
                {
                    ThrowTerminatingError( new ErrorRecord(new ArgumentException("Priority"), "Priority must be low/medium/critical", ErrorCategory.InvalidOperation, _Priority));
                }
            }
        }

        protected override void ProcessRecord()
        {
            try
            {

                if ( _announcement != null )
                {
                    WriteVerbose("Setting by instance");

                    bool change = false;
                    if ( _DisplayName != null ) 
                    { 
                        WriteDebug("Setting instance Name");
                        _announcement[null, "DisplayName"].Value = _DisplayName; 
                        _announcement[null,"Title"].Value = _DisplayName; 
                        change = true;
                    }
                    if ( _Body != null ) 
                    {   
                        WriteDebug("Setting instance Body");
                        _announcement[null, "Body"].Value = _Body; 
                        change = true;
                    }
                    if ( _Priority != null ) 
                    { 
                        WriteDebug("Setting instance Priority");
                        _announcement[null, "Priority"].Value = enumPriority.Id; 
                        change = true;
                    }
                    if ( _ExpirationDate != null ) 
                    { 
                        WriteDebug("Setting instance ExpirationDate");
                        _announcement[null, "ExpirationDate"].Value = _ExpirationDate; 
                        change = true;
                    }
                    if ( change )
                    {
                        WriteDebug("Overwriting instance");
                        _announcement.Overwrite();
                        if ( _passThru ) { WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, _announcement)); }
                    }

                }
                else if (_InternalID != null)
                {

                    bool change = false;
                    WriteVerbose("Setting by ID");
                    EnterpriseManagementObject emo = _mg.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid(_InternalID), ObjectQueryOptions.Default);
                    if ( _DisplayName != null ) 
                    { 
                        emo[clsAnnouncement, "DisplayName"].Value = _DisplayName; 
                        emo[clsAnnouncement,"Title"].Value = _DisplayName; 
                        change = true;
                    }
                    if ( _Body != null ) 
                    {   
                        emo[clsAnnouncement, "Body"].Value = _Body; 
                        change = true;
                    }
                    if ( _Priority != null ) 
                    { 
                        emo[clsAnnouncement, "Priority"].Value = enumPriority.Id; 
                        change = true;
                    }
                    if ( _ExpirationDate != null ) 
                    { 
                        emo[clsAnnouncement, "ExpirationDate"].Value = _ExpirationDate; 
                        change = true;
                    }
                    if ( change )
                    {
                        emo.Overwrite();
                        if ( _passThru ) { WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, emo)); }
                    }
                    
                }
            }
            catch (Exception exception)
            {
                WriteError(new ErrorRecord(exception, "Object", ErrorCategory.NotSpecified, "Announcement"));
            }
        }

    }
}
