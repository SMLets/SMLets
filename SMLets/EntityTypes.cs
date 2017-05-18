using System;
using System.IO;
using System.Xml;
using System.Text;
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
    public class EntityTypeHelper : SMCmdletBase
    {
        // Parameters
        private string _name = ".*";
        [Parameter(Position=0,ValueFromPipeline=true)]
        public string Name
        {
            get {return _name; }
            set { _name = value; }
        }
    }

    public abstract class GetGroupQueueCommand : ObjectCmdletHelper
    {
        // This needs to be overridden in the Get-Group/Queue cmdlet to
        // get the appropriate objects
        public abstract string neededClassName
        {
            get;
        }

        private string[] _displayname = { "*" };
        [Parameter(Position = 0, ParameterSetName = "DISPLAYNAME")]
        public string[] DisplayName
        {
            get { return _displayname; }
            set { _displayname = value; }
        }
        private string[] _name;
        [Parameter(ParameterSetName = "NAME", Position = 0)]
        public string[] Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private Guid[] _id;
        [Parameter(Position = 0, ParameterSetName = "ID")]
        public Guid[] Id
        {
            get { return _id; }
            set { _id = value; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        protected override void EndProcessing()
        {
            // Short circuit the entire process if you got a collection of IDs
            if (ParameterSetName == "ID")
            {
                foreach (Guid g in Id)
                {
                    WriteObject(new EnterpriseManagementGroupObject(_mg.EntityObjects.GetObject<EnterpriseManagementObject>(g, ObjectQueryOptions.Default)));
                }
                return;
            }
            // OK - we're in the Name/DisplayName parametersets, so figure out what base class to get
            ManagementPackClass cig = null;
            foreach (ManagementPackClass c in _mg.EntityTypes.GetClasses())
            {
                // Microsoft.SystemCenter.ConfigItemGroup or System.WorkItemGroup
                if (String.Compare(c.Name, neededClassName, true) == 0)
                {
                    cig = c;
                }
            }
            foreach (EnterpriseManagementObject emo in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(cig, ObjectQueryOptions.Default))
            {
                switch (ParameterSetName)
                {
                    case "DISPLAYNAME":
                        foreach (string s in DisplayName)
                        {
                            WildcardPattern wc = new WildcardPattern(s, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
                            if (wc.IsMatch(emo.DisplayName))
                            {
                                WriteObject(new EnterpriseManagementGroupObject(emo));
                            }
                        }
                        break;
                    case "NAME":
                        foreach (string s in Name)
                        {
                            WildcardPattern wc = new WildcardPattern(s, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
                            // We need to match against the ClassName
                            if (wc.IsMatch(emo.GetLeastDerivedNonAbstractClass().Name))
                            {
                                WriteObject(new EnterpriseManagementGroupObject(emo));
                            }
                        }
                        break;
                    default:
                        ThrowTerminatingError(new ErrorRecord(new InvalidOperationException("Bad switch"), "GroupOutput", ErrorCategory.InvalidOperation, this));
                        break;
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCSMClass")]
    public class GetSMClassCommand : EntityTypeHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try
                {
                    WriteObject(_mg.EntityTypes.GetClass(Id));
                }
                catch ( ObjectNotFoundException e )
                {
                    WriteError(new ErrorRecord(e, "Class not found", ErrorCategory.ObjectNotFound, Id));
                }
                catch ( Exception e )
                {
                    WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id));
                }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackClass o in _mg.EntityTypes.GetClasses())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCSMRelationshipClass")]
    public class GetSMRelationshipClassCommand : EntityTypeHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.EntityTypes.GetRelationshipClass(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "Relationship not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackRelationship o in _mg.EntityTypes.GetRelationshipClasses())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Get,"SCSMTypeProjection")]
    public class GetSMTypeProjectionCommand : EntityTypeHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        private SwitchParameter _noAdapt;
        [Parameter]
        public SwitchParameter NoAdapt
        {
            get { return _noAdapt; }
            set { _noAdapt = value; }
        }
            
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                try { WriteObject(_mg.EntityTypes.GetTypeProjection(Id)); }
                catch ( ObjectNotFoundException e ) { WriteError(new ErrorRecord(e, "TypeProjection not found", ErrorCategory.ObjectNotFound, Id)); }
                catch ( Exception e ) { WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id)); }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPackTypeProjection o in _mg.EntityTypes.GetTypeProjections())
                {
                    if ( r.Match(o.Name).Success )
                    {
                        if (NoAdapt)
                        {
                            WriteObject(o);
                        }
                        else
                        {
                            PSObject AP = AdaptProjection(o);
                            WriteObject(AP);
                        }
                    }
                }
            }
        }
        public PSObject AdaptProjection(ManagementPackTypeProjection projection)
        {
            PSObject o = new PSObject();
            o.Members.Add(new PSNoteProperty("__base",projection));
            o.Members.Add(new PSScriptMethod("GetAsXml",ScriptBlock.Create("[xml]($this.__base.CreateNavigator().OuterXml)")));
            o.TypeNames.Insert(0, projection.GetType().FullName);
            o.TypeNames.Insert(0, projection.Name);
            Type T = projection.GetType();
            foreach(PropertyInfo pi in T.GetProperties())
            {
                // no need to catch - just get what you can
                // I've seen problems with Item
                try
                {
                    o.Members.Add(new PSNoteProperty(pi.Name, T.InvokeMember(pi.Name, BindingFlags.GetProperty, null, projection, null, CultureInfo.CurrentCulture)));
                }
                catch
                {
                    ;
                }
            }
            return o;
        }

    }

    #region SCSMEnumeration cmdlets

    [Cmdlet(VerbsCommon.Get, "SCSMChildEnumeration")]
    public class GetSMChildEnumerationCommand : EntityTypeHelper
    {
        private TraversalDepth _depth = TraversalDepth.Recursive;
        [Parameter]
        public TraversalDepth Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        private ManagementPackEnumeration _enumeration;
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ManagementPackEnumeration Enumeration
        {
            get { return _enumeration; }
            set { _enumeration = value; }
        }

        protected override void ProcessRecord()
        {
            foreach (ManagementPackEnumeration o in _mg.EntityTypes.GetChildEnumerations(Enumeration.Id, Depth))
            {
                WriteObject(o);
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMEnumeration")]
    public class GetSMEnumerationCommand : EntityTypeHelper
    {
        private Guid _id = Guid.Empty;
        [Parameter]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }
        protected override void ProcessRecord()
        {
            if (Id != Guid.Empty)
            {
                try
                {
                    WriteObject(_mg.EntityTypes.GetEnumeration(Id));
                }
                catch (ObjectNotFoundException e)
                {
                    WriteError(new ErrorRecord(e, "Class not found", ErrorCategory.ObjectNotFound, Id));
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, "Unknown error", ErrorCategory.NotSpecified, Id));
                }
            }
            else
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach (ManagementPackEnumeration o in _mg.EntityTypes.GetEnumerations())
                {
                    if (r.Match(o.Name).Success)
                    {
                        WriteObject(o);
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Remove, "SCSMEnumeration", SupportsShouldProcess = true)]
    public class RemoveSMEnumerationCommand : EntityTypeHelper
    {
        private ManagementPackEnumeration _enumeration;

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ManagementPackEnumeration Enumeration
        {
            get { return _enumeration; }
            set { _enumeration = value; }

        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            ManagementPackEnumeration enumeration = _mg.EntityTypes.GetEnumeration(_enumeration.Id);
            ManagementPack mp = enumeration.GetManagementPack();
            enumeration.Status = ManagementPackElementStatus.PendingDelete;
            string enumInfo = _enumeration.Name;
            if (_enumeration.DisplayName != null)
            {
                enumInfo = _enumeration.DisplayName;
            }
            if(ShouldProcess(enumInfo))
            {
                mp.AcceptChanges();
            }
        }
    }

    [Cmdlet(VerbsCommon.Add, "SCSMEnumeration", SupportsShouldProcess = true)]
    public class AddSCSMEnumerationCommand : SMCmdletBase
    {
        #region Parameters
        private ManagementPackEnumeration _parent;
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public ManagementPackEnumeration Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }
        private String _name;
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private String _displayName;
        [Parameter]
        public String DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        private Double _ordinal;
        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public Double Ordinal
        {
            get { return _ordinal; }
            set { _ordinal = value; }
        }
        private ManagementPack _mp;
        [Parameter(Position = 3, Mandatory = true, ParameterSetName = "MP")]
        [Alias("mp")]
        public ManagementPack ManagementPack
        {
            get { return _mp; }
            set { _mp = value; }
        }
        private String _mpName;
        [Parameter(Position = 3, Mandatory = true, ParameterSetName = "MPName")]
        [Alias("MPName")]
        public String ManagementPackName
        {
            get { return _mpName; }
            set { _mpName = value; }
        }
        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        #endregion
        EnterpriseManagementGroup emg;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            emg = Parent.ManagementGroup;
            if (!emg.IsConnected)
            {
                emg.Reconnect();
            }
            if (ParameterSetName == "MPName")
            {
                WildcardPattern wp = new WildcardPattern(ManagementPackName, WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
                int mpMatchCount = 0;
                foreach (ManagementPack m in emg.ManagementPacks.GetManagementPacks())
                {
                    if ((wp.IsMatch(m.Name) || wp.IsMatch(m.DisplayName)) && m.Sealed)
                    {
                        mpMatchCount++;
                        ManagementPack = m;
                    }
                }
                if (mpMatchCount == 0)
                {
                    ThrowTerminatingError(new ErrorRecord(new ObjectNotFoundException(ManagementPackName + " could not be found"), "No MP", ErrorCategory.ObjectNotFound, ManagementPackName));
                }
                else if (mpMatchCount > 1)
                {
                    ThrowTerminatingError(new ErrorRecord(new ObjectNotFoundException(ManagementPackName + " matched multiple mps"), "Multiple MP", ErrorCategory.ObjectNotFound, ManagementPackName));
                }
            }
            if (ManagementPack.Sealed)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(ManagementPack.Name + " is sealed"), "Sealed MP", ErrorCategory.InvalidOperation, ManagementPack));
            }
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            ManagementPackEnumeration e = new ManagementPackEnumeration(ManagementPack, Name, ManagementPackAccessibility.Public);
            e.Ordinal = Ordinal;
            e.Parent = Parent;
            ManagementPack ParentMP = Parent.GetManagementPack();
            if (DisplayName != null) { e.DisplayName = DisplayName; }

            if (ShouldProcess(e.Name))
            {
                if (!ManagementPack.References.ContainsValue(ParentMP))
                {
                    WriteVerbose("Adding reference to " + ParentMP.Name);
                    // Errors here are not fatal
                    // but could be later (The MP may not have the appropriate references)
                    try { ManagementPack.References.Add(ParentMP.Name.Replace('.', '_'), ParentMP); } catch { ; }
                }
                ManagementPack.AcceptChanges();
            }
            if (PassThru)
            {
                WriteObject(e);
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "SCSMTopLevelEnumeration")]
    public class GetSMTopLevelEnumerationCommand : EntityTypeHelper
    {
        protected override void ProcessRecord()
        {
            Regex r = new Regex(Name, RegexOptions.IgnoreCase);
            foreach (ManagementPackEnumeration o in _mg.EntityTypes.GetTopLevelEnumerations())
            {
                if (r.Match(o.Name).Success)
                {
                    WriteObject(o);
                }
            }
        }
    }

    #endregion

    #region GroupCmdlets
    #region GetSCGroup
    [Cmdlet("Get", "SCGroup", DefaultParameterSetName = "DISPLAYNAME")]
    public class GetSCGroupCommand : GetGroupQueueCommand
    {
        public override string neededClassName
        {
            get { return "Microsoft.SystemCenter.ConfigItemGroup"; }
        }
    }
    #endregion

    #region NewSCGroup
    /// <summary>
    /// This cmdlet creates a new group
    /// TODO: Support dynamic members
    /// </summary>
    [Cmdlet("New", "SCGroup", SupportsShouldProcess = true)]
    public class NewSCGroupCommand : ObjectCmdletHelper
    {
        #region parameters
        private string _managementPackName = null;
        [Parameter(Position = 1, ParameterSetName = "MPName")]
        public string ManagementPackName
        {
            get { return _managementPackName; }
            set { _managementPackName = value; }
        }
        private string _managementPackFriendlyName = null;
        [Parameter(ParameterSetName = "MPName")]
        public string ManagementPackFriendlyName
        {
            get { return _managementPackFriendlyName; }
            set { _managementPackFriendlyName = value; }
        }

        private EnterpriseManagementObject[] _include = null;
        [Parameter(Position = 2, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public EnterpriseManagementObject[] Include
        {
            get { return _include; }
            set { _include = value; }
        }
        private EnterpriseManagementObject[] _exclude = null;
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public EnterpriseManagementObject[] Exclude
        {
            get { return _exclude; }
            set { _exclude = value; }
        }
        private EnterpriseManagementGroupObject[] _subGroup = null;
        [Parameter]
        public EnterpriseManagementGroupObject[] SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }
        private string _name = null;
        [Parameter(Position = 0)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _description;
        [Parameter(ParameterSetName = "MPName")]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        private ManagementPack _managementPack;
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "FromMP")]
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

        private SwitchParameter _import;
        [Parameter]
        public SwitchParameter Import
        {
            get { return _import; }
            set { _import = value; }
        }
        public enum __GroupType { InstanceGroup, WorkItemGroup };
        private __GroupType _groupType = __GroupType.InstanceGroup;
        [Parameter]
        public __GroupType GroupType
        {
            get { return _groupType; }
            set { _groupType = value; }
        }
        private SwitchParameter _force;
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        #endregion
        // private String myGroupType;
        // private Guid InstanceGroupRelationship;
        private ManagementPackModuleType GroupPopulatorModuleType;

        private Dictionary<ManagementPackClass, List<Guid>> excludedList;
        private Dictionary<ManagementPackClass, List<Guid>> includedList;
        private List<EnterpriseManagementGroupObject> subGroupCollection;

        private string myGroupName;
        private string myDiscoveryName;

        private string myRelationshipName;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (ParameterSetName == "MPName")
            {
                if (ManagementPackName == null)
                {
                    ManagementPackName = "MP_" + Guid.NewGuid().ToString().Replace("-", "");
                }
                if (ManagementPackFriendlyName == null)
                {
                    ManagementPackFriendlyName = ManagementPackName;
                }
            }
            if (Name == null)
            {
                Name = "Group_" + Guid.NewGuid().ToString().Replace("-", "");
            }
            includedList = new Dictionary<ManagementPackClass, List<Guid>>();
            excludedList = new Dictionary<ManagementPackClass, List<Guid>>();
            // Handle the case where you get something on the command line
            if (Exclude != null)
            {
                foreach (EnterpriseManagementObject emo in Exclude)
                {
                    ManagementPackClass c = emo.GetLeastDerivedNonAbstractClass();
                    if (!excludedList.ContainsKey(c))
                    {
                        List<Guid> l = new List<Guid>();
                        excludedList.Add(c, l);
                    }
                    excludedList[c].Add(emo.Id);
                }
            }
            if (Include != null)
            {
                foreach (EnterpriseManagementObject emo in Include)
                {
                    ManagementPackClass c = emo.GetLeastDerivedNonAbstractClass();
                    if (!includedList.ContainsKey(c))
                    {
                        List<Guid> l = new List<Guid>();
                        includedList.Add(c, l);
                    }
                    WriteVerbose("In BeginProcessing, adding " + emo.Id.ToString() + " to include list");
                    includedList[c].Add(emo.Id);
                }
            }
            if (SubGroup != null)
            {
                foreach (EnterpriseManagementGroupObject go in SubGroup)
                {
                    if (!go.ManagementPack.Sealed)
                    {
                        WriteError(new ErrorRecord(new InvalidOperationException(go.ManagementPack.Name + " is not sealed"), "Error adding SubGroup", ErrorCategory.InvalidOperation, go));
                    }
                    else
                    {
                        if (subGroupCollection == null)
                        {
                            subGroupCollection = new List<EnterpriseManagementGroupObject>();
                        }
                        subGroupCollection.Add(go);
                    }
                }
            }
        }
        protected override void ProcessRecord()
        {
            myGroupName = "Group_" + Guid.NewGuid().ToString().Replace("-", "");
            myDiscoveryName = myGroupName + ".Discovery";
            myRelationshipName = myGroupName + "_Contains_WorkItem";
            if (Include != null)
            {
                foreach (EnterpriseManagementObject o in Include)
                {
                    ManagementPackClass c = o.GetLeastDerivedNonAbstractClass();
                    if (!includedList.ContainsKey(c))
                    {
                        includedList.Add(c, new List<Guid>());
                    }
                    // This test is needed because we may have added to the include list in
                    // BeginProcessing
                    if (!includedList[c].Contains(o.Id))
                    {
                        WriteVerbose("ProcessRecord, adding " + o.Id.ToString() + " to include list");
                        includedList[c].Add(o.Id);
                    }
                }
            }
        }
        protected override void EndProcessing()
        {
            // We know that we need these at least.
            // for now, we'll just build a new MP and ignore the one we're handed
            ManagementPack mp;
            if (ParameterSetName == "FromMP")
            {
                mp = ManagementPack;
            }
            else
            {
                mp = new ManagementPack(ManagementPackName, ManagementPackFriendlyName, new Version(1, 0, 0, 0), _mg);
            }
            // Now collect MPs which we'll use in our references section
            #region References
            ManagementPack SysMP = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);
            ManagementPack scl = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.SystemCenter);
#if ( _SERVICEMANAGER_R2_ ) // R2 allows for version to be null
            ManagementPack IGL = _mg.ManagementPacks.GetManagementPack("Microsoft.SystemCenter.InstanceGroup.Library", SysMP.KeyToken, null);
            ManagementPack WI = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Library", SysMP.KeyToken, null);
            ManagementPack SWAL = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Activity.Library", SysMP.KeyToken, null);
            ManagementPack SNL = _mg.ManagementPacks.GetManagementPack("System.Notifications.Library", SysMP.KeyToken, null);
#else 
            ManagementPack IGL = _mg.ManagementPacks.GetManagementPack("Microsoft.SystemCenter.InstanceGroup.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack WI = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack SWAL = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Activity.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack SNL = _mg.ManagementPacks.GetManagementPack("System.Notifications.Library", SysMP.KeyToken, SysMP.Version);
#endif
            ManagementPack Windows = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.Windows);
            List<ManagementPack> mplist = new List<ManagementPack>();
            mplist.Add(SysMP);
            mplist.Add(scl);
            mplist.Add(IGL);
            mplist.Add(WI);
            mplist.Add(Windows);
            mplist.Add(SWAL);
            mplist.Add(SNL);
            foreach (ManagementPack m in mplist)
            {
                if (!mp.References.ContainsValue(m))
                {
                    try { mp.References.Add(m.Name.Replace('.', '_'), m); } catch { ; }
                }
            }

            GroupPopulatorModuleType = _mg.Monitoring.GetModuleType("Microsoft.SystemCenter.GroupPopulator", scl);
            #endregion
            ManagementPackClass baseClass = _mg.EntityTypes.GetClass("Microsoft.SystemCenter.ConfigItemGroup", IGL);
            if (GroupType == __GroupType.WorkItemGroup)
            {
                baseClass = _mg.EntityTypes.GetClass("System.WorkItemGroup", WI);
            }

            c = GetNewClass(mp, myGroupName, baseClass);

            ManagementPackDiscovery d = GetNewDiscovery(mp, myDiscoveryName, c);
            ManagementPackRelationship relationshipForWorkItemGroup = null;
            if (GroupType == __GroupType.WorkItemGroup)
            {
                // We need to create a new relationship type based on the object that we got.
                relationshipForWorkItemGroup = AddRelationship(mp, baseClass);
            }

            AddDiscoveryRelationship(d, GroupType, relationshipForWorkItemGroup, _mg);

            AddLanguagePack(mp, c, d, Name, Description);

            #region Configuration
            string alias = String.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("<RuleId>$MPElement$</RuleId>");
            sb.Append(String.Format("<GroupInstanceId>$MPElement[Name=\"{0}\"]$</GroupInstanceId>", myGroupName));
            sb.Append("<MembershipRules>");
            List<ManagementPackClass> IncludedAndExcluded = new List<ManagementPackClass>();
            foreach (ManagementPackClass key in includedList.Keys) { if (!IncludedAndExcluded.Contains(key)) { WriteVerbose("Adding key for " + key.Name); IncludedAndExcluded.Add(key); } }
            foreach (ManagementPackClass key in excludedList.Keys) { if (!IncludedAndExcluded.Contains(key)) { WriteVerbose("Adding key for " + key.Name); IncludedAndExcluded.Add(key); } }
            foreach (ManagementPackClass lc in IncludedAndExcluded)
            {
                bool found = false;
                sb.Append("<MembershipRule>");
                foreach (KeyValuePair<string, ManagementPackReference> kv in mp.References)
                {
                    if (lc.GetManagementPack() == kv.Value.GetManagementPack())
                    {
                        WriteVerbose("found a reference for " + lc.Name + " in " + lc.GetManagementPack().Name);
                        alias = kv.Key;
                        found = true;
                    }
                }
                if (!found)
                {
                    WriteVerbose("Adding reference for " + lc.GetManagementPack().Name);
                    // Add a reference!
                    alias = lc.GetManagementPack().Name.Replace('.', '_');
                    try { mp.References.Add(alias, lc.GetManagementPack()); } catch { ; }
                }
                if (alias == String.Empty) { throw new InvalidOperationException("could not find alias"); }
                string monitorString = String.Format("<MonitoringClass>$MPElement[Name=\"{0}!{1}\"]$</MonitoringClass>", alias, lc.Name);
                sb.Append(monitorString);
                // JWT
                sb.Append(String.Format("<RelationshipClass>$MPElement[Name=\"{0}!Microsoft.SystemCenter.InstanceGroupContainsEntities\"]$</RelationshipClass>", IGL.Name.Replace('.', '_')));
                if (includedList.ContainsKey(lc))
                {
                    sb.Append("<IncludeList>");
                    foreach (Guid g in includedList[lc])
                    {
                        sb.Append(String.Format("<MonitoringObjectId>{0}</MonitoringObjectId>", g));
                    }
                    sb.Append("</IncludeList>");
                }
                if (excludedList.ContainsKey(lc))
                {
                    sb.Append("<ExcludeList>");
                    foreach (Guid g in excludedList[lc])
                    {
                        sb.Append(String.Format("<MonitoringObjectId>{0}</MonitoringObjectId>", g));
                    }
                    sb.Append("</ExcludeList>");
                }
                sb.Append("</MembershipRule>");
            }
            sb.Append("</MembershipRules>");
            WriteVerbose(sb.ToString());
            d.DataSource.Configuration = sb.ToString();
            #endregion
            // Verification errors are fatal
            try
            {
                mp.AcceptChanges(ManagementPackVerificationTypes.XSDVerification);
                mp.Verify();
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "MPVerify", ErrorCategory.InvalidResult, mp));
            }
            // Import errors are fatal
            if (Import)
            {
                try
                {
                    if (ShouldProcess("Import " + myGroupName))
                    {
                        _mg.ManagementPacks.ImportManagementPack(mp);
                    }
                }
                catch (Exception e)
                {
                    if (Force)
                    {
                        WriteObject(mp);
                    }
                    ThrowTerminatingError(new ErrorRecord(e, "ImportGroupMP", ErrorCategory.InvalidResult, mp));
                }
            }
            if (PassThru)
            {
                if (ParameterSetName == "FromMP")
                {
                    WriteObject(c);
                }
                else
                {
                    WriteObject(mp);
                }
            }
        }

        private ManagementPackClass GetNewClass(ManagementPack m, string name, ManagementPackClass baseClass)
        {
            ManagementPackClass c = new ManagementPackClass(m, name, ManagementPackAccessibility.Public);
            c.Base = baseClass;
            c.Abstract = false;
            c.Hosted = false;
            c.Singleton = true;
            c.Extension = false;
            return c;
        }
        private ManagementPackDiscovery GetNewDiscovery(ManagementPack m, string name, ManagementPackClass target)
        {
            ManagementPackDiscovery d = new ManagementPackDiscovery(m, myDiscoveryName);
            d.Category = ManagementPackCategoryType.Discovery;
            d.Enabled = ManagementPackMonitoringLevel.@true;
            d.Target = target;
            d.ConfirmDelivery = false;
            d.Remotable = true;
            d.Priority = ManagementPackWorkflowPriority.Normal;
            return d;
        }

        private void AddDiscoveryRelationship(ManagementPackDiscovery d, __GroupType groupType, ManagementPackRelationship r, EnterpriseManagementGroup emg)
        {
            ManagementPackDiscoveryRelationship dr = new ManagementPackDiscoveryRelationship();
            if (groupType == __GroupType.InstanceGroup)
            {
                ManagementPack mpIGL = SMHelpers.GetManagementPack(ManagementPacks.Microsoft_SystemCenter_InstanceGroup_Library, emg);
                dr.TypeID = _mg.EntityTypes.GetRelationshipClass("Microsoft.SystemCenter.InstanceGroupContainsEntities", mpIGL);
            }
            else
            {
                dr.TypeID = r;
            }
            d.DiscoveryRelationshipCollection.Add(dr);
            ManagementPackDataSourceModule dsm = new ManagementPackDataSourceModule(d, "GroupPopulationDataSource");
            dsm.TypeID = (ManagementPackDataSourceModuleType)GroupPopulatorModuleType;
            d.DataSource = dsm;
            return;
        }
        private void AddLanguagePack(ManagementPack m, ManagementPackClass c, ManagementPackDiscovery d, string name, string classDescription)
        {
            ManagementPackLanguagePack lp = new ManagementPackLanguagePack(m, "ENU");
            lp.IsDefault = true;
            ManagementPackDisplayString ds1 = new ManagementPackDisplayString(c, "ENU");
            ds1.Name = name;
            ds1.Description = classDescription;
            ManagementPackDisplayString ds2 = new ManagementPackDisplayString(d, "ENU");
            ds2.Name = name + "_Discovery";
            ds2.Description = "Discovery for Group " + name;
        }
        private ManagementPackRelationship AddRelationship(ManagementPack m, ManagementPackClass targetClass)
        {
            ManagementPackRelationship r = new ManagementPackRelationship(m, myRelationshipName, ManagementPackAccessibility.Public);
            r.Accessibility = ManagementPackAccessibility.Public;
            r.Abstract = false;
            r.Base = SMHelpers.GetManagementPackRelationship(RelationshipTypes.Microsoft_SystemCenter_InstanceGroupContainsEntities, SMHelpers.GetManagementPack(ManagementPacks.Microsoft_SystemCenter_InstanceGroup_Library, _mg), _mg);
            ManagementPackRelationshipEndpoint source = new ManagementPackRelationshipEndpoint(r, "ContainedByGroup");
            source.MinCardinality = 0;
            source.MaxCardinality = Int32.MaxValue;
            source.Type = c;
            r.Source = source;
            ManagementPackRelationshipEndpoint target = new ManagementPackRelationshipEndpoint(r, "GroupContains");
            target.MinCardinality = 0;
            target.MaxCardinality = Int32.MaxValue;
            target.Type = targetClass;
            r.Target = target;
            return r;
        }

        ManagementPackClass c;
    }
    #endregion

    #region RemoveSCGroup
    [Cmdlet(VerbsCommon.Remove, "SCGroup", SupportsShouldProcess = true)]
    public class RemoveSCGroupCommand : SMCmdletBase
    {
        private EnterpriseManagementGroupObject[] _group;
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        public EnterpriseManagementGroupObject[] Group
        {
            get { return _group; }
            set { _group = value; }
        }
        protected override void ProcessRecord()
        {
            foreach (EnterpriseManagementGroupObject g in Group)
            {
                if (g.ManagementPack.Sealed)
                {
                    WriteError(new ErrorRecord(new InvalidOperationException("Can't remove from sealed management pack"), "SealedMP", ErrorCategory.InvalidOperation, g));
                }
                else
                {
                    if (ShouldProcess(g.DisplayName))
                    {
                        g.ManagementPack.DeleteEnterpriseManagementObjectGroup(g.__EnterpriseManagementObject);
                    }
                }
            }
        }
    }
    #endregion

    #region UpdateSCGroup
    /// <summary>
    /// This cmdlet updates a group
    /// </summary>
    /// 
    /*  Commenting out until it is completed
    [Cmdlet(VerbsData.Update, "SCGroup")]
    public class UpdateSCGroupCommand : ObjectCmdletHelper
    {
        private EnterpriseManagementGroupObject _group;
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public EnterpriseManagementGroupObject Group
        {
            get { return _group; }
            set { _group = value; }
        }
        override protected void ProcessRecord()
        {
            ThrowTerminatingError(new ErrorRecord(new NotImplementedException(), "Not yet", ErrorCategory.InvalidOperation, this));
        }

    }
    */ 
    #endregion
    #endregion

    #region QueueCmdlets
    #region GetSCQueue
    [Cmdlet("Get", "SCQueue", DefaultParameterSetName = "DISPLAYNAME")]
    public class GetSCQueueCommand : GetGroupQueueCommand
    {
        public override string neededClassName
        {
            get { return "System.WorkItemGroup"; }
        }
    }
    #endregion

    #region NewSCQueue
    /// <summary>
    /// Create a new queue
    /// Usage Pattern is:
    /// "Status -eq 'Active'" | new-scqueue -class (get-scsmclass workitem.incident$) -mp (get-scsmmanagementpack default)
    /// "DisplayName -eq 'foo'","DisplayName -eq 'bar'" | new-scqueue -class (get-scsmclass workitem.incident$) -mp (get-scsmmanagementpack default)
    /// etc
    /// which will create
    /// </summary>
    [Cmdlet(VerbsCommon.New, "SCQueue", SupportsShouldProcess = true)]
    public class NewSCQueueCommand : ObjectCmdletHelper
    {
        #region parameters
        private string _name;
        [Parameter(Position = 0)]
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
        private ManagementPack _managementPack;
        [Parameter(Position = 1, ParameterSetName = "MP")]
        [Alias("MP")]
        public ManagementPack ManagementPack
        {
            get { return _managementPack; }
            set { _managementPack = value; }
        }
        private string _managementPackName;
        [Parameter(Position = 1, ParameterSetName = "MPName", Mandatory = true)]
        public string ManagementPackName
        {
            get { return _managementPackName; }
            set { _managementPackName = value; }
        }
        private string _managementPackFriendlyName;
        [Parameter(ParameterSetName = "MPName")]
        public string ManagementPackFriendlyName
        {
            get { return _managementPackFriendlyName; }
            set { _managementPackFriendlyName = value; }
        }
        private ManagementPackClass _class;
        [Parameter(Position = 2, Mandatory = true)]
        public ManagementPackClass Class
        {
            get { return _class; }
            set { _class = value; }
        }

        private string[] _filter;
        [Parameter(Position = 3, Mandatory = true, ValueFromPipeline = true)]
        public string[] Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }
        private SwitchParameter _import;
        [Parameter]
        public SwitchParameter Import
        {
            get { return _import; }
            set { _import = value; }
        }
        // if force, then output the MP even if not verified
        private SwitchParameter _force;
        [Parameter]
        public SwitchParameter Force
        {
            get { return _force; }
            set { _force = value; }
        }
        #endregion

        private string FilterToDiscoveryCriteria(string filter, ManagementPackClass c)
        {
            Regex r = new Regex(" or | -or ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (r.Match(filter).Success)
            {
                ThrowTerminatingError(new ErrorRecord(new InvalidOperationException("OR is not allowed, only '-AND'"), "Bad Filter", ErrorCategory.InvalidOperation, filter));
            }
            ReadOnlyCollection<string> GenericProperties = EnterpriseManagementObjectCriteria.GetSpecialPropertyNames();
            CreatableEnterpriseManagementObject cemo = new CreatableEnterpriseManagementObject(c.ManagementGroup, c);
            ReadOnlyCollection<ManagementPackProperty> propertyList = (ReadOnlyCollection<ManagementPackProperty>)cemo.GetProperties();
            List<string> pNamelist = new List<string>();


            r = new Regex(" AND | -AND ", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            string[] subfilters = r.Split(filter);
            StringBuilder sb = new StringBuilder();
            sb.Append("<Expression>\r\n");
            bool multipleExpressions = false;
            if (subfilters.Length > 1)
            {
                multipleExpressions = true;
                sb.Append(" <And>\r\n");
            }
            foreach (string subfilter in subfilters)
            {
                bool found = false;
                ManagementPackProperty currentProperty = null;
                if (multipleExpressions)
                {
                    sb.Append("  <Expression>\r\n");
                }
                string sub = subfilter.Trim();
                PropertyOperatorValue POV = new PropertyOperatorValue(sub);
                sb.Append("   <SimpleExpression>\r\n");
                sb.Append("    <ValueExpression>\r\n");
                foreach (string s in GenericProperties)
                {
                    if (string.Compare(s, POV.Property, true) == 0)
                    {
                        found = true;
                        sb.Append("     <GenericProperty>" + s + "</GenericProperty>\r\n");
                    }
                }
                foreach (ManagementPackProperty p in propertyList)
                {
                    if (!found && string.Compare(p.Name, POV.Property, true) == 0)
                    {
                        // sb.Append("     <Property>" + p.Id + "</Property>\r\n");
                        sb.Append(String.Format("     <Property>$Context/Property[Type='{0}!{1}']/{2}$</Property>\r\n", c.GetManagementPack().Name.Replace('.', '_'), c.Name, p.Name));
                        currentProperty = p;
                    }
                }
                sb.Append("    </ValueExpression>\r\n");
                sb.Append("    <Operator>" + POV.Operator + "</Operator>\r\n");
                sb.Append("    <ValueExpression>\r\n");
                if (currentProperty != null && currentProperty.SystemType == typeof(Enum))
                {
                    ManagementPackElementReference<ManagementPackEnumeration> mpe = currentProperty.EnumType;
                    ManagementPackEnumeration e = SMHelpers.GetEnum(POV.Value, mpe.GetElement());
                    sb.Append("     <Value>{" + e.Id.ToString() + "}</Value>\r\n");
                }
                else
                {
                    sb.Append("     <Value>" + POV.Value + "</Value>\r\n");
                }
                sb.Append("    </ValueExpression>\r\n");
                sb.Append("   </SimpleExpression>\r\n");
                if (multipleExpressions)
                {
                    sb.Append("  </Expression>\r\n");
                }
            }
            if (multipleExpressions)
            {
                sb.Append(" </And>\r\n");
            }
            sb.Append("</Expression>\r\n");

            return sb.ToString();
        }
        private string myQueueName;
        private string myRelationshipName;
        private ManagementPackModuleType GroupPopulatorModuleType;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Name == null)
            {
                Name = "WorkItemGroup." + Guid.NewGuid().ToString().Replace("-", "");
            }
            if (ParameterSetName == "MPName" && ManagementPackFriendlyName == null)
            {
                ManagementPackFriendlyName = ManagementPackName;
            }
            myQueueName = "WorkItemGroup." + Guid.NewGuid().ToString().Replace("-", "");
            myDiscoveryName = myQueueName + ".Discovery";
            myRelationshipName = myQueueName + "_Contains_" + Class.Name;
        }
        private string convertedFilter;
        protected override void ProcessRecord()
        {
            foreach (string f in Filter)
            {
                convertedFilter = FilterToDiscoveryCriteria(f, Class);
                WriteVerbose(convertedFilter);
            }
        }
        private string myDiscoveryName;
        protected override void EndProcessing()
        {
            ManagementPack mp;

            if (ParameterSetName == "MP")
            {
                mp = ManagementPack;
            }
            else
            {
                mp = new ManagementPack(ManagementPackName, ManagementPackFriendlyName, new Version(1, 0, 0, 0), _mg);
            }

            #region References

            // Now collect MPs which we'll use in our references section
            ManagementPack SysMP = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);
            ManagementPack scl = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.SystemCenter);
            ManagementPack Windows = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.Windows);
#if ( _SERVICEMANAGER_R2_ ) // R2 allows for version to be null
            ManagementPack IGL = _mg.ManagementPacks.GetManagementPack("Microsoft.SystemCenter.InstanceGroup.Library", SysMP.KeyToken, null);
            ManagementPack WI = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Library", SysMP.KeyToken, null);
            ManagementPack SWAL = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Activity.Library", SysMP.KeyToken, null);
            ManagementPack SNL = _mg.ManagementPacks.GetManagementPack("System.Notifications.Library", SysMP.KeyToken, null);
#else
            ManagementPack IGL = _mg.ManagementPacks.GetManagementPack("Microsoft.SystemCenter.InstanceGroup.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack WI = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack SWAL = _mg.ManagementPacks.GetManagementPack("System.WorkItem.Activity.Library", SysMP.KeyToken, SysMP.Version);
            ManagementPack SNL = _mg.ManagementPacks.GetManagementPack("System.Notifications.Library", SysMP.KeyToken, SysMP.Version);
#endif
            ManagementPack classMP = Class.GetManagementPack();
            List<ManagementPack> mplist = new List<ManagementPack>();
            mplist.Add(SysMP);
            mplist.Add(scl);
            mplist.Add(IGL);
            mplist.Add(WI);
            mplist.Add(Windows);
            mplist.Add(SWAL);
            mplist.Add(SNL);
            mplist.Add(classMP);
            foreach (ManagementPack m in mplist)
            {
                if (!mp.References.ContainsValue(m))
                {
                    try { mp.References.Add(m.Name.Replace('.', '_'), m); } catch { ; }
                }
            }

            #endregion

            GroupPopulatorModuleType = _mg.Monitoring.GetModuleType("Microsoft.SystemCenter.GroupPopulator", scl);

            ManagementPackClass classWorkItemGroup = _mg.EntityTypes.GetClass("System.WorkItemGroup", WI);

            ManagementPackClass c = GetNewClass(mp, myQueueName, classWorkItemGroup);

            ManagementPackDiscovery d = GetNewDiscovery(mp, myDiscoveryName, c);

            ManagementPackRelationship relationshipForWorkItemGroup = AddRelationship(mp, Class, c);

            AddDiscoveryRelationship(d, relationshipForWorkItemGroup);

            AddLanguagePack(mp, c, d, Name, Description);

            #region Configuration

            string alias = String.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("<RuleId>$MPElement$</RuleId>\r\n");
            sb.Append(String.Format("<GroupInstanceId>$MPElement[Name=\"{0}\"]$</GroupInstanceId>\r\n", myQueueName));
            sb.Append("<MembershipRules>\r\n");
            sb.Append("<MembershipRule>\r\n");
            sb.Append(String.Format("<MonitoringClass>$MPElement[Name=\"{0}!{1}\"]$</MonitoringClass>\r\n", Class.GetManagementPack().Name.Replace('.', '_'), Class.Name));
            sb.Append(String.Format("<RelationshipClass>$MPElement[Name=\"{0}\"]$</RelationshipClass>\r\n", myRelationshipName));


            sb.Append(convertedFilter);
            sb.Append("</MembershipRule>\r\n");
            sb.Append("</MembershipRules>\r\n");
            WriteVerbose(sb.ToString());
            d.DataSource.Configuration = sb.ToString();

            #endregion

            // Verification errors are fatal
            try
            {
                mp.AcceptChanges(ManagementPackVerificationTypes.XSDVerification);
                mp.Verify();
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "MPVerify", ErrorCategory.InvalidResult, mp));
            }
            // Import errors are fatal
            if (Import)
            {
                try
                {
                    if (ShouldProcess("Import " + myQueueName))
                    {
                        _mg.ManagementPacks.ImportManagementPack(mp);
                    }
                }
                catch (Exception e)
                {
                    ThrowTerminatingError(new ErrorRecord(e, "ImportGroupMP", ErrorCategory.InvalidResult, mp));
                }
            }
            if (PassThru)
            {
                if (ParameterSetName == "MP")
                {
                    WriteObject( c );
                }
                else
                {
                    WriteObject( mp );
                }
            }

        }
        private ManagementPackClass GetNewClass(ManagementPack m, string name, ManagementPackClass baseClass)
        {
            ManagementPackClass c = new ManagementPackClass(m, name, ManagementPackAccessibility.Public);
            c.Base = baseClass;
            c.Abstract = false;
            c.Hosted = false;
            c.Singleton = true;
            c.Extension = false;
            return c;
        }
        private ManagementPackDiscovery GetNewDiscovery(ManagementPack m, string name, ManagementPackClass target)
        {
            ManagementPackDiscovery d = new ManagementPackDiscovery(m, myDiscoveryName);
            d.Category = ManagementPackCategoryType.Discovery;
            d.Enabled = ManagementPackMonitoringLevel.@true;
            d.Target = target;
            d.ConfirmDelivery = false;
            d.Remotable = true;
            d.Priority = ManagementPackWorkflowPriority.Normal;
            return d;
        }
        private void AddDiscoveryRelationship(ManagementPackDiscovery d, ManagementPackRelationship r)
        {
            ManagementPackDiscoveryRelationship dr = new ManagementPackDiscoveryRelationship();
            dr.TypeID = r;
            d.DiscoveryRelationshipCollection.Add(dr);
            ManagementPackDataSourceModule dsm = new ManagementPackDataSourceModule(d, "GroupPopulationDataSource");
            dsm.TypeID = (ManagementPackDataSourceModuleType)GroupPopulatorModuleType;
            d.DataSource = dsm;
            return;
        }
        private void AddLanguagePack(ManagementPack m, ManagementPackClass c, ManagementPackDiscovery d, string name, string classDescription)
        {
            ManagementPackLanguagePack lp = new ManagementPackLanguagePack(m, "ENU");
            lp.IsDefault = true;
            ManagementPackDisplayString ds1 = new ManagementPackDisplayString(c, "ENU");
            ds1.Name = name;
            ds1.Description = classDescription;
            ManagementPackDisplayString ds2 = new ManagementPackDisplayString(d, "ENU");
            ds2.Name = name + "_Discovery";
            ds2.Description = "Discovery for Group " + name;
        }
        private ManagementPackRelationship AddRelationship(ManagementPack m, ManagementPackClass targetClass, ManagementPackClass sourceClass)
        {
            ManagementPackRelationship r = new ManagementPackRelationship(m, myRelationshipName, ManagementPackAccessibility.Public);
            r.Accessibility = ManagementPackAccessibility.Public;
            r.Abstract = false;
            r.Base = SMHelpers.GetManagementPackRelationship(RelationshipTypes.System_WorkItemGroupContainsWorkItems, SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Library, _mg), _mg);
            ManagementPackRelationshipEndpoint source = new ManagementPackRelationshipEndpoint(r, "ContainedByGroup");
            source.MinCardinality = 0;
            source.MaxCardinality = Int32.MaxValue;
            source.Type = sourceClass;
            r.Source = source;
            ManagementPackRelationshipEndpoint target = new ManagementPackRelationshipEndpoint(r, "GroupContains");
            target.MinCardinality = 0;
            target.MaxCardinality = Int32.MaxValue;
            target.Type = targetClass;
            r.Target = target;
            return r;
        }

    }
    #endregion

    #region RemoveSCQueue
    [Cmdlet(VerbsCommon.Remove, "SCQueue", SupportsShouldProcess = true)]
    public class RemoveSCQueueCommand : SMCmdletBase
    {
        private EnterpriseManagementGroupObject[] _queue;
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        public EnterpriseManagementGroupObject[] Queue
        {
            get { return _queue; }
            set { _queue = value; }
        }
        protected override void ProcessRecord()
        {
            foreach (EnterpriseManagementGroupObject q in Queue)
            {
                if (q.ManagementPack.Sealed)
                {
                    WriteError(new ErrorRecord(new InvalidOperationException("Can't remove from sealed management pack"), "SealedMP", ErrorCategory.InvalidOperation, q));
                }
                else
                {
                    if (ShouldProcess(q.DisplayName))
                    {
                        q.ManagementPack.DeleteEnterpriseManagementObjectGroup(q.__EnterpriseManagementObject);
                    }
                }
            }
        }
    }
    #endregion
    #endregion

    [Cmdlet(VerbsCommon.Get, "SCSMClassProperty")]
    public class GetSMClassPropertyCommand : SMCmdletBase
    {
        // Parameters
        private ManagementPackClass _class = null;
        [Parameter(ParameterSetName = "Class", Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public ManagementPackClass Class
        {
            get { return _class; }
            set { _class = value; }
        }


        private SwitchParameter _recursive;
        [Parameter]
        public SwitchParameter Recursive
        {
            get { return _recursive; }
            set { _recursive = value; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            WriteVerbose("Process class " + this.Class.Name);
            if (this.Recursive.ToBool())
            {
                this.WriteVerbose("Return recursive list");
                this.WriteObject(this.Class.GetProperties(BaseClassTraversalDepth.Recursive), true);
            }
            else
            {
                this.WriteVerbose("Return list only for current class");
                this.WriteObject(this.Class.GetProperties(), true);
            }
        }

    }
}
