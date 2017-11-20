using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using Microsoft.EnterpriseManagement.Common;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SMLets
{
    [Cmdlet(VerbsCommon.New, "SCSMIncident", SupportsShouldProcess = true)]
    public class SCSMIncidentNew : SMCmdletBase
    {
        #region Parameters
        private String _Title = null;
        private String _Description = null;
        private String _Impact = null;
        private String _Urgency = null;
        private String _Status = "active";
        private String _Classification = null;
        private String _Source = "console";
        private EnterpriseManagementObjectProjection[] _AffectedCIs;
        private String _AffectedUser = null;
        private String _SupportGroup = null;


        [Parameter(Position = 0,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The title of the incident.")]

        [ValidateNotNullOrEmpty]
        public string Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        [Parameter(Position = 1,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The description of the incident")]

        [ValidateNotNullOrEmpty]
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        [Parameter(Position = 2,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The Impact of the incident (Low/Medium/High)")]

        [ValidateNotNullOrEmpty]
        [ValidateSet("Low","Medium","High")]
        public string Impact
        {
            get { return _Impact; }
            set { _Impact = value; }
        }

        [Parameter(Position = 3,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The Urgency of the incident (Low/Medium/High)")]

        [ValidateNotNullOrEmpty]
        [ValidateSet("Low","Medium","High")]
        public string Urgency
        {
            get { return _Urgency; }
            set { _Urgency = value; }
        }

        [Parameter(Position = 4,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The \"Classification\" of the incident (X/Y/Z)")]

        [ValidateNotNullOrEmpty]
        public string Classification
        {
            get { return _Classification; }
            set { _Classification = value; }
        }

        [Parameter(Position = 5,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The \"Status\" of the incident (X/Y/Z)")]

        [ValidateNotNullOrEmpty]
        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        [Parameter(Position = 6,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The \"Source\" of the incident (Console/Portal/X/Y/Z)")]
        [ValidateNotNullOrEmpty]
        public string Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        /*
        [Parameter(Position = 7,
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Provide if you don't want the cmdlet to work against localhost.")]
        [ValidateNotNullOrEmpty]
        public string ServerName
        {
            get { return _ServerName; }
            set { _ServerName = value; }
        }
        */

        [Parameter(Position = 8,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The \"Affected User\" of the incident. Syntax: \"Domain\\userid\"")]
        [ValidateNotNullOrEmpty]
        public string AffectedUser
        {
            get { return _AffectedUser; }
            set { _AffectedUser = value; }
        }

        [Parameter]
        public string SupportGroup
        {
            get { return _SupportGroup; }
            set { _SupportGroup = value; }
        }

        [Parameter(
           Mandatory = false,
           ValueFromPipeline = true)]
        public EnterpriseManagementObjectProjection[] AffectedCIs
        {
            get { return this._AffectedCIs; }
            set { _AffectedCIs = value; }
        }

        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }

        private IncrementalDiscoveryData idd;
        private int batchSize = 200;
        private int toCommit = 0;
        private SwitchParameter _bulk;
        [Parameter]
        public SwitchParameter Bulk
        {
            get { return _bulk; }
            set { _bulk = value; }
        }

        private DateTime? _createdDate;
        [Parameter]
        public DateTime? CreatedDate
        {
            get { return _createdDate; }
            set { _createdDate = value; }
        }

        #endregion

        private string incidentPrefix = "IR{0}";
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if ( Bulk )
            {
                idd = new IncrementalDiscoveryData();
            }
            // If you can't get the prefix, just prepend 'IR'
            try
            {
                ManagementPackClass incidentSettings = SMHelpers.GetManagementPackClass("System.WorkItem.Incident.GeneralSetting", SMHelpers.GetManagementPack("ServiceManager.IncidentManagement.Library", _mg), _mg );
                EnterpriseManagementObject incidentSettingsInstance = _mg.EntityObjects.GetObject<EnterpriseManagementObject>(incidentSettings.Id, ObjectQueryOptions.Default);
                incidentPrefix = incidentSettingsInstance[null, "PrefixForId"].Value.ToString() + "{0}";
            }
            catch
            {
                // do nothing - incidentPrefix was already set
                ;
            }
            
        }

        protected override void ProcessRecord()
        {
            try
            {
                ManagementPackClass clsIncident = SMHelpers.GetManagementPackClass(ClassTypes.System_WorkItem_Incident, SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Incident_Library, _mg), _mg);

                EnterpriseManagementObjectProjection incidentProjection = new EnterpriseManagementObjectProjection(_mg, clsIncident);

                WriteVerbose("Setting basic properties");
                incidentProjection.Object[clsIncident, "Id"].Value = incidentPrefix;

                incidentProjection.Object[clsIncident, "Title"].Value = this.Title;

                if ( CreatedDate != null )
                {
                    incidentProjection.Object[clsIncident, "CreatedDate"].Value = this.CreatedDate;
                }

                SMHelpers.UpdateIncident(_mg, clsIncident, incidentProjection,
                    this.Impact, this.Urgency, this.Status, this.Classification, this.Source, this.SupportGroup, null, null, this.Description, null);


                if (AffectedCIs!=null)
                {
                    WriteVerbose("Adding affected CIs");
                    foreach (var item in AffectedCIs)
                    {
                        WriteVerbose(string.Format("Adding {0} as affected configuration item.", item.Object.DisplayName));
                        SMHelpers.AddAffectedCI(incidentProjection, item.Object, _mg);
                    }
                }                

                if (AffectedUser != null)
                {
                    WriteVerbose(string.Format("Adding {0} as affected configuration item.", AffectedUser));
                    SMHelpers.AddAffectedUser(incidentProjection, this.AffectedUser, _mg);
                }

                // a bulk operation
                // do in batches of toCommit (set above)
                if ( Bulk )
                {
                    toCommit++;
                    idd.Add(incidentProjection);
                    if ( toCommit >= batchSize )
                    {
                        idd.Commit(_mg);
                        idd = new IncrementalDiscoveryData();
                        toCommit = 0;
                    }
                }
                else
                {
                    incidentProjection.Commit();
                }

                if ( PassThru )
                {
                    //Pass the new object to the pipeline
                    WriteObject(incidentProjection);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "NewIncident", ErrorCategory.InvalidOperation, Title));
            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            if ( Bulk && toCommit > 0)
            {
                idd.Commit(_mg);
            }
        }

        private void RegisterNewIncident(EnterpriseManagementGroup emg, ManagementPackClass clsIncident, EnterpriseManagementObject AffectedCIs)
        {
            
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMIncident", SupportsShouldProcess = true)]
    public class SCSMIncidentGet : SMCmdletBase
    {
        #region Parameters
        private String _ID = null;
        private String _Title = null;
        private String _Impact = null;
        private String _Urgency = null;
        private String _Status = null;
        private String _Classification = null;
        private String _Source = null;
        private TimeSpan _InactiveFor;
        private DateTime _CreatedBefore;
        private DateTime _CreatedAfter;
        // private String _ServerName = "localhost";

        [Parameter(Position = 0,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The id of the incident to retrieve.")]

        [ValidateNotNullOrEmpty]
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        [Parameter(Position = 1,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The title that you want to use in search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        [Parameter(Position = 2,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "The minimum amount of time since the incident was last modified.")]

        [ValidateNotNullOrEmpty]
        public TimeSpan InactiveFor
        {
            get { return _InactiveFor; }
            set { _InactiveFor = value; }
        }

        [Parameter(Position = 3,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a date to the search filter casuing all incidents created after the \"datetime\" to be excluded.")]

        [ValidateNotNullOrEmpty]
        public DateTime CreatedBefore
        {
            get { return _CreatedBefore; }
            set { _CreatedBefore = value; }
        }

        [Parameter(Position = 3,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a date to the search filter casuing all incidents created before the given \"datetime\" to be excluded.")]

        [ValidateNotNullOrEmpty]
        public DateTime CreatedAfter
        {
            get { return _CreatedAfter; }
            set { _CreatedAfter = value; }
        }

        [Parameter(Position = 4,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a impact as a search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Impact
        {
            get { return _Impact; }
            set { _Impact = value; }
        }

        [Parameter(Position = 5,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a urgency as a search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Urgency
        {
            get { return _Urgency; }
            set { _Urgency = value; }
        }

        [Parameter(Position = 6,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a status as a search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        [Parameter(Position = 7,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a classification as a search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Classification
        {
            get { return _Classification; }
            set { _Classification = value; }
        }

        [Parameter(Position = 8,
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        HelpMessage = "Adds a source as a search criteria.")]

        [ValidateNotNullOrEmpty]
        public string Source
        {
            get { return _Source; }
            set { _Source = value; }
        }

        /*
        [Parameter(Position = 9,
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Provide if you don't want the cmdlet to work against localhost.")]
        [ValidateNotNullOrEmpty]
        public string ServerName
        {
            get { return _ServerName; }
            set { _ServerName = value; }
        }
        */
        #endregion

        // These enumerations are used 
        private ManagementPackEnumeration impactBase;
        private ManagementPackEnumeration urgencyBase;
        private ManagementPackEnumeration statusBase;
        private ManagementPackEnumeration classificationBase;


        // This string is used to create the criteria below
        private string criteriaString = @"<SimpleExpression>" +
            "<ValueExpressionLeft><Property>$Context/Property[Type='WorkItem!System.WorkItem.Incident']/{0}$</Property></ValueExpressionLeft>" +
            "<Operator>{1}</Operator>" +
            "<ValueExpressionRight><Value>{2}</Value></ValueExpressionRight>" +
            "</SimpleExpression>";


        // Under some circumstances, the enumeration may not be there, so we need
        // to do these in a try/catch
        private string GetEnumerationDisplayString(EnterpriseManagementSimpleObject so)
        {
            try
            {
                return ((ManagementPackEnumeration)so.Value).DisplayName;
            }
            catch
            {
                if (so.Value != null)
                {
                    WriteWarning("Could not find enumeration: " + so.Value);
                    return so.Value.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        private ManagementPack systemMp;
        private ManagementPack incidentMp;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            // get the BaseEnums, these are used later!
            impactBase = SMHelpers.GetEnum("System.WorkItem.TroubleTicket.ImpactEnum", _mg);
            urgencyBase = SMHelpers.GetEnum("System.WorkItem.TroubleTicket.UrgencyEnum", _mg);
            statusBase = SMHelpers.GetEnum("IncidentStatusEnum", _mg);
            classificationBase = SMHelpers.GetEnum("IncidentClassificationEnum", _mg);
            systemMp = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);
            incidentMp = _mg.ManagementPacks.GetManagementPacks(new ManagementPackCriteria("Name = 'System.WorkItem.Incident.Library'")).First();
        }

        protected override void ProcessRecord()
        {

            ManagementPackClass clsIncident = SMHelpers.GetManagementPackClass(ClassTypes.System_WorkItem_Incident, SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Incident_Library, _mg), _mg);
                
            ManagementPack incidentProjectionMp = SMHelpers.GetManagementPack(ManagementPacks.ServiceManager_IncidentManagement_Library, _mg);
            ManagementPackTypeProjection incidentTypeProjection = SMHelpers.GetManagementPackTypeProjection(TypeProjections.System_WorkItem_Incident_ProjectionType, incidentProjectionMp, _mg);

            WriteVerbose("Starting to build search criteria...");
            List<string> criterias = new List<string>();
            bool haveCriteria = false;


            // Define the query criteria string. 
            // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.              
            StringBuilder incidentCriteria = new StringBuilder(String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                  <Reference Id=""System.WorkItem.Incident.Library"" PublicKeyToken=""{0}"" Version=""{1}"" Alias=""WorkItem"" />
                      <Expression>", incidentMp.KeyToken, incidentMp.Version.ToString()));

            if (this._ID != null)
            {
                haveCriteria = true;
                WriteVerbose(string.Format("Adding \"ID equal {0}\" to search criteria", this.ID));
                criterias.Add(String.Format(criteriaString, "Id", "Equal", _ID));
            }

            if (this._Title != null)
            {
                haveCriteria = true;
                WriteVerbose(string.Format("Adding \"Title like {0}\" to search criteria", this.Title));
                criterias.Add(String.Format(criteriaString, "Title", "Like", _Title));
            }

            if (this.Impact != null)
            {
                haveCriteria = true;
                ManagementPackEnumeration impEnum = SMHelpers.GetEnum(this.Impact, impactBase);
                WriteVerbose(string.Format("Adding \"Impact equal {0}({1})\" to search criteria", impEnum.DisplayName, impEnum.Id));
                criterias.Add(String.Format(criteriaString, "Impact", "Equal", impEnum.Id));
            }

            if (this.Urgency != null)
            {
                haveCriteria = true;
                ManagementPackEnumeration urgEnum = SMHelpers.GetEnum(this.Urgency, urgencyBase);
                WriteVerbose(string.Format("Adding \"Urgency equal {0}({1})\" to search criteria", urgEnum.DisplayName, urgEnum.Id));
                criterias.Add(String.Format(criteriaString, "Urgency", "Equal", urgEnum.Id));
            }

            if (this.Status != null)
            {
                haveCriteria = true;
                ManagementPackEnumeration statEnum = SMHelpers.GetEnum(this.Status, statusBase);
                WriteVerbose(string.Format("Adding \"Status equal {0}({1})\" to search criteria", statEnum.DisplayName, statEnum.Id));
                criterias.Add(String.Format(criteriaString, "Status", "Equal", statEnum.Id));
            }

            if (this.Classification != null)
            {
                haveCriteria = true;
                ManagementPackEnumeration classificationEnum = SMHelpers.GetEnum(this.Classification, classificationBase);
                WriteVerbose(string.Format("Adding \"Classification equal {0}({1})\" to search criteria", classificationEnum.DisplayName, classificationEnum.Id));
                criterias.Add(String.Format(criteriaString, "Classification", "Equal", classificationEnum.Id));
            }

            if (this.CreatedBefore != DateTime.MinValue)
            {
                haveCriteria = true;
                CultureInfo enUsInfo = new CultureInfo("en-US");
                WriteVerbose(string.Format("Adding \"CreatedDate less than {0}\" to search criteria", this.CreatedBefore.ToString()));
                criterias.Add(String.Format(criteriaString, "CreatedDate", "Less", CreatedBefore.ToUniversalTime()));
            }

            if (this.CreatedAfter != DateTime.MinValue)
            {
                haveCriteria = true;
                CultureInfo enUsInfo = new CultureInfo("en-US");
                WriteVerbose(string.Format("Adding \"CreatedDate greater than {0}\" to search criteria", this.CreatedAfter.ToString()));
                criterias.Add(String.Format(criteriaString, "CreatedDate", "Greater", this.CreatedAfter.ToUniversalTime()));
            }

            if (criterias.Count > 1)
            {
                haveCriteria = true;
                for (int i = 0; i < criterias.Count; i++)
                {
                    criterias[i] = "<Expression>" + criterias[i] + "</Expression>";
                }
            }

            if (criterias.Count > 1)
            {
                incidentCriteria.AppendLine("<And>");
            }

            foreach (var item in criterias)
            {
                incidentCriteria.AppendLine(item);
            }

            if (criterias.Count > 1)
            {
                incidentCriteria.AppendLine("</And>");
            }

            incidentCriteria.AppendLine(@"</Expression>
                </Criteria>");

            WriteDebug("Search criteria: " + incidentCriteria.ToString());

            ObjectProjectionCriteria criteria = null;
            if (haveCriteria)
            {
                // Define the criteria object by using one of the criteria strings.
                criteria = new ObjectProjectionCriteria(incidentCriteria.ToString(), incidentTypeProjection, _mg);
                WriteVerbose("Criteria is: " + incidentCriteria.ToString());
            }
            else
            {
                criteria = new ObjectProjectionCriteria(incidentTypeProjection);
                WriteVerbose("Criteria is Projection");
            }
            string[] EnumPropertiesToAdapt = { "Urgency", "Impact", "TierQueue", "Classification", "Status" };
            // For each retrieved type projection, display the properties.
            List<EnterpriseManagementObjectProjection> result = new List<EnterpriseManagementObjectProjection>();
            foreach (EnterpriseManagementObjectProjection projection in
                _mg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(criteria, ObjectQueryOptions.Default))
            {
                if (this.InactiveFor == TimeSpan.Zero || projection.Object.LastModified.Add(this.InactiveFor) < DateTime.UtcNow)
                {
                    //if (this.OlderThan == TimeSpan.Zero || projection.Object.TimeAdded.Add(this.OlderThan) < DateTime.UtcNow)
                    //{
                    //result.Add(projection);
                    WriteVerbose(String.Format("Adding incident \"{0}\" to the pipeline", projection.Object.Name));
                    // WriteObject(projection, false);
                    PSObject o = new PSObject();
                    o.Members.Add(new PSNoteProperty("__base", projection));
                    o.Members.Add(new PSScriptMethod("GetAsXml", ScriptBlock.Create("[xml]($this.__base.CreateNavigator().OuterXml)")));
                    o.Members.Add(new PSNoteProperty("Object", ServiceManagerObjectHelper.AdaptManagementObject(this, projection.Object)));
                    o.Members.Add(new PSNoteProperty("ID", projection.Object[null, "Id"]));
                    o.Members.Add(new PSNoteProperty("Title", projection.Object[null, "Title"]));
                    o.Members.Add(new PSNoteProperty("Description", projection.Object[null, "Description"]));
                    o.Members.Add(new PSNoteProperty("DisplayName", projection.Object[null, "DisplayName"]));
                    o.Members.Add(new PSNoteProperty("Priority", projection.Object[null, "Priority"]));
                    foreach (string s in EnumPropertiesToAdapt)
                    {
                        o.Members.Add(new PSNoteProperty(s, GetEnumerationDisplayString(projection.Object[null, s])));
                    }

                    o.Members.Add(new PSNoteProperty("CreatedDate", projection.Object[null, "CreatedDate"]));
                    o.Members.Add(new PSNoteProperty("LastModified", projection.Object.LastModified.ToLocalTime()));
                    // add the relationship values
                    foreach (KeyValuePair<ManagementPackRelationshipEndpoint, IComposableProjection> helper in projection)
                    {
                        // EnterpriseManagementObject myEMO = (EnterpriseManagementObject)helper.Value.Object;
                        WriteVerbose("Adapting relationship objects: " + helper.Key.Name);
                        String myName = helper.Key.Name;
                        PSObject adaptedEMO = ServiceManagerObjectHelper.AdaptManagementObject(this, helper.Value.Object);
                        if (helper.Key.MaxCardinality > 1)
                        {
                            // OK, this is a collection, so add the critter
                            // This is so much easier in PowerShell
                            if (o.Properties[myName] == null)
                            {
                                o.Members.Add(new PSNoteProperty(myName, new ArrayList()));
                            }
                            ((ArrayList)o.Properties[myName].Value).Add(adaptedEMO);
                        }
                        else
                        {
                            try
                            {
                                o.Members.Add(new PSNoteProperty(helper.Key.Name, adaptedEMO));
                            }
                            catch (ExtendedTypeSystemException e)
                            {
                                WriteVerbose("Readapting relationship object -> collection :" + e.Message);
                                // We should really only get this exception if we
                                // try to add a create a new property which already exists
                                Object currentPropertyValue = o.Properties[myName].Value;
                                ArrayList newValue = new ArrayList();
                                newValue.Add(currentPropertyValue);
                                newValue.Add(adaptedEMO);
                                o.Properties[myName].Value = newValue;

                                // WriteError(new ErrorRecord(e, "AddAssociatedObject", ErrorCategory.InvalidOperation, p));
                            }
                        }
                    }


                    o.TypeNames[0] = String.Format(CultureInfo.CurrentCulture, "EnterpriseManagementObjectProjection#{0}", incidentTypeProjection.Name);
                    WriteObject(o);
                    //}                    
                }
            }

        }

    }

    [Cmdlet(VerbsCommon.Set, "SCSMIncident", SupportsShouldProcess = true)]
    public class SCSMIncidentSet : SMCmdletBase
    {
        #region Parameters
        private String _ID = null;
        private String _Description = null;
        private String _Impact = null;
        private String _Urgency = null;
        private String _Status = null;
        private String _Comment = null;
        private String _UserComment = null;
        private String _Classification = null;
        private String _Source = null;
        private String _AttachmentPath = null;
        private String _SupportGroup = null;
        // private String _ServerName = "localhost";
        private EnterpriseManagementObjectProjection[] _InputObject;
        private ManagementPackClass clsIncident;
        private ManagementPack systemMp;
        private bool hasChanged = false;
        [Parameter(Position = 0,
            ParameterSetName = "ID",
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The id of the incident to update.")]

        [ValidateNotNullOrEmpty]
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        [Parameter(Position = 0,
           ParameterSetName = "InputObject",
           Mandatory = true,
           ValueFromPipeline = true)]
        public EnterpriseManagementObjectProjection[] InputObject
        {
            get { return this._InputObject; }
            set { _InputObject = value; hasChanged = true; }
        }

        [Parameter(Position = 1,
            HelpMessage = "The description of the incident")]

        [ValidateNotNullOrEmpty]
        public string Description
        {
            get { return _Description; }
            set { _Description = value; hasChanged = true; }
        }

        [Parameter(Position = 2,
            HelpMessage = "The Impact of the incident (Low/Medium/High)")]

        [ValidateNotNullOrEmpty]
        [ValidateSet("Low","Medium","High")]
        public string Impact
        {
            get { return _Impact; }
            set { _Impact = value; hasChanged = true; }
        }

        [Parameter(Position = 3,
            HelpMessage = "The Urgency of the incident (Low/Medium/High)")]

        [ValidateNotNullOrEmpty]
        [ValidateSet("Low","Medium","High")]
        public string Urgency
        {
            get { return _Urgency; }
            set { _Urgency = value; hasChanged = true; }
        }

        [Parameter(Position = 4,
            HelpMessage = "The Status of the incident (Active/Pending/Resolved/Closed)")]
        [ValidateNotNullOrEmpty]
        public string Status
        {
            get { return _Status; }
            set { _Status = value; hasChanged = true; }
        }

        [Parameter(Position = 5,
            HelpMessage = "A comment that will be added to the action log")]
        [ValidateNotNullOrEmpty]
        public string Comment
        {
            get { return _Comment; }
            set { _Comment = value; hasChanged = true; }
        }

        [Parameter(HelpMessage = "A user comment that will be added to the action log")]
        public string UserComment
        {
            get { return _UserComment; }
            set { _UserComment = value; hasChanged = true; }
        }

        [Parameter(Position = 6,
            HelpMessage = "A path to the file you want to attach to the incident")]
        [ValidateNotNullOrEmpty]
        public string AttachmentPath
        {
            get { return _AttachmentPath; }
            set { _AttachmentPath = value; hasChanged = true; }
        }

        [Parameter(Position = 7, HelpMessage = "Incident source")]
        [ValidateNotNullOrEmpty]
        public string Source
        {
            get { return _Source; }
            set { _Source = value; hasChanged = true; }
        }

        [Parameter(Position = 8, HelpMessage = "Incident classification")]
        [ValidateNotNullOrEmpty]
        public string Classification
        {
            get { return _Classification; }
            set { _Classification = value; hasChanged = true; }
        }

        [Parameter]
        public string SupportGroup
        {
            get { return _SupportGroup; }
            set { _SupportGroup = value; hasChanged = true; }
        }


        #endregion
        private ManagementPack workitemMp;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            workitemMp = _mg.ManagementPacks.GetManagementPacks(new ManagementPackCriteria("Name = 'System.WorkItem.Library'")).First();
        }

        protected override void ProcessRecord()
        {
            // If nothing has changed, stop
            if ( ! hasChanged ) 
            { 
                ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException("No values have changed, please provide new values for the incident"), 
                        "Incident", 
                        ErrorCategory.InvalidOperation, 
                        "NoNewValues")
                    );
            }
            try
            {
                systemMp = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);

                try
                {
                    clsIncident = SMHelpers.GetManagementPackClass(ClassTypes.System_WorkItem_Incident, SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Incident_Library, _mg), _mg);
                }
                catch
                {
                    try
                    {
                        // last ditch try to get a class (this happens if a debug build is used
                        clsIncident = _mg.EntityTypes.GetClasses(new ManagementPackClassCriteria("Name = 'System.WorkItem.Incident'")).First();
                    }
                    catch (Exception e)
                    {
                        ThrowTerminatingError(new ErrorRecord(e, "badclass", ErrorCategory.ObjectNotFound, "System.WorkItem.Incident"));
                    }
                    
                }

                int i = 1;
                if (InputObject != null)
                {
                    foreach (var item in this.InputObject)
                    {
                        ProgressRecord prog = new ProgressRecord(1, "Updating " + item.Object.DisplayName, String.Format("{0} of {1}", i, this.InputObject.Length.ToString()));
                        WriteProgress(prog);
                        SMHelpers.UpdateIncident(_mg,clsIncident,item,this.Impact,this.Urgency,this.Status,this.Classification,this.Source,this.SupportGroup, this.Comment,this.UserComment,this.Description,this.AttachmentPath);
                        i++;
                    }                    
                }
                else
                {
                    WriteDebug("No input object passed");
                    if (this._ID!=null)
                    {
                        UpdateNamedIncident(_mg, this._ID);
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "BadSet", ErrorCategory.InvalidOperation, InputObject));
            }
        }

        private void UpdateNamedIncident(EnterpriseManagementGroup emg, string id)
        {
            // Define the type projection that you want to query for.
            // This example queries for type projections defined by the System.WorkItem.Incident.ProjectionType
            // type in the ServiceManager.IncidentManagement.Library management pack.
            ManagementPackTypeProjection incidentTypeProjection = SMHelpers.GetManagementPackTypeProjection(TypeProjections.System_WorkItem_Incident_ProjectionType, SMHelpers.GetManagementPack(ManagementPacks.ServiceManager_IncidentManagement_Library, emg), emg);

            // Define the query criteria string. 
            // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.              
            string incidentCriteria = String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                  <Reference Id=""System.WorkItem.Library"" PublicKeyToken=""{0}"" Version=""{1}"" Alias=""WorkItem"" />
                      <Expression>
                            <SimpleExpression>
                              <ValueExpressionLeft>
                                <Property>$Context/Property[Type='WorkItem!System.WorkItem']/Id$</Property>
                              </ValueExpressionLeft>
                              <Operator>Equal</Operator>
                              <ValueExpressionRight>
                                <Value>" + _ID + @"</Value>
                              </ValueExpressionRight>
                            </SimpleExpression>
                      </Expression>
                </Criteria>
                ", workitemMp.KeyToken, workitemMp.Version.ToString());


            WriteDebug(incidentCriteria);
            // Define the criteria object by using one of the criteria strings.
            ObjectProjectionCriteria criteria = new ObjectProjectionCriteria(incidentCriteria, incidentTypeProjection, emg);

            EnterpriseManagementObjectProjection emop = null;

            // For each retrieved type projection, display the properties.
            foreach (EnterpriseManagementObjectProjection projection in
                emg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(criteria, ObjectQueryOptions.Default))
            {
                emop = projection;
            }

            if (emop != null)
            {
                SMHelpers.UpdateIncident(emg, clsIncident, emop, this.Impact, this.Urgency, this.Status, this.Classification, this.Source, this.SupportGroup, this.Comment, this.UserComment, this.Description, this.AttachmentPath);
            }
            else
            {
                WriteError(new ErrorRecord(new ObjectNotFoundException(_ID), "Incident not found", ErrorCategory.ObjectNotFound, criteria));
                return;
            }
        }

        
    }
}
