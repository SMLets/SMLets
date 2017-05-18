using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using System.Text.RegularExpressions;

namespace SMLets
{
    public class DWHelper : SMCmdletBase
    {
        // Parameters
        private string _name = ".*";
        [Parameter(Position=0,ValueFromPipeline=true)]
        public string Name
        {
            get {return _name; }
            set { _name = value; }
        }
        private Guid _id = Guid.Empty;
        [Parameter]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
    }

    [Cmdlet(VerbsCommon.Get, "DataWarehouseConfiguration")]
    public class GetDataWarehouseConfigurationCommand : SMCmdletBase
    {
        protected override void EndProcessing()
        {
            WriteObject(_mg.DataWarehouse.GetDataWarehouseConfiguration());
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWDimensionTypes")]
    public class GetSCDWDimensionTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetDimensionType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "DimensionType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackDimensionType o in _mg.DataWarehouse.GetDimensionTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWFactTypes")]
    public class GetSCDWFactTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetFactType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "FactType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackFactType o in _mg.DataWarehouse.GetFactTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWMeasureTypes")]
    public class GetSCDWMeasureTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetMeasureType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "MeasureType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackMeasureType o in _mg.DataWarehouse.GetMeasureTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWOutriggerTypes")]
    public class GetSCDWOutriggerTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetOutriggerType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "OutriggerType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackOutriggerType o in _mg.DataWarehouse.GetOutriggerTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWRelationshipFactTypes")]
    public class GetSCDWRelationshipFactTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetRelationshipFactType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "RelationshipFactType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackRelationshipFactType o in _mg.DataWarehouse.GetRelationshipFactTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCDWWarehouseModuleTypes")]
    public class GetSCDWWareHoueModuleTypesCommand : DWHelper
    {
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.DataWarehouse.GetWarehouseModuleType(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "WarehouseModuleType not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackWarehouseModuleType o in _mg.DataWarehouse.GetWarehouseModuleTypes())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }
}
