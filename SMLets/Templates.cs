using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Xml;
using System.IO;
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
    #region GetSCSMObjectTemplate
    [Cmdlet(VerbsCommon.Get, "SCSMObjectTemplate", DefaultParameterSetName = "Name")]
    public class GetSCSMObjectTemplateCommand : EntityTypeHelper
    {
        private Guid[] _id = null;
        [Parameter(ParameterSetName = "Id")]
        public Guid[] Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private string[] _displayName = null;
        [Parameter(ParameterSetName = "DisplayName")]
        public string[] DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "Id")
            {
                foreach (Guid i in Id)
                {
                    try { WriteObject(_mg.Templates.GetObjectTemplate(i)); }
                    catch (ObjectNotFoundException e) { WriteError(new ErrorRecord(e, "ObjectTemplate not found", ErrorCategory.ObjectNotFound, Id)); }
                    catch (Exception e) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, i)); }
                }
            }

            else if (ParameterSetName == "DisplayName")
            {
                foreach (string n in DisplayName)
                {
                    Regex r = new Regex(n, RegexOptions.IgnoreCase);
                    foreach (ManagementPackObjectTemplate o in _mg.Templates.GetObjectTemplates())
                    {
                        if (r.Match(o.DisplayName).Success)
                        {
                            WriteObject(o);
                        }
                    }
                }
            }
            else
            {
                if (Name == null)
                {
                    foreach (ManagementPackObjectTemplate ot in _mg.Templates.GetObjectTemplates())
                    {
                        WriteObject(ot);
                    }
                }
                else
                {
                    Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                    foreach (ManagementPackObjectTemplate ot in _mg.Templates.GetObjectTemplates())
                    {
                        if (r.Match(ot.Name).Success) { WriteObject(ot); }
                    }
                }
            }

        }
    }
    #endregion

    #region NewSCSMObjectTemplate
    [Cmdlet(VerbsCommon.New, "SCSMObjectTemplate", SupportsShouldProcess = true,DefaultParameterSetName="Projection")]
    public class NewSCSMObjectTemplateCommand : SMCmdletBase
    {
        private string _displayName;
        [Parameter(Position=0, Mandatory=true, ValueFromPipeline = true)]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        private ManagementPack _managementPack;
        [Parameter(Position = 1, Mandatory = true)]
        public ManagementPack ManagementPack
        {
            get { return _managementPack; }
            set
            {
                if (value.Sealed)
                {
                    throw (new ArgumentException("ManagementPack must not be sealed"));
                }
                else
                {
                    _managementPack = value;
                }
            }
        }
        private ManagementPackClass _class;
        [Parameter(Position = 2, Mandatory = true, ParameterSetName = "Class")]
        public ManagementPackClass Class
        {
            get { return _class; }
            set { _class = value; }
        }
        private ManagementPackTypeProjection _projection;
        [Parameter(Position = 2, Mandatory = true, ParameterSetName = "Projection")]
        public ManagementPackTypeProjection Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }
        /*
         * Commented out for now, just create a blank template which can then be modifed at a later date via the
         * UI or cmdlet
         * private Hashtable _templateData;
         * [Parameter(Position = 3)]
         * public Hashtable TemplateData
         * {
         *   get { return _templateData; }
         *   set { _templateData = value; }
         * }
         */
        private string _name;
        [Parameter]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private string _description;
        [Parameter]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            if (Name == null)
            {
                Name = String.Format("Template.{0:N}", Guid.NewGuid());
            }
            ManagementPackObjectTemplate template = new ManagementPackObjectTemplate(this.ManagementPack, Name);
            template.DisplayName = DisplayName;
            if (Description != null) { template.Description = Description; }
            if (Class != null)
            {
                template.TypeID = Class;
            }
            else
            {
                template.TypeID = Projection;
            }
            if (ShouldProcess(DisplayName))
            {
                this.ManagementPack.AcceptChanges();
            }
            WriteObject(template);
        }
    }
    #endregion

    #region SetSCSMObjectTemplate
    [Cmdlet(VerbsCommon.Set, "SCSMObjectTemplate", SupportsShouldProcess = true)]
    public class SetSCSMObjectTemplateCommand : SMCmdletBase
    {
        private EnterpriseManagementObjectProjection[] _projection = null;
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "P", ValueFromPipeline = true)]
        public EnterpriseManagementObjectProjection[] Projection
        {
            get { return _projection; }
            set { _projection = value; }
        }

        private EnterpriseManagementObject[] _object = null;
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "O", ValueFromPipeline = true)]
        public EnterpriseManagementObject[] Object
        {
            get { return _object; }
            set { _object = value; }
        }

        private string _name = null;
        // [Parameter(Position=1,Mandatory=true)]
        // [Parameter(ParameterSetName="Projection")]
        // [Parameter(ParameterSetName="Object")]
        [Parameter]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private ManagementPackObjectTemplate _template = null;
        // [Parameter(Position=1,Mandatory=true)]
        // [Parameter(ParameterSetName="Projection")]
        // [Parameter(ParameterSetName="Object")]
        [Parameter]
        public ManagementPackObjectTemplate Template
        {
            get { return _template; }
            set { _template = value; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Template == null && Name != null)
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach (ManagementPackObjectTemplate ot in _mg.Templates.GetObjectTemplates())
                {
                    if (r.Match(ot.Name).Success)
                    {
                        Template = ot;
                        return;
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            try
            {
                if (Object != null)
                {
                    foreach (EnterpriseManagementObject o in Object)
                    {
                        if (ShouldProcess(o[null, "Id"].Value.ToString()))
                        {
                            o.ApplyTemplate(Template);
                            o.Overwrite();
                        }
                    }
                }
                else if (Projection != null)
                {
                    foreach (EnterpriseManagementObjectProjection p in Projection)
                    {
                        if (ShouldProcess(p.Object[null, "Id"].Value.ToString()))
                        {
                            p.ApplyTemplate(Template);
                            p.Overwrite();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "ApplyTemplate", ErrorCategory.InvalidOperation, Template));

            }
        }
    }
    #endregion

}
