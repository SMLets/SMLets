#if ( _SERVICEMANAGER_R2_ )
using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System.Text.RegularExpressions;

namespace SMLets
{
    [Cmdlet(VerbsCommon.Get, "SCSMRequestOffering", DefaultParameterSetName = "NAME")]
    public class GetSCSMRequestOfferingCommand : GetSCSMROSO
    {
        public override void SetClassType(ManagementPackClass c)
        {
            base.SetClassType(c);
        }
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            SetClassType(SMHelpers.GetManagementPackClass("System.RequestOffering", SMHelpers.GetManagementPack("System.ServiceCatalog.Library", _mg)));
            SetProjectionType(SMHelpers.GetManagementPackTypeProjection("System.RequestOffering.ProjectionType", SMHelpers.GetManagementPack("ServiceManager.ServiceCatalog.Library", _mg)));
        }
    }
    [Cmdlet(VerbsCommon.Get, "SCSMServiceOffering", DefaultParameterSetName = "NAME")]
    public class GetSCSMServiceOfferingCommand : GetSCSMROSO
    {
        public override void SetClassType(ManagementPackClass c)
        {
            base.SetClassType(c);
        }
       
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            SetClassType(SMHelpers.GetManagementPackClass("System.ServiceOffering", SMHelpers.GetManagementPack("System.ServiceCatalog.Library", _mg)));
            SetProjectionType(SMHelpers.GetManagementPackTypeProjection("System.ServiceOffering.ProjectionType", SMHelpers.GetManagementPack("ServiceManager.ServiceCatalog.Library", _mg)));
        }
    }

    public class GetSCSMROSO : ObjectCmdletHelper
    {

        private string[] _name = {"*"};
        [Parameter(Position = 0, ParameterSetName = "NAME",ValueFromPipeline=true)]
        public string[] Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string[] _displayName;
        [Parameter(ParameterSetName = "DISPLAYNAME")]
        public string[] DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        private string[] _id;
        [Parameter(Position=0,ParameterSetName = "ID", ValueFromPipeline = true)]
        public string[] Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private SwitchParameter _published;
        [Parameter]
        public SwitchParameter Published
        {
            get { return _published; }
            set { _published = value; }
        }

        Object[] myParamVals;
        private ManagementPackClass ROSOclass;
        private ManagementPackTypeProjection ROSOprojection;
        public virtual void SetClassType(ManagementPackClass c)
        {
            ROSOclass = c;
        }
        public virtual void SetProjectionType(ManagementPackTypeProjection p)
        {
            ROSOprojection = p;
        }
        private List<string> filters;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            filters = new List<string>();
        }



        protected override void ProcessRecord()
        {
            string myProperty = "NAME";
            switch (ParameterSetName)
            {
                case "NAME":  
                    {
                        myParamVals = Name;
                        myProperty = "Name";
                        break;
                    }
                case "DISPLAYNAME": 
                    {
                        myParamVals = DisplayName;
                        myProperty = "DisplayName";
                        break;
                    }
                case "ID":
                    {
                        myParamVals = Id;
                        myProperty = "Id";
                        break;
                    }
                default:
                    { 
                    ThrowTerminatingError(new ErrorRecord(new ArgumentException("unknown parameter set"), "bad parameter set", ErrorCategory.InvalidArgument, ParameterSetName)); 
                    break; 
                }
            }
            string OPERATOR = "=";
            foreach (string s in myParamVals)
            {
                string myS = s.Replace("*", "%");
                WriteVerbose(s + " => " + myS);
                if (myS.IndexOf("%") > -1)
                {
                    OPERATOR = "LIKE";
                }
                filters.Add(String.Format("{0} {1} '{2}'", myProperty, OPERATOR, myS));
            }
        }
        protected override void EndProcessing()
        {
            base.EndProcessing();
            string combinedFilter = String.Join(" OR ", filters.ToArray<string>());
            WriteVerbose(combinedFilter);
            try
            {
                EnterpriseManagementObjectGenericCriteria myc = new EnterpriseManagementObjectGenericCriteria(combinedFilter);

                foreach (EnterpriseManagementObject o in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(myc, ROSOclass, ObjectQueryOptions.Default))
                {
                    // This should really be done as criteria, but I don't want to change the way that I use generic criteria above
                    if ( Published)
                    {
                        try // if things fall over, just keep going
                        {
                            if (o[null, "Status"].Value.ToString() != "System.Offering.StatusEnum.Published")
                            {
                                continue;
                            }
                        }
                        catch
                        {
                            ;
                        }
                    }
                    string pcr = String.Format("ID -eq '{0}'", o[null, "ID"].Value.ToString());
                    WriteVerbose(pcr);
                    String pcc = ConvertFilterToCriteriaString(o.GetLeastDerivedNonAbstractClass(), pcr, true);
                    WriteVerbose(":" + pcc + ":");
                    ObjectProjectionCriteria pc = new ObjectProjectionCriteria(pcc ,ROSOprojection,_mg);

                    foreach (EnterpriseManagementObjectProjection p in _mg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(pc, ObjectQueryOptions.Default))
                    {
                        WriteObject(ServiceManagerObjectHelper.AdaptProjection(this, p, ROSOprojection.Name));
                        // WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(o));
                    }
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "bad generic criteria", ErrorCategory.InvalidArgument, combinedFilter));
            }
        }
    }
    [Cmdlet(VerbsCommon.Remove, "SCSMRequestOffering", SupportsShouldProcess = true)]
    public class RemoveSCSMRequestOfferingCommand : RemoveOffering
    {
        protected override void SetObjectClassName(string name)
        {
            base.SetObjectClassName("System.RequestOffering");
        }
        
    }

    [Cmdlet(VerbsCommon.Remove, "SCSMServiceOffering", SupportsShouldProcess = true)]
    public class RemoveSCSMServiceOfferingCommand : RemoveOffering
    {
        protected override void SetObjectClassName(string name)
        {
            base.SetObjectClassName("System.ServiceOffering");
        }
    }


    public class RemoveOffering : ObjectCmdletHelper
    {
        private EnterpriseManagementObjectProjection[] _projection;
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public EnterpriseManagementObjectProjection[] Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }
        private string _objectClassName = "System.ServiceOffering";
        protected virtual void SetObjectClassName(string name)
        {
            _objectClassName = name;
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (EnterpriseManagementObjectProjection p in Projection)
            {
                if (String.Compare(p.Object.GetLeastDerivedNonAbstractClass().Name, _objectClassName, true) == 0)
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("Invalid Projection Type"), 
                        "ProjectionNotServiceOffering", ErrorCategory.InvalidArgument, p));
                }
                else
                {
                    if (ShouldProcess(p.Object.DisplayName))
                    {
                        p.Remove();
                        p.Overwrite();
                    }
                }
            }
        }
    }
    [Cmdlet(VerbsCommon.Add, "SCSMRequestOffering", SupportsShouldProcess = true)]
    public class AddSCSMRequestOfferingCommand : PSCmdlet
    {
        #region privates
        private EnterpriseManagementObject[] _requestOffering;
        private EnterpriseManagementObjectProjection[] _serviceOffering;
        private ManagementPackRelationship RelatesToRequestOffering;
        private IncrementalDiscoveryData idd;
        #endregion
        #region parameters
        [Parameter(Position=0,Mandatory=true)]
        public EnterpriseManagementObject[] RequestOffering
        {
            get { return _requestOffering; }
            set { _requestOffering = value; }
        }
        [Parameter(Position = 1, Mandatory = true)]
        public EnterpriseManagementObjectProjection[] ServiceOffering
        {
            get { return _serviceOffering; }
            set { _serviceOffering = value; }
        }
        #endregion
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            RelatesToRequestOffering = RequestOffering[0].ManagementGroup.EntityTypes.GetRelationshipClasses(new ManagementPackRelationshipCriteria("Name = 'System.ServiceOfferingRelatesToRequestOffering'")).First();
            idd = new IncrementalDiscoveryData();
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (EnterpriseManagementObjectProjection p in ServiceOffering)
            {
                foreach (EnterpriseManagementObject e in RequestOffering)
                {
                    if (ShouldProcess(p.Object.DisplayName))
                    {
                        p.Add(e, RelatesToRequestOffering.Target);
                        idd.Add(p);
                    }
                }
            }
        }
        protected override void EndProcessing()
        {
            base.EndProcessing();
            if (ShouldProcess("Save All Changes"))
            {
                idd.Commit(RequestOffering[0].ManagementGroup);
            }
        }
    }
    [Cmdlet(VerbsCommon.New, "SCSMRequestOffering", SupportsShouldProcess = true)]
    public class NewSCSMRequestOfferingCommand : ObjectCmdletHelper
    {
        #region privates
        private String _briefDescription;
        private String _comment;
        private String _displayName;
        private String _estimatedTimeToCompletion;
        private Stream _image;
        private String _notes;
        private String _overview;
        private ROMapElement[] _presentationMappingTemplate;
        private DateTime ? _publishDate;
        private string _status;
        private ManagementPackObjectTemplate _targetTemplate;
        private ManagementPack _managementPack;
        private EnterpriseManagementObject[] _relatedKnowledgeArticle;
        private EnterpriseManagementObject _publishedBy;
        private EnterpriseManagementObject _owner;
        private SwitchParameter _passThru;
        private ManagementPackClass _roClass;
        private ManagementPackTypeProjection _roProjection;
        private SwitchParameter _noCommit;
        #endregion

        #region parameters
        [Parameter]
        public String BriefDescription
        {
            get { return _briefDescription; }
            set { _briefDescription = value; }
        }
        [Parameter]
        public String Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }
        [Parameter]
        public String DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        [Parameter]
        public String EstimatedTimeToCompletion
        {
            get { return _estimatedTimeToCompletion; }
            set { _estimatedTimeToCompletion = value; }
        }
        [Parameter]
        public Stream Image
        {
            get { return _image; }
            set { _image = value; }
        }
        [Parameter]
        public String Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }
        [Parameter]
        public String Overview
        {
            get { return _overview; }
            set { _overview = value; }
        }
        [Parameter]
        public ROMapElement[] Questions
        {
            get { return _presentationMappingTemplate; }
            set { _presentationMappingTemplate = value; }
        }
        [Parameter]
        public DateTime ? PublishDate
        {
            get { return _publishDate; }
            set { _publishDate = value; }
        }
        [Parameter]
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }
        [Parameter]
        public ManagementPackObjectTemplate TargetTemplate
        {
            get { return _targetTemplate; }
            set { _targetTemplate = value; }
        }
        private String _title;
        [Parameter(Mandatory = true)]
        public String Title
        {
            get { return _title; }
            set { _title = value; }
        }
        [Parameter(Mandatory = true)]
        public ManagementPack ManagementPack
        {
            get { return _managementPack; }
            set { _managementPack = value; }
        }
        [Parameter]
        public EnterpriseManagementObject[] RelatedKnowledgeArticle
        {
            get { return _relatedKnowledgeArticle; }
            set { _relatedKnowledgeArticle = value; }
        }
        [Parameter]
        public EnterpriseManagementObject PublishedBy
        {
            get { return _publishedBy; }
            set { _publishedBy = value; }
        }
        [Parameter]
        public EnterpriseManagementObject Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        [Parameter]
        public SwitchParameter NoCommit
        {
            get { return _noCommit; }
            set { _noCommit = value; }
        }
        #endregion
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _roClass = SMHelpers.GetManagementPackClass("System.RequestOffering", SMHelpers.GetManagementPack("System.ServiceCatalog.Library", _mg));
            _roProjection = SMHelpers.GetManagementPackTypeProjection("System.RequestOffering.ProjectionType", SMHelpers.GetManagementPack("ServiceManager.ServiceCatalog.Library", _mg));
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            if (TargetTemplate == null)
            {
                TargetTemplate = _mg.Templates.GetObjectTemplates(new ManagementPackObjectTemplateCriteria("Name = 'ServiceManager.ServiceRequest.Library.Template.DefaultServiceRequest'")).First();
            }
            EnterpriseManagementObjectProjection junk = new EnterpriseManagementObjectProjection(_mg, TargetTemplate);
            IEnumerable<string> propertynames = from property in junk.Object.GetProperties() select property.Name;
            ROMap mappingTemplate = new ROMap(ManagementPack);
            foreach(ROMapElement e in Questions)
            {
                if ( propertynames.Contains<string>(e.TargetPath, StringComparer.InvariantCulture))
                {
                    mappingTemplate.AddQuestion(e);
                }
                else
                {
                    WriteError(new ErrorRecord(new ArgumentException("Property '" + e.TargetPath + "' does not exist"), "BadQuestionTarget", ErrorCategory.InvalidArgument, e));
                }
            }

            string ROName = "Offering." + Guid.NewGuid().ToString("N");
            CreatableEnterpriseManagementObject cemo = new CreatableEnterpriseManagementObject(_mg, _roClass);
            cemo[null, "ID"].Value = ExtensionIdentifier.CreateObjectIdentifier(ManagementPack, ROName, ExtensionIdentifier.CreateTypeIdentifier("RequestOffering")).ToString();

            if (Title != null)  { cemo[null, "Title"].Value = Title; }
            if (BriefDescription != null) { cemo[null, "BriefDescription"].Value = BriefDescription; }
            if ( Comment != null ) { cemo[null, "Comment"].Value = Comment; }
            if ( DisplayName != null) { cemo[null, "DisplayName"].Value = DisplayName; }
            cemo[null, "Domain"].Value = ManagementPack.Name;
            if ( EstimatedTimeToCompletion != null ) { cemo[null, "EstimatedTimeToCompletion"].Value = EstimatedTimeToCompletion; }
            if ( Notes != null ) { cemo[null, "Notes"].Value = Notes; }
            if ( Overview != null) { cemo[null, "Overview"].Value = Overview; }
            if ( Image != null ) { cemo[null, "Image"].Value = Image; }
            cemo[null, "Path"].Value = ROName;
            cemo[null, "PresentationMappingTemplate"].Value = mappingTemplate.ToString();
            if ( PublishDate != null) { cemo[null, "PublishDate"].Value = PublishDate; }
            if ( Status != null ) { cemo[null, "Status"].Value = SMHelpers.GetEnum(Status, _mg); }
            else { cemo[null, "Status"].Value = SMHelpers.GetEnum("Draft", _mg); }
            // TargetTemplate is an object template, usually a service request, but could be an incident
            cemo[null, "TargetTemplate"].Value = TargetTemplate.Identifier.ToString();

            // OK - we've created the bits we needed to create, now hook up the projection
            // and attache the publisher and owner



            EnterpriseManagementObjectProjection emop = new EnterpriseManagementObjectProjection(cemo);
            if ( RelatedKnowledgeArticle != null )
            {
                ITypeProjectionComponent rel = _roProjection["RelatedKnowledgeArticles"];
                foreach (EnterpriseManagementObject emo in RelatedKnowledgeArticle)
                {
                    emop.Add(emo, rel.TargetEndpoint);
                }
            }
            if (PublishedBy != null)
            {
                ManagementPackRelationship pubRel = _mg.EntityTypes.GetRelationshipClasses(new ManagementPackRelationshipCriteria("Name = 'System.OfferingPublishedBy'")).First();
                emop.Add(PublishedBy, pubRel.Target);
            }
            if (Owner != null)
            {
                ManagementPackRelationship ownRel = _mg.EntityTypes.GetRelationshipClasses(new ManagementPackRelationshipCriteria("Name = 'System.OfferingOwner'")).First();
                emop.Add(Owner, ownRel.Target);
            }
            if ( NoCommit )
            {
                WriteObject(emop);
                return;
            }
            if (ShouldProcess(emop.Object.DisplayName))
            {
                try
                {
                    emop.Commit();
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, "CommitFailure", ErrorCategory.NotSpecified, emop));
                }
            }
            if (PassThru) { WriteObject(emop); }
        }
    }
    // This cmdlet can auto generate Service Request data based on a Request Offering
    // or based on a question collection, it can create the Service Request (or anything that the Request Offering
    // Point to
    [Cmdlet(VerbsCommon.New, "SCSMServiceRequest", SupportsShouldProcess = true)]
    public class NewSCSMServiceRequestCommand : PSCmdlet
    {
        #region parameters
        private EnterpriseManagementObjectProjection _requestOffering;
        [Parameter(Position=0,Mandatory=true,ValueFromPipelineByPropertyName=true)]
        public EnterpriseManagementObjectProjection RequestOffering
        {
            get { return _requestOffering; }
            set { _requestOffering = value; }
        }
        private SwitchParameter _auto;
        [Parameter(ParameterSetName="AutoGenerate")]
        public SwitchParameter Auto
        {
            get { return _auto; }
            set { _auto = value; WriteVerbose("This is not activated yet"); }
        }
        private SwitchParameter _noCommit;
        [Parameter]
        public SwitchParameter NoCommit
        {
            get { return _noCommit; }
            set { _noCommit = value; }
        }
        private RequestOfferingQuestion[] _question;
        [Parameter(ParameterSetName = "Question",Position=1,ValueFromPipelineByPropertyName=true)]
        public RequestOfferingQuestion[] Question
        {
            get { return _question; }
            set { _question = value; }
        }
        #endregion
        private Stream fileAttachement = null;
        private ManagementPackRelationship workItemHasFileAttachment;
        private ManagementPackRelationship workItemHasRequestOffering;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        private string GetIdPrefix(EnterpriseManagementObjectProjection p)
        {
            string prefix = String.Empty;
            try
            {
                // support two different types ServiceRequest and Incident
                ManagementPackClass c = p.Object.GetLeastDerivedNonAbstractClass();
                Regex r = new Regex("Incident", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                ManagementPackClass settingsClass;
                string propertyName;
                if (r.Match(c.Name).Success)
                {
                    settingsClass = p.Object.ManagementGroup.EntityTypes.GetClasses(new ManagementPackClassCriteria("Name = 'System.WorkItem.Incident.GeneralSetting'")).First();
                    propertyName = "PrefixForId";
                }
                else
                {
                    settingsClass = p.Object.ManagementGroup.EntityTypes.GetClasses(new ManagementPackClassCriteria("Name = 'System.GlobalSetting.ServiceRequestSettings'")).First();
                    propertyName = "ServiceRequestPrefix";
                }
                EnterpriseManagementObject settingInstance = p.Object.ManagementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(settingsClass.Id, ObjectQueryOptions.Default);
                prefix = settingInstance[null, propertyName].Value as String;
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "RetrievePrefix", ErrorCategory.InvalidOperation, p)) ;
            }
            return prefix;
        }
        protected override void ProcessRecord()
        {
            ManagementPackObjectTemplate t = SMHelpers.GetObjectTemplateFromRequestOffering(RequestOffering);
            EnterpriseManagementGroup EMG = RequestOffering.Object.ManagementGroup;
            EnterpriseManagementObjectProjection emop = new EnterpriseManagementObjectProjection(RequestOffering.Object.ManagementGroup, t);
            string prefix = GetIdPrefix(emop);
            emop.Object[null, "Id"].Value = String.Format("{0}{{0}}", prefix);
            emop.Object[null, "DisplayName"].Value = String.Format("{0} - {1}", emop.Object[null, "Id"].Value, RequestOffering.Object[null, "Title"].Value);
            workItemHasFileAttachment = RequestOffering.Object.ManagementGroup.EntityTypes.GetRelationshipClasses(new ManagementPackRelationshipCriteria("Name = 'System.WorkItemHasFileAttachment'")).First();
            workItemHasRequestOffering = RequestOffering.Object.ManagementGroup.EntityTypes.GetRelationshipClasses(new ManagementPackRelationshipCriteria("Name = 'System.WorkItemRelatesToRequestOffering'")).First();
            foreach (RequestOfferingQuestion q in Question)
            {
                if (q.SourceControlType != ControlType.FileAttachment)
                {
                    try
                    {
                        if (q.TargetPath != String.Empty)
                        {
                            WriteVerbose("Setting " + q.TargetPath + " to " + q.answer);
                            emop.Object[null, q.TargetPath].Value = q.answer;
                        }
                    }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "ValueAssignment", ErrorCategory.InvalidOperation, q));
                    }
                }
                else
                {
                    // this will be a PATH
                    // in the case where we do not commit, we must add the file stream to the emop so the user can close the
                    // stream
                    // in the case that we do commit, we can close the stream;
                    ProviderInfo pi;
                    string fileName = q.answer.ToString();
                    Collection<string> paths = GetResolvedProviderPathFromPSPath(fileName, out pi);
                    if (paths.Count == 1)
                    {
                        string filePath = paths[0];
                        WriteVerbose("Attaching " + filePath);
                        fileAttachement = File.OpenRead(filePath);
                        ManagementPackClass fileAttachmentClass = EMG.EntityTypes.GetClasses(new ManagementPackClassCriteria("Name = 'System.FileAttachment'")).First();
                        CreatableEnterpriseManagementObject fileAttachmentInstance = new CreatableEnterpriseManagementObject(EMG, fileAttachmentClass);

                        fileAttachmentInstance[fileAttachmentClass, "Id"].Value = Guid.NewGuid().ToString();
                        fileAttachmentInstance[fileAttachmentClass, "DisplayName"].Value = fileName;
                        fileAttachmentInstance[fileAttachmentClass, "Description"].Value = fileName;
                        fileAttachmentInstance[fileAttachmentClass, "Extension"].Value = Path.GetExtension(filePath);
                        fileAttachmentInstance[fileAttachmentClass, "Size"].Value = (int)fileAttachement.Length;
                        fileAttachmentInstance[fileAttachmentClass, "AddedDate"].Value = DateTime.Now.ToUniversalTime();
                        fileAttachmentInstance[fileAttachmentClass, "Content"].Value = fileAttachement;

                        emop.Add(fileAttachmentInstance, workItemHasFileAttachment.Target);
                    }
                    else
                    {
                        WriteWarning(q.answer.ToString() + "does not resolve to a single path, not attaching file");
                    }
                }
            }
            try
            {
                string userInputString = GetUserInputString();
                WriteVerbose("Setting UserInput to " + userInputString);

                emop.Object[null, "UserInput"].Value = userInputString;
            }
            catch ( Exception e)
            {
                WriteError(new ErrorRecord(e, "UserInputAssigmentFailure", ErrorCategory.InvalidOperation, emop));
            }
            try
            {
                emop.Add(RequestOffering, workItemHasRequestOffering.Target);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "RelationshipToRequestOffering", ErrorCategory.InvalidOperation, emop));
            }
            if (NoCommit)
            {
                WriteObject(emop);
                return;
            }
            else
            {
                try
                {
                    if (ShouldProcess(emop.Object.DisplayName))
                    {
                        WriteVerbose("Committing " + emop.Object.DisplayName);
                        emop.Commit();
                    }
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, "Commit Failure", ErrorCategory.InvalidOperation, emop));
                }
                finally
                {
                    if ( fileAttachement != null ) { fileAttachement.Close(); fileAttachement.Dispose(); }
                }
            }

        }
        private string GetUserInputString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<UserInputs>");
            foreach (RequestOfferingQuestion q in Question)
            {
                sb.AppendFormat(@" <UserInput Question=""{0}"" Answer=""{1}"" Type=""{2}"" />", q.Prompt, q.answer, q.stringMapper[q.SourceControlType].OutputType);
            }
            sb.Append("</UserInputs>");
            return sb.ToString();
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMRequestOfferingQuestion")]
    public class GetSCSMRequestOfferingQuestionCommand : PSCmdlet
    {
        #region parameters
        private EnterpriseManagementObjectProjection[] _requestOffering;
        [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true)]
        public EnterpriseManagementObjectProjection[] RequestOffering
        {
            get { return _requestOffering; }
            set { _requestOffering = value; }
        }
        #endregion
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            foreach (EnterpriseManagementObjectProjection p in RequestOffering)
            {
                string PMT;
                try
                {
                    PMT = p.Object[null, "PresentationMappingTemplate"].Value.ToString();
                }
                catch ( Exception e )
                {
                    WriteError(new ErrorRecord(e, "NoPresentationMappingTemplate", ErrorCategory.ObjectNotFound, p));
                    break;
                }
                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(PMT);
                foreach (RequestOfferingQuestion e in GetROQuestions(xdoc))
                {
                    e.RequestOffering = p;
                    e.RequestOfferingName = p.Object[null, "Title"].Value.ToString();
                    // Last chance to find our listID
                    if (e.SourceControlType == ControlType.List && e.ListId == Guid.Empty)
                    {
                        try
                        {
                            e.ListId = GetListIdFromRO(p, e.TargetPath);
                        }
                        catch ( Exception ex )
                        {
                            WriteError(new ErrorRecord(ex, "badListId", ErrorCategory.InvalidOperation, xdoc));
                        }
                    }
                    WriteObject(e);
                }
            }
        }
        private Guid GetListIdFromRO(EnterpriseManagementObjectProjection p, string propertyName)
        {
            ManagementPackObjectTemplate t = SMHelpers.GetObjectTemplateFromRequestOffering(p); // p.Object.ManagementGroup.Templates.GetObjectTemplates(new ManagementPackObjectTemplateCriteria("Name = '" +  p.Object[null, "TargetTemplate"].Value.ToString().Split('|')[3] + "'")).First();
            EnterpriseManagementObjectProjection emop = new EnterpriseManagementObjectProjection(p.Object.ManagementGroup, t);
            return emop.Object[null, propertyName].Type.EnumType.Id;
        }
        private List<RequestOfferingQuestion> GetROQuestions(XmlDocument doc)
        {
            List<RequestOfferingQuestion> myQuestions = new List<RequestOfferingQuestion>();
            XPathNavigator nav = doc.CreateNavigator();
            XPathExpression exp = nav.Compile("/Object/Data/PresentationMappingTemplate/Sources/Source");
            exp.AddSort("@Ordinal", XmlSortOrder.Ascending, XmlCaseOrder.None, "", XmlDataType.Number);
            // XmlNodeList questions = pmtXML.SelectNodes("/Object/Data/PresentationMappingTemplate/Sources/Source") ;
            XPathNodeIterator questions = nav.Select(exp);
            string prompt = "";
            string target = "";
            string type = "";
            int ordinal = 0;
            bool optional = true;
            foreach (XPathNavigator q in questions)
            {
                XmlNode question = ((IHasXmlNode)q).GetNode();
                if (question.Attributes.GetNamedItem("Id").Value != Guid.Empty.ToString())
                {

                    try { prompt = question.Attributes.GetNamedItem("Prompt").Value.ToString(); }
                    catch { prompt = String.Empty; }
                    try { target = question.SelectSingleNode("Targets/Target").Attributes.GetNamedItem("Path").Value; }
                    catch { target = String.Empty; }
                    string controlType = question.Attributes.GetNamedItem("ControlType").Value;
                    try { type = question.SelectSingleNode("ControlConfiguration/AddressableOutputs/AddressableOutput").Attributes.GetNamedItem("OutputType").Value; }
                    catch { type = string.Empty; }
                    try { ordinal = int.Parse(question.Attributes.GetNamedItem("Ordinal").Value); }
                    catch { ordinal = 0; }
                    try { optional = bool.Parse(question.Attributes.GetNamedItem("Optional").Value); }
                    catch { optional = true; }
                    RequestOfferingQuestion roq = new RequestOfferingQuestion();
                    roq.Prompt = prompt;
                    roq.TargetPath = target;
                    roq.Optional = optional;
                    roq.Ordinal = ordinal;
                    // roq.SourceControlType 
                    foreach (var k in roq.stringMapper.Keys)
                    {
                        if (roq.stringMapper[k].ControlTypeString == controlType) { roq.SourceControlType = k; }
                    }
                    if (roq.SourceControlType == ControlType.List)
                    {
                        // Try this first, not all lists provide an id for the enum
                        try
                        {
                            roq.ListId = new Guid(question.SelectSingleNode("ControlConfiguration/AddressableOutputs/AddressableOutput").Attributes.GetNamedItem("OutputTypeMetadata").Value);
                        }
                        catch
                        {
                            ;
                        }
                    }
                    if (roq.SourceControlType == ControlType.InlineList)
                    {
                        int count = question.SelectNodes("ControlConfiguration/Configuration/Details/ListValue").Count;
                        List<string> listElements = new List<string>();
                        foreach(XmlNode n in question.SelectNodes("ControlConfiguration/Configuration/Details/ListValue"))
                        {
                            listElements.Add(n.Attributes.GetNamedItem("DisplayName").Value);
                        }
                        roq.InlineListElements = listElements.ToArray();
                    }
                    myQuestions.Add(roq);

                }
            }
            return myQuestions;
        }

    }
    public class RequestOfferingQuestion : ROMapElement
    {
        public object answer;
        public EnterpriseManagementObjectProjection RequestOffering;
        public string RequestOfferingName;
        public RequestOfferingQuestion() { ; }
    }
    [Cmdlet(VerbsCommon.New, "SCSMRequestOfferingQuestion")]
    public class NewSCSMRequestOfferingQuestionCommand : ObjectCmdletHelper, IDynamicParameters
    {
        #region parameters
        private string _prompt;
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Prompt
        {
            get { return _prompt; }
            set { _prompt = value; }
        }
        private string _targetPath;
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string TargetPath
        {
            get { return _targetPath; }
            set { _targetPath = value; }
        }
        private ControlType _type;
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public ControlType Type
        {
            get { return _type; }
            set { _type = value; }
        }
        private SwitchParameter _mandatory;
        [Parameter]
        public SwitchParameter Mandatory
        {
            get { return _mandatory; }
            set { _mandatory = value; }
        }
        private SendInlineListParameter inlineList;
        private SendEnumerationListParameter enumerationList;

        public object GetDynamicParameters()
        {
            if ( Type == ControlType.InlineList )
            {
                inlineList = new SendInlineListParameter();
                return inlineList;
            }
            if (Type == ControlType.List)
            {
                enumerationList = new SendEnumerationListParameter();
                return enumerationList;
            }
            return null;
        }
        #endregion
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            ROMapElement question = new ROMapElement();
            question.Prompt = Prompt;
            question.TargetPath = TargetPath;
            question.SourceControlType = Type;
            if ( Mandatory )
            {
                question.Optional = false;
            }
            switch (Type)
            {
                case ControlType.List: 
                { 
                    question.ListId = enumerationList.EnumerationList.Id;
                    break;
                }
                case ControlType.InlineList: 
                {
                    question.InlineListElements = inlineList.ListElements;
                    break; 
                }
                default: { break; }
            }
            WriteObject(question);
        }
    }
    public class SendInlineListParameter
    {
        private string[] _listElements;
        [Parameter(Mandatory = true, Position = 3, ValueFromPipelineByPropertyName = true)]
        public string[] ListElements
        {
            get { return _listElements; }
            set { _listElements = value; }
        }
    }
    public class SendEnumerationListParameter
    {
        private ManagementPackEnumeration _enumerationList;
        [Parameter(Mandatory = true, Position = 3, ValueFromPipelineByPropertyName = true)]
        public ManagementPackEnumeration EnumerationList
        {
            get { return _enumerationList; }
            set { _enumerationList = value; }
        }
    }
    // TODO: There may be something wrong with this, it shows up appropriately in the tool, but not in
    // in the website
    [Cmdlet(VerbsCommon.New, "SCSMServiceOffering", SupportsShouldProcess = true)]
    public class NewSCSMServiceOfferingCommand : ObjectCmdletHelper
    {
        #region parameters
        private String _briefDescription;
        [Parameter]
        public String BriefDescription
        {
            get { return _briefDescription; }
            set { _briefDescription = value; }
        }
        private String _category;
        [Parameter]
        public String Category
        {
            get { return _category; }
            set { _category = value; }
        }
        private String _comment;
        [Parameter]
        public String Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }
        private String _costInformation;
        [Parameter]
        public String CostInformation
        {
            get { return _costInformation; }
            set { _costInformation = value; }
        }
        private Uri _costInformationLink;
        [Parameter]
        public Uri CostInformationLink
        {
            get { return _costInformationLink; }
            set { _costInformationLink = value; }
        }
        private String _displayName;
        [Parameter]
        public String DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        private Stream _image;
        [Parameter]
        public Stream Image
        {
            get { return _image; }
            set { _image = value; }
        }
        private String _notes;
        [Parameter]
        public String Notes
        {
            get { return _notes; }
            set { _notes = value; }
        }
        private String _overview;
        [Parameter]
        public String Overview
        {
            get { return _overview; }
            set { _overview = value; }
        }
        private DateTime ? _publishDate;
        [Parameter]
        public DateTime ? PublishDate
        {
            get { return _publishDate; }
            set { _publishDate = value; }
        }
        private String _sLAInformation;
        [Parameter]
        public String SLAInformation
        {
            get { return _sLAInformation; }
            set { _sLAInformation = value; }
        }
        private Uri _sLAInformationLink;
        [Parameter]
        public Uri SLAInformationLink
        {
            get { return _sLAInformationLink; }
            set { _sLAInformationLink = value; }
        }
        private string _status;
        [Parameter]
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }
        private String _title;
        [Parameter(Mandatory = true)]
        public String Title
        {
            get { return _title; }
            set { _title = value; }
        }
        private EnterpriseManagementObject _owner;
        [Parameter]
        public EnterpriseManagementObject Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }
        private EnterpriseManagementObject _publishedBy;
        [Parameter]
        public EnterpriseManagementObject PublishedBy
        {
            get { return _publishedBy; }
            set { _publishedBy = value; }
        }

        private EnterpriseManagementObject[] _requestOffering;
        [Parameter]
        public EnterpriseManagementObject[]  RequestOffering
        {
            get { return _requestOffering; }
            set { _requestOffering = value; }
        }
        private EnterpriseManagementObject[] _relatedKnowledgeArticle;
        [Parameter]
        public EnterpriseManagementObject[] RelatedKnowledgeArticle
        {
            get { return _relatedKnowledgeArticle; }
            set { _relatedKnowledgeArticle = value; }
        }
        private EnterpriseManagementObject[] _relatedService;
        [Parameter]
        public EnterpriseManagementObject[] RelatedService
        {
            get { return _relatedService; }
            set { _relatedService = value; }
        }
        private ManagementPack _managementPack;
        [Parameter(Mandatory = true, Position = 0)]
        public ManagementPack ManagementPack
        {
            get { return _managementPack; }
            set { _managementPack = value; }
        }
        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        #endregion

        private string[] classMembers = { "BriefDescription", "Category", "Comment", "CostInformation", "CostInformationLink", 
                                            "DisplayName", "Image", "Notes", "Overview", "PublishDate", "SLAInformation", 
                                            "SLAInformationLink", "Status", "Title" };
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            ManagementPack catalogMP = SMHelpers.GetManagementPack("System.ServiceCatalog.Library", _mg);
            ManagementPackClass SOClass = SMHelpers.GetManagementPackClass("System.ServiceOffering", catalogMP);
            ManagementPack projectionMP = SMHelpers.GetManagementPack("ServiceManager.ServiceCatalog.Library", _mg);
            ManagementPackTypeProjection SOProjectionType = SMHelpers.GetManagementPackTypeProjection("System.ServiceOffering.ProjectionType",projectionMP);
            ManagementPackTypeProjection projectionType = SMHelpers.GetManagementPackTypeProjection("System.Extensibility.ManagementPackServiceOffering.Projection", SMHelpers.GetManagementPack("System.ServiceCatalog.Library", _mg));
            EnterpriseManagementObjectProjection projection = new EnterpriseManagementObjectProjection(_mg, SOClass);
            CreatableEnterpriseManagementObject cemo = new CreatableEnterpriseManagementObject(_mg, SOClass);
            ExtensionIdentifier ServiceOfferingTypeId = ExtensionIdentifier.CreateTypeIdentifier("ServiceOffering");
            string ServiceOfferingName = String.Format("Offering.{0}", Guid.NewGuid().ToString("N"));
            string ServiceOfferingID = ExtensionIdentifier.CreateObjectIdentifier(ManagementPack, ServiceOfferingName, ServiceOfferingTypeId).ToString();
            projection.Object[SOClass, "ID"].Value = ServiceOfferingID;
            projection.Object[SOClass, "Domain"].Value = ManagementPack.Name;
            projection.Object[null, "CultureName"].Value = String.Empty;
            #region SetSeedProperties
            Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (ManagementPackProperty prop in cemo.GetProperties())
            {
                ht.Add(prop.Name, prop);
            }
            Type t = this.GetType();
            foreach(string s in classMembers)
            {
                PropertyInfo mi = t.GetMember(s).GetValue(0) as PropertyInfo;
                object o = mi.GetValue(this, null);
                if (o == null)
                {
                    WriteVerbose(s + " is not set");
                }
                else
                {
                    WriteVerbose(s + " is set to " + o.ToString());
                    ManagementPackProperty p = ht[s] as ManagementPackProperty;
                    AssignNewValue(p, projection.Object[p], o);
                }
            }
            #endregion
            #region SetRelationships
            if (RelatedKnowledgeArticle != null)
            {
                ITypeProjectionComponent rel = SOProjectionType["RelatedKnowledgeArticles"];
                foreach (EnterpriseManagementObject emo in RelatedKnowledgeArticle)
                {
                    projection.Add(emo, rel.TargetEndpoint);
                }
            }
            if (RelatedService != null)
            {
                ITypeProjectionComponent rel = SOProjectionType["RelatedServices"];
                foreach (EnterpriseManagementObject emo in RelatedKnowledgeArticle)
                {
                    projection.Add(emo, rel.TargetEndpoint);
                }
            }
            if (RequestOffering != null)
            {
                ITypeProjectionComponent rel = SOProjectionType["RequestOfferings"];
                foreach (EnterpriseManagementObject emo in RequestOffering)
                {
                    projection.Add(emo, rel.TargetEndpoint);
                }
            }
            if (Owner != null)
            {
                ITypeProjectionComponent rel = SOProjectionType["Owner"];
                projection.Add(Owner, rel.TargetEndpoint);
            }
            if (PublishedBy != null)
            {
                ITypeProjectionComponent rel = SOProjectionType["PublishedBy"];
                projection.Add(PublishedBy, rel.TargetEndpoint);
            }
            #endregion
            if (ShouldProcess(projection.Object.DisplayName))
            {
                projection.Commit();
                if (PassThru)
                {
                    WriteObject(ServiceManagerObjectHelper.AdaptProjection(this, projection, SOProjectionType.Name));
                }
            }
        }
    }
     
    // [Cmdlet(VerbsCommon.Set, "SCSMServiceOffering", SupportsShouldProcess = true)]
    // [Cmdlet(VerbsCommon.Set, "SCSMRequestOffering", SupportsShouldProcess = true)]
    
    public enum ControlType
    {
        String,
        DateTime,
        Double,
        FileAttachment,
        Integer,
        List,
        InlineList,
        Boolean
    };

    public class stringMap
    {
        public string OutputType;
        public string OutputName;
        public string ControlTypeString;
        public stringMap(string t, string n, string c)
        {
            OutputType = t;
            OutputName = n;
            ControlTypeString = c;
        }
    }

    public class ROMap
    {
        public List<ROMapElement> Elements;
        public ManagementPack myMP;
        public int currentQuestion = 1;
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(@"<Object ID=""1|{0}|1.0.0.0|Offering{1}/GeneratedId_PresentationMappingTemplate0|4|RequestOffering/PresentationMappingTemplate"">", myMP.Name, Guid.NewGuid().ToString("N"));
            sb.Append("<References>");
            sb.AppendFormat(@"<Reference Alias=""EnterpriseManagement"" Name=""Microsoft.EnterpriseManagement.ServiceManager.UI.Console"" KeyToken=""31bf3856ad364e35"" Version=""7.5.1088.205"" />");
            sb.AppendFormat(@"<Reference Alias=""Alias_1"" Name=""ServiceManager.ServiceRequest.Library"" KeyToken=""31bf3856ad364e35"" Version=""7.5.1088.205"" />");
            sb.Append("</References>");
            sb.Append("<Data>");
            sb.Append("<PresentationMappingTemplate>");
            sb.Append("<Sources>");
            sb.Append(@"<Source Id=""00000000-0000-0000-0000-000000000000"" Ordinal=""0"" ReadOnly=""false"" Optional=""false"" ControlType=""System.SupportingItem.PortalControl"" >");
            sb.Append(@"<ControlConfiguration>");
            sb.Append(@"<Dependencies />");
            sb.Append(@"<AddressableOutputs>");
            sb.Append(@"<AddressableOutput OutputName=""Token: Portal User Name"" OutputType=""string"" />");
            sb.Append(@"</AddressableOutputs>");
            sb.Append(@"</ControlConfiguration>");
            sb.Append(@"<Targets />");
            sb.Append(@"</Source>");
            foreach (ROMapElement e in Elements)
            {
                sb.Append(e.ToString());
            }
            sb.Append("</Sources>");
            sb.Append("</PresentationMappingTemplate>");
            sb.Append("</Data>");
            sb.Append("</Object>");
            return sb.ToString();
        }
        public string ToIndentedString()
        {
            StringBuilder sb = new StringBuilder();
            StringWriter stream = new StringWriter(sb);
            XmlTextWriter xmlWriter = new XmlTextWriter(stream);
            xmlWriter.Formatting = Formatting.Indented;
            XmlDocument xmlblob = new XmlDocument();
            string s = this.ToString();
            xmlblob.LoadXml(s);
            xmlblob.WriteTo(xmlWriter);
            return sb.ToString();
        }
        private ROMap()
        {
            Elements = new List<ROMapElement>();
        }
        public ROMap(ManagementPack m)
        {
            Elements = new List<ROMapElement>();
            myMP = m;
        }

        public void AddQuestion(string prompt, string targetPath, ControlType pathType)
        {
            // handle ControlTypes which we do in other places
            if (pathType == ControlType.List) { throw new ArgumentException("Use AddListQuestion for adding an enum list"); }
            if (pathType == ControlType.InlineList) { throw new ArgumentException("Use AddInlineListQuestion for adding a simple list"); }
            ROMapElement element = new ROMapElement();
            element.Ordinal = currentQuestion++;
            element.Prompt = prompt;
            element.TargetPath = targetPath;
            element.SourceControlType = pathType;
            // TODO: VALIDATION
            this.Elements.Add(element);
        }
        // this is for a simple list
        public void AddInlineListQuestion(string prompt, string targetPath, string[] listElements)
        {
            ROMapElement element = new ROMapElement();
            element.Ordinal = currentQuestion++;
            element.Prompt = prompt;
            element.TargetPath = targetPath;
            element.SourceControlType = ControlType.InlineList;
            element.InlineListElements = listElements;
            this.Elements.Add(element);
        }
        // add an enum type
        public void AddListQuestion(string prompt, string targetPath, ManagementPackEnumeration enumList)
        {
            ROMapElement element = new ROMapElement();
            element.Ordinal = currentQuestion++;
            element.Prompt = prompt;
            element.TargetPath = targetPath;
            element.SourceControlType = ControlType.List;
            element.ListId = enumList.Id;
            this.Elements.Add(element);
        }
        // This will overwrite the ordinal if it's passed
        public void AddQuestion(ROMapElement e)
        {
            e.Ordinal = currentQuestion++;
            this.Elements.Add(e);
        }
        public List<ValidationResult> Validate(EnterpriseManagementObjectProjection p)
        {
            List<ValidationResult> myResults = new List<ValidationResult>();
            foreach (ROMapElement element in this.Elements)
            {
                ValidationResult mystatus = new ValidationResult();
                mystatus.PropertyName = element.TargetPath;
                mystatus.PropertyType = element.GetControlTypeString(element.SourceControlType);
                mystatus.Status = MappingValidationStatus.ERROR;
                foreach (ManagementPackProperty prop in p.Object.GetProperties())
                {
                    if (string.Compare(element.TargetPath, prop.Name) == 0)
                    {
                        mystatus.Status = MappingValidationStatus.OK;
                        mystatus.Message = "Map ok: " + prop.Name;
                        break;
                    }
                }
                if (mystatus.Status != MappingValidationStatus.OK)
                {
                    mystatus.Message = String.Format("Map fail: no property '{0}' in {1}", element.TargetPath, p.Object.Name);
                }
                myResults.Add(mystatus);
            }
            return myResults;
        }
    }

    public class ROMapElement
    {
        public Guid Id;
        public int Ordinal;
        public string Prompt;
        public bool ReadOnly; // default is false
        public bool Optional = true;
        public ControlType SourceControlType = ControlType.String;
        public Guid ListId; // note that this will only be available when the ControlType is List this is the id of the parent enum
        public string[] InlineListElements;
        public int DefaultListElement;
        public string TargetPath;
        public Dictionary<ControlType, stringMap> stringMapper;
        public ROMapElement()
        {
            Id = Guid.NewGuid();
            stringMapper = new Dictionary<ControlType, stringMap>();
            stringMapper.Add(ControlType.Boolean, new stringMap("bool", "True/False", "System.SupportingItem.PortalControl.Boolean"));
            stringMapper.Add(ControlType.DateTime, new stringMap("datetime", "Date", "System.SupportingItem.PortalControl.DateTime"));
            stringMapper.Add(ControlType.Double, new stringMap("double", "Decimal", "System.SupportingItem.PortalControl.Double"));
            stringMapper.Add(ControlType.FileAttachment, new stringMap(String.Empty, String.Empty, "System.SupportingItem.PortalControl.FileAttachment"));
            stringMapper.Add(ControlType.InlineList, new stringMap("string", "ListValue", "System.SupportingItem.PortalControl.InlineList"));
            stringMapper.Add(ControlType.Integer, new stringMap("int", "Integer", "System.SupportingItem.PortalControl.Integer"));
            stringMapper.Add(ControlType.List, new stringMap("list", "List Item", "System.SupportingItem.PortalControl.List"));
            stringMapper.Add(ControlType.String, new stringMap("string", "String", "System.SupportingItem.PortalControl.String"));
        }
        public string GetControlTypeString(ControlType t)
        {
            return stringMapper[t].ControlTypeString;
        }
        public override string ToString()
        {
            if (SourceControlType == ControlType.InlineList && InlineListElements == null) { throw new InvalidOperationException("List types must be populated"); }
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(@"<Source Id=""{0}"" Ordinal=""{1}"" Prompt=""{2}"" ReadOnly=""{3}"" Optional=""{4}"" ControlType=""{5}"">",
                Id, Ordinal, Prompt, ReadOnly.ToString().ToLower(), Optional.ToString().ToLower(), GetControlTypeString(SourceControlType));
            sb.Append("<ControlConfiguration>");
            sb.Append("<Dependencies />");


            if (SourceControlType == ControlType.InlineList)
            {
                sb.Append("<AddressableOutputs>");
                sb.Append(@"<AddressableOutput OutputName=""ListValue"" OutputType=""string"" />");
                sb.Append("</AddressableOutputs>");
                sb.Append("<Configuration>");
                sb.Append("<Details>");
                int i = 0;
                foreach (string s in InlineListElements)
                {
                    bool isDefault;
                    if (i == DefaultListElement) { isDefault = true; } else { isDefault = false; }
                    sb.AppendFormat(@"<ListValue DisplayName=""{0}"" IsDefault=""{1}"" />", s, isDefault.ToString().ToLower());
                    i++;
                }
                sb.Append("</Details>");
                sb.Append("</Configuration>");
            }
            else if (SourceControlType == ControlType.FileAttachment)
            {
                sb.Append("<AddressableOutputs />");
            }
            else if (SourceControlType == ControlType.List)
            {
                sb.Append("<AddressableOutputs>");
                sb.AppendFormat(@"<AddressableOutput OutputName=""{0}"" OutputType=""enum"" OutputTypeMetadata=""{1}"" />", stringMapper[SourceControlType].OutputName, ListId);
                sb.Append("</AddressableOutputs>");
            }
            else
            {
                sb.Append("<AddressableOutputs>");
                sb.AppendFormat(@"<AddressableOutput OutputName=""{0}"" OutputType=""{1}"" />", stringMapper[SourceControlType].OutputName, stringMapper[SourceControlType].OutputType);
                sb.Append("</AddressableOutputs>");
            }
            sb.Append("</ControlConfiguration>");
            if (TargetPath == null || SourceControlType == ControlType.FileAttachment)
            {
                sb.Append("<Targets />");
            }
            else
            {
                sb.Append("<Targets>");
                sb.AppendFormat(@"<Target Path=""{0}"" OutputName=""{1}"" />", TargetPath, stringMapper[SourceControlType].OutputName);
                sb.Append("</Targets>");
            }
            sb.Append("</Source>");

            return sb.ToString();
        }
    }
    public enum MappingValidationStatus { OK, ERROR };
    public class ValidationResult
    {
        public string PropertyName;
        public string PropertyType;
        public MappingValidationStatus Status;
        public string Message;
    }

}
#endif
