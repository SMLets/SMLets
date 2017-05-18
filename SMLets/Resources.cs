using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System.Text.RegularExpressions;

namespace SMLets
{
    [Cmdlet(VerbsCommon.Get, "SCSMResource", DefaultParameterSetName = "MP")]
    public class GetSMResourceCommand : ObjectCmdletHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter(ParameterSetName = "Guid", Position = 0)]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private ManagementPack _managementPack;
        [Parameter(ParameterSetName = "MP")]
        public ManagementPack ManagementPack
        {
            get { return _managementPack; }
            set { _managementPack = value; }
        }
        private string _name;
        [Parameter(ParameterSetName = "MP", ValueFromPipeline = true)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _tag = ".*";
        [Parameter]
        public string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }
        private SwitchParameter _data;
        [Parameter]
        public SwitchParameter Data
        {
            get { return _data; }
            set { _data = value; }
        }

        private void HandleOutput(ManagementPackResource res)
        {
            Regex myTag = new Regex(Tag, RegexOptions.IgnoreCase);
            // If we don't match our tag, just return
            if (!myTag.Match(res.XmlTag).Success)
            {
                return;
            }
            PSObject CompositeResource = new PSObject(res);

            ManagementPackImage i = res as ManagementPackImage;
            bool isImage = false;
            if (i != null) { isImage = true; WriteVerbose("resource is an image!"); }

            ManagementPackReportResource rep = res as ManagementPackReportResource;
            bool isReport = false;
            if (rep != null) { isReport = true; WriteVerbose("resource is an report!"); }

            if (this.Data || isImage || isReport)
            {
                // Just add something if it might be there
                if (!res.HasNullStream)
                {
                    try
                    {
                        Stream s = _mg.Resources.GetResourceData(res);
                        int l = (int)s.Length;
                        byte[] data = new byte[l];
                        s.Read(data, 0, l);
                        s.Close();
                        s.Dispose();
                        CompositeResource.Members.Add(new PSNoteProperty("StreamData", data));
                        CompositeResource.Members.Add(new PSScriptMethod("ConvertToString", ScriptBlock.Create("[char[]]($this.StreamData) -join ''")));
                        CompositeResource.Members.Add(new PSScriptMethod("Save", ScriptBlock.Create(@"
                    $fs = new-object io.filestream (""$PWD/"" + $this.FileName),OpenOrCreate
                    $result = $fs.write($this.StreamData,0,$this.StreamData.length)
                    $fs.close()
                    $fs.dispose()
                    ""File saved as: "" + $this.Filename
                    ")));
                    }
                    catch
                    {
                        this.WriteWarning("Resource stream " + res.Name + " is unexpectedly null");
                    }
                }

            }
            CompositeResource.Members.Add(new PSNoteProperty("__TYPE", res.GetType().Name));
            this.WriteObject(CompositeResource);
        }
        protected override void ProcessRecord()
        {
            base.BeginProcessing();
            if (Id != Guid.Empty)
            {
                try
                {
                    ManagementPackResource res = _mg.Resources.GetResource<ManagementPackResource>(Id);
                    HandleOutput(res);
                }
                catch (Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e, "Resource Failure", ErrorCategory.InvalidOperation, Id));
                }
            }
            else if (ManagementPack != null)
            {
                if (Name != null)
                {
                    try
                    {
                        ManagementPackResource res = ManagementPack.GetResource<ManagementPackResource>(Name);
                        HandleOutput(res);
                    }
                    catch (Exception e)
                    {
                        ThrowTerminatingError(new ErrorRecord(e, "Resource Failure", ErrorCategory.InvalidOperation, Name));
                    }
                }
                else
                {
                    foreach (ManagementPackResource r in ManagementPack.GetResources<ManagementPackResource>())
                    {
                        HandleOutput(r);
                    }
                }
            }
            else // Get all the resources!!!
            {
                foreach (ManagementPackResource r in _mg.Resources.GetResources<ManagementPackResource>())
                {
                    HandleOutput(r);
                }
            }
        }
    }

}
