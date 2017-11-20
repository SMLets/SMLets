using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Subscriptions;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using System.Globalization;

namespace SMLets
{
    public class SMCmdletBase : PSCmdlet
    {
        // This contains the ComputerName parameter and the 
        // setup for the ManagementGroup where needed

        // data
        internal EnterpriseManagementGroup _mg;
        // Parameters
        private string _computerName = "localhost";
        [Parameter(Mandatory = false, HelpMessage = "The computer to use for the connection to the Service Manager Data Access Service")]
        [ValidateNotNullOrEmpty]
        public string ComputerName
        {
            get { return _computerName; }
            set { _computerName = value; }
        }
        private PSCredential _credential = null;
        [Parameter]
        public PSCredential Credential
        {
            get { return _credential; }
            set { _credential = value; }
        }
        private EnterpriseManagementGroup _scsmSession = null;
        [Parameter(HelpMessage = "A connection to a Service Manager Data Access Service")]
        public EnterpriseManagementGroup SCSMSession
        {
            get { return _scsmSession; }
            set { _scsmSession = value; }
        }

        private string _threeLetterWindowsLanguageName = CultureInfo.CurrentUICulture.ThreeLetterWindowsLanguageName;
        [Parameter(HelpMessage = "Language code for connection. The deafult is current UI Culture", Mandatory = false)]
        [ValidateNotNullOrEmpty]
        public string ThreeLetterWindowsLanguageName
        {
            get { return _threeLetterWindowsLanguageName; }
            set { _threeLetterWindowsLanguageName = value; }
        }
        
        protected override void BeginProcessing()
        {
            try
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
                    PSVariable DefaultSession = SessionState.PSVariable.Get("SMDefaultSession");
                    if (DefaultSession != null && (DefaultSession.Value is EnterpriseManagementGroup || (DefaultSession.Value is PSObject && (DefaultSession.Value as PSObject).BaseObject is EnterpriseManagementGroup)))
                    {
                        _mg = DefaultSession.Value is EnterpriseManagementGroup? 
                            (EnterpriseManagementGroup)DefaultSession.Value:
                             (EnterpriseManagementGroup)(DefaultSession.Value as PSObject).BaseObject;
                        ConnectionHelper.SetMG(_mg);
                    }
                    else
                    {
                        PSVariable DefaultComputer = SessionState.PSVariable.Get("SMDefaultComputer");
                        if (DefaultComputer != null)
                        {
                            _mg = ConnectionHelper.GetMG(DefaultComputer.Value.ToString(), _credential, this._threeLetterWindowsLanguageName);
                        }
                        else
                        {
                            _mg = ConnectionHelper.GetMG(ComputerName, _credential, this._threeLetterWindowsLanguageName);
                        }
                    }
                }
            }
            catch (Exception e) // If we had a problem, the connection is bad, so we have to stop
            {
                ThrowTerminatingError(
                        new ErrorRecord(e, "GenericMessage",
                            ErrorCategory.InvalidOperation, ComputerName)
                        );
            }
        }
    }

    internal class DataAdapter
    {
        public string Assembly;
        public string Name;
        public string Type;
    }

    public class Column
    {
        public string Name;
        public string DisplayMemberBinding;
        public string Width;
        public string DisplayNameId;
        public string DisplayNameString;
        public string Property;
        public string DataType;
    }

    public class ItemStatistics
    {
        public Object Type;
        public string TypeName;
        public int Count;
        public ItemStatistics(Object o, string s, int c)
        {
            Type = o;
            TypeName = s;
            Count = c;
        }
    }

    public class UserHelper
    {
        public static object GetUserObjectFromString(EnterpriseManagementGroup EMG, string userName, Cmdlet currentCmdlet)
        {
            try
            {
                ManagementPackClass userClass = EMG.EntityTypes.GetClass("System.Domain.User", EMG.ManagementPacks.GetManagementPack(SystemManagementPack.System));
                string name = userName.Split('\\')[1];
                string domain = userName.Split('\\')[0];
                EnterpriseManagementObjectCriteria c = new EnterpriseManagementObjectCriteria(String.Format("UserName = '{0}' and Domain = '{1}'", name, domain), userClass);
                IObjectReader<EnterpriseManagementObject> reader = EMG.EntityObjects.GetObjectReader<EnterpriseManagementObject>(c, ObjectQueryOptions.Default);
                if (reader.Count == 1)
                {
                    return ServiceManagerObjectHelper.AdaptManagementObject(reader.GetData(0));
                }
                else
                {
                    return userName;
                }
            }
            catch (Exception e)
            {
                currentCmdlet.WriteError(new ErrorRecord(e, "GetUserObjectFromString", ErrorCategory.NotSpecified, userName));
                return userName;
            }
        }
    }

    public class EnterpriseManagementGroupObject
    {
        public EnterpriseManagementObject __EnterpriseManagementObject;
        public ManagementPackClass __Class;
        public String Description;
        public String DisplayName;
        public Guid Id;
        public String Name;
        public ManagementPack ManagementPack;
        public List<XmlNode> MembershipRules;
        public List<EnterpriseManagementObject> IncludeList;
        public List<EnterpriseManagementObject> ExcludeList;
        public List<EnterpriseManagementObject> Members;
        public string Configuration;
        public EnterpriseManagementGroupObject(EnterpriseManagementObject emo)
        {
            __EnterpriseManagementObject = emo;
            __Class = emo.GetLeastDerivedNonAbstractClass();
            Id = emo.Id;
            Description = emo.GetLeastDerivedNonAbstractClass().Description;
            DisplayName = emo.GetLeastDerivedNonAbstractClass().DisplayName;
            Name = __Class.Name;
            ManagementPack = __Class.GetManagementPack();
            ManagementPackDiscovery d = ManagementPack.GetDiscovery(Name + ".Discovery");
            Configuration = d.DataSource.Configuration;
            XmlDocument xmld = new XmlDocument();
            xmld.LoadXml(d.CreateNavigator().OuterXml);
            MembershipRules = new List<XmlNode>();
            Hashtable includeHT = new Hashtable();
            Hashtable excludeHT = new Hashtable();
            XmlNodeList l;
            foreach (XmlNode node in xmld.SelectNodes("Discovery/DataSource/MembershipRules/MembershipRule"))
            {
                MembershipRules.Add(node);
                l = node.SelectNodes("IncludeList/MonitoringObjectId");
                if (l.Count > 0)
                {
                    foreach (XmlNode MO in l)
                    {
                        string value = MO.FirstChild.Value;
                        if (value != string.Empty && !includeHT.ContainsKey(value)) { includeHT.Add(value, 1); }
                    }
                }
                l = node.SelectNodes("ExcludeList/MonitoringObjectId");
                if (l.Count > 0)
                {
                    foreach (XmlNode MO in l)
                    {
                        string value = MO.FirstChild.Value;
                        if (value != string.Empty && !excludeHT.ContainsKey(value)) { excludeHT.Add(value, 1); }
                    }
                }

            }
            IncludeList = new List<EnterpriseManagementObject>();

            foreach (string s in includeHT.Keys)
            {
                IncludeList.Add(emo.ManagementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid(s), ObjectQueryOptions.Default));
            }
            ExcludeList = new List<EnterpriseManagementObject>();
            foreach (string s in excludeHT.Keys)
            {
                ExcludeList.Add(emo.ManagementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(new Guid(s), ObjectQueryOptions.Default));
            }

            Members = new List<EnterpriseManagementObject>();
            foreach (EnterpriseManagementObject remo in emo.ManagementGroup.EntityObjects.GetRelatedObjects<EnterpriseManagementObject>(emo.Id, TraversalDepth.OneLevel, ObjectQueryOptions.Default))
            {
                Members.Add(remo);
            }
        }
    }

    public class WorkflowHelper
    {
        public static PSObject[] GetJobStatus(PSObject instance)
        {
            ManagementPackRule rule = (ManagementPackRule)instance.BaseObject;
            List<PSObject> statuslist = new List<PSObject>();
            foreach (SubscriptionJobStatus s in rule.ManagementGroup.Subscription.GetSubscriptionStatusById(rule.Id))
            {
                PSObject o = new PSObject(s);
                // sometimes the object id is empty, handle that gracefully
                try
                {
                    if (s.ObjectId == Guid.Empty)
                    {
                        o.Members.Add(new PSNoteProperty("Object", null));
                    }
                    else
                    {
                        o.Members.Add(new PSNoteProperty("Object", rule.ManagementGroup.EntityObjects.GetObject<EnterpriseManagementObject>(s.ObjectId, ObjectQueryOptions.Default)));
                    }
                }
                catch
                {
                    o.Members.Add(new PSNoteProperty("Object", null));
                }

                o.Members.Add(new PSNoteProperty("Rule", rule.ManagementGroup.Monitoring.GetRule(s.RuleId)));
                statuslist.Add(o);
            }
            PSObject[] jobstatus = new PSObject[statuslist.Count];
            statuslist.CopyTo(jobstatus);
            return jobstatus;
        }
    }

    public sealed class ConnectionHelper
    {
        private static Hashtable ht;
        private static object locker = new object();
        public static List<string> GetMGList()
        {
            return GetMGList(".*");
        }
        public static List<string> GetMGList(string re)
        {
            List<string> l = new List<string>();
            Regex r = new Regex(re,RegexOptions.IgnoreCase);
            foreach(string k in ht.Keys)
            {
                if ( r.Match(k).Success )
                {
                    l.Add(k.ToString());
                }
            }
            return l;
        }
        public static EnterpriseManagementGroup GetMG(string computerName)
        {
            if ( ht == null ) { ht = new Hashtable(StringComparer.OrdinalIgnoreCase); }
            if ( ht.ContainsKey(computerName)) 
            { 
                return (EnterpriseManagementGroup)ht[computerName]; 
            }
            else
            {
                return null;
            }
        }
        public static EnterpriseManagementGroup GetMG(string computerName, PSCredential credential, string threeLetterWindowsLanguageName)
        {
            lock (locker)
            {
                string sessionId = GenereateUniqSessionId(computerName, threeLetterWindowsLanguageName, credential);
                if (ht == null) { ht = new Hashtable(StringComparer.OrdinalIgnoreCase); }
                if (!ht.ContainsKey(sessionId))
                {
                    EnterpriseManagementGroup emg;
                    EnterpriseManagementConnectionSettings settings = new EnterpriseManagementConnectionSettings(computerName);
                    settings.ThreeLetterWindowsLanguageName = threeLetterWindowsLanguageName;

                    if (credential != null)
                    {

                        settings.UserName = credential.GetNetworkCredential().UserName;
                        settings.Domain = credential.GetNetworkCredential().Domain;
                        settings.Password = credential.Password;

                    }

                    emg = new EnterpriseManagementGroup(settings);
                    try
                    {
                        ht.Add(sessionId, emg);
                    }
                    catch (Exception er)
                    {
                        throw new Exception( string.Format("Unable to add new session {0}", sessionId), er);
                    }
                }
                if (!((EnterpriseManagementGroup)ht[sessionId]).IsConnected)
                {
                    ((EnterpriseManagementGroup)ht[sessionId]).Reconnect();
                }
                return ht[sessionId] as EnterpriseManagementGroup;
            }
        }

        public static void SetMG(EnterpriseManagementGroup emg)
        {
            if ( ht == null ) { ht = new Hashtable(StringComparer.OrdinalIgnoreCase); }
            string sessionId = GenereateUniqSessionId(emg.ConnectionSettings);
            if (!ht.ContainsKey(sessionId))
            {
                ht.Add(sessionId, emg);
            }
        }
        public static void RemoveMG(EnterpriseManagementConnectionSettings settings)
        {
            string sessionId = GenereateUniqSessionId(settings);
            if ( ht == null ) { ht = new Hashtable(StringComparer.OrdinalIgnoreCase); }
            if (ht.ContainsKey(sessionId))
            {
                ht.Remove(sessionId);
            }
        }
        private ConnectionHelper() { ; }

        private static string GenereateUniqSessionId(string serverName, string threeLetterWindowsLanguageName, string userName, string domain)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(domain))
                return serverName + "_" + threeLetterWindowsLanguageName + "_" + domain + "_" + userName;
            else
                return serverName + "_" + threeLetterWindowsLanguageName;
        }

        private static string GenereateUniqSessionId(string serverName, string threeLetterWindowsLanguageName, PSCredential credential)
        {
            if (credential != null)
                return serverName + "_" + threeLetterWindowsLanguageName + "_" + credential.GetNetworkCredential().Domain + "_" + credential.GetNetworkCredential().UserName;
            else
                return serverName + "_" + threeLetterWindowsLanguageName;
        }

        private static string GenereateUniqSessionId(EnterpriseManagementConnectionSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.UserName) && !string.IsNullOrEmpty(settings.Domain))
                return settings.ServerName + "_" + settings.ThreeLetterWindowsLanguageName + "_" + settings.Domain + "_" + settings.UserName;
            else
                return settings.ServerName + "_" + settings.ThreeLetterWindowsLanguageName;
        }
    }

    public sealed class ObjectHelper
    {
        public static PSObject PromoteProjectionProperties(EnterpriseManagementObjectProjection projection, string node, string typeName)
        {
            PSObject PromotedObject = new PSObject();
            PromotedObject.TypeNames[0] = typeName;
            try
            {
                Collection<PSObject> objectList = new Collection<PSObject>();
                XPathNavigator Navigator = projection.CreateNavigator();
                if ( Navigator.Select(node).Count > 1)
                {
                    foreach(XPathNavigator xnav in Navigator.Select(node))
                    {
                        PSObject listmember = new PSObject();
                        IComposableProjection composedProjection = (IComposableProjection)xnav.UnderlyingObject;
                        listmember.Members.Add(new PSNoteProperty("__base", composedProjection.Object));
                        foreach ( ManagementPackProperty p in composedProjection.Object.GetProperties())
                        {
                            listmember.Members.Add(new PSNoteProperty(p.Name, composedProjection.Object[p].Value));
                        }
                        objectList.Add(listmember);
                    }
                    PromotedObject.Members.Add(new PSNoteProperty(typeName, objectList));

                }
                else
                {
                    XPathNavigator singleNodeNavigator = Navigator.SelectSingleNode(node);
                    IComposableProjection composedProjection = (IComposableProjection)singleNodeNavigator.UnderlyingObject;
                    PromotedObject.Members.Add(new PSNoteProperty("__base", composedProjection.Object));
                    foreach ( ManagementPackProperty p in composedProjection.Object.GetProperties())
                    {
                        PromotedObject.Members.Add(new PSNoteProperty(p.Name, composedProjection.Object[p].Value));
                    }
                }
            }
            catch ( Exception e )
            {
                PromotedObject.Members.Add(new PSNoteProperty("PromotionFailure",e.Message));
            }
            return PromotedObject;
        }
        // This looks through *all* the enumerations in the system
        // This could be done more efficiently, but at least does't
        // require a round trip to the server since these are cached
        // the client side.
        public static ManagementPackEnumeration GetEnumerationFromName(EnterpriseManagementGroup emg, string name)
        {
            foreach( ManagementPackEnumeration e in emg.EntityTypes.GetEnumerations())
            {
                int CompareResult = String.Compare(e.Name, name, StringComparison.OrdinalIgnoreCase);
                if ( CompareResult == 0)
                {
                    return e;
                }
            }
            return null;
        }

        private ObjectHelper() { ; }
    }

    public class ProjectionConverter : PSTypeConverter
    {
        // Currently, this converter is only targeted at/ EnterpriseManagementObjectProjection, 
        // (via the SMLets.Types.ps1xml file) but could easily be extended

        public override bool CanConvertFrom(object source, Type destination) 
        { 
            PSObject o = source as PSObject;
            return CanConvertFrom(o, destination);
        }
        public override bool CanConvertFrom(PSObject source, Type destination)
        {
            if ( source.Properties["__base"] != null )
            {
                return true;
            }
            return false;
        }
        public override object ConvertFrom(Object source, Type destination, IFormatProvider p, bool ignoreCase )
        {
            PSObject o = source as PSObject;
            if ( o == null ) { throw new InvalidCastException("Conversion failed"); }
            if ( this.CanConvertFrom(o, destination))
            {
                try
                {
                    return o.Properties["__base"].Value;
                }
                catch
                {
                    throw new InvalidCastException("Conversion failed");
                }
            }
            throw new InvalidCastException("Conversion failed");
        }
        public override object ConvertFrom(PSObject source, Type destination, IFormatProvider p, bool ignoreCase )
        {
            if ( source == null ) { throw new InvalidCastException("Conversion failed"); }
            if ( this.CanConvertFrom(source, destination))
            {
                try
                {
                    return source.Properties["__base"].Value;
                }
                catch
                {
                    throw new InvalidCastException("Conversion failed");
                }
            }
            throw new InvalidCastException("Conversion failed");
        }
        public override bool CanConvertTo(object source, Type destination) 
        { 
            PSObject o = source as PSObject;
            return CanConvertTo(o, destination);
        }
        public override bool CanConvertTo(PSObject value, Type destination)
        {
            return false;
        }
        public override object ConvertTo(object value, Type destination, IFormatProvider p, bool ignoreCase)
        {
            throw new InvalidCastException("Conversion failed");
        }
        public override object ConvertTo(PSObject value, Type destination, IFormatProvider p, bool ignoreCase)
        {
            throw new InvalidCastException("Conversion failed");
        }
    }

    public class PropertyOperatorValue
    {
        public string Property;
        public string Operator;
        public string Value;
        public PropertyOperatorValue() { ; }
        public PropertyOperatorValue(string filter)
        {
            RegexOptions ropt = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
            Regex r = new Regex("(?<Property>.*)\\s+(?<Operator>-like|-notlike|=|==|<|>|!=|-eq|-ne|-gt|-ge|-le|-lt|-isnull|-isnotnull)\\s+(?<Value>.*)", ropt);
            // OK - we have a filter we can use
            Match m = r.Match(filter);
            if (!m.Success) { throw new InvalidOperationException("Filter '" + filter + "' is invalid"); }
            Property = m.Groups["Property"].Value.Trim();
            Operator = GetOperator(m.Groups["Operator"].Value.Trim().ToLower());
            // this now handles wildcard characters in a simple minded way
            Value = m.Groups["Value"].Value.Trim().Trim('"', '\'').Replace("*", "%").Replace("?", "_");
        }
        public static string GetOperator(string myOperator)
        {
            switch (myOperator.ToLowerInvariant())
            {
                case "-like":                     return "Like";
                case "-notlike":                  return "NotLike";
                case "-eq": case "=": case "==":  return "Equal";
                case "-ne": case "!=":            return "NotEqual";
                case "-gt": case ">":             return "Greater";
                case "-ge": case ">=":            return "GreaterEqual";
                case "-le": case "<=":            return "LessEqual";
                case "-lt": case "<":             return "Less";
                case "-isnull":                   return "Is Null";
                case "-isnotnull":                return "Is Not Null";
                default: throw new InvalidOperationException("'" + myOperator + "' is not a valid operator");
            }
        }
    }

    public static class CriteriaHelper<T>
    {
        //
        // Usage pattern:
        // ManagementPackClassCriteria mpcc = CriteriaHelper<ManagementPackClassCriteria>.CreateGenericCriteria("Name -like '*Entity'");
        // EnterpriseManagementRelationshipObjectGenericCriteria foo = CriteriaHelper<EnterpriseManagementRelationshipObjectGenericCriteria>.CreateCriteria("TargetObjectDisplayName -like 'Custom%'");
        // 

        // If the Criteria schema changes this will break
        public const string CriteriaFormatString = "<Criteria xmlns='http://Microsoft.EnterpriseManagement.Core.Criteria/'><Expression><SimpleExpression><ValueExpressionLeft><GenericProperty>{0}</GenericProperty></ValueExpressionLeft><Operator>{1}</Operator><ValueExpressionRight><Value>{2}</Value></ValueExpressionRight></SimpleExpression></Expression></Criteria>";
        public static string critXml;

        public static string getGenericXml(Type t, string filter)
        {
            String message = "unknown error";
            try
            {
                PropertyOperatorValue POV = new PropertyOperatorValue(filter);

                BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
                string method = "GetValidPropertyNames";
                if (string.Compare(t.Name, "EnterpriseManagementObjectCriteria", true) == 0) { method = "GetSpecialPropertyNames"; }
                ReadOnlyCollection<string> propertyNames = (ReadOnlyCollection<String>)t.InvokeMember(method, flags, null, t, null);
                string[] names = new string[propertyNames.Count];
                propertyNames.CopyTo(names,0);
                message = String.Format("Property '{0}' not found, allowed values: {1}", POV.Property, String.Join(", ", names));
                foreach (string pn in propertyNames)
                {
                    if (String.Compare(pn, POV.Property, true) == 0)
                    {
                        return String.Format(CriteriaFormatString, pn, POV.Operator, POV.Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            throw new ObjectNotFoundException(message);
        }
        // Usefull for Generic Criteria and ManagementPack
        public static T CreateGenericCriteria(string filter)
        {
            critXml = getGenericXml(typeof(T), filter);
            if (critXml == null) { return default(T); }
            return (T)Activator.CreateInstance(typeof (T), critXml);
        }
        public static T CreateGenericCriteria(string filter, ManagementPackTypeProjection p, EnterpriseManagementGroup emg)
        {
            critXml = getGenericXml(typeof(T), filter);
            if (critXml == null) { return default(T); }
            return (T)Activator.CreateInstance(typeof(T), critXml,p,emg);
        }
        public static T CreateGenericCriteria(string filter, ManagementPackClass c, EnterpriseManagementGroup emg)
        {
            critXml = getGenericXml(typeof(T), filter);
            if (critXml == null) { return default(T); }
            return (T)Activator.CreateInstance(typeof(T), critXml, c, emg);
        }
    }

    public static class SMHelpers
    {
        public static bool GuidTryParse(string s, out Guid result)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Regex format = new Regex(
                "^[A-Fa-f0-9]{32}$|" +
                "^({|\\()?[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}(}|\\))?$|" +
                "^({)?[0xA-Fa-f0-9]{3,10}(, {0,1}[0xA-Fa-f0-9]{3,6}){2}, {0,1}({)([0xA-Fa-f0-9]{3,4}, {0,1}){7}[0xA-Fa-f0-9]{3,4}(}})$");
            Match match = format.Match(s);
            if (match.Success)
            {
                result = new Guid(s);
                return true;
            }
            else
            {
                result = Guid.Empty;
                return false;
            }
        }

        public static ManagementPackEnumeration GetEnum(string identifier, ManagementPackEnumeration etype)
        {
            // Try really hard to find an enumeration based on a parent enumeration
            // Match in the following order:
            //   ID
            //   Name
            //   The last token of the Name after the last "."
            //   The DisplayName
            //   A regex match of the name
            //   A regex match of the DisplayName
            // If any of those is found, return the first one you succeed in matching
            // if you still can't find a match, then throw and provide a helping message

            if (etype == null) { throw new ArgumentException("Base Enumeration is null"); }
            try
            {
                Regex r = new Regex(identifier, RegexOptions.IgnoreCase);
                Regex removeToDot = new Regex(".*\\.");
                foreach (ManagementPackEnumeration e in etype.ManagementGroup.EntityTypes.GetChildEnumerations(etype.Id, TraversalDepth.Recursive))
                {
                    if (String.Compare(identifier, e.Id.ToString(), true) == 0) { return e; }
                    if (String.Compare(identifier, e.Name, true) == 0) { return e; }
                    if (String.Compare(identifier, removeToDot.Replace(e.Name, ""), true) == 0) { return e; }
                    if (String.Compare(identifier, e.DisplayName, true) == 0) { return e; }
                    if (r.Match(e.Name).Success == true) { return e; }
                    if (r.Match(e.DisplayName).Success == true) { return e; }
                }
            }
            catch
            {
                ;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Could not find '" + identifier + "' in enumeration '");
            sb.Append(etype.DisplayName);
            sb.Append("'. Allowed values are:\n");
            foreach (ManagementPackEnumeration e in etype.ManagementGroup.EntityTypes.GetChildEnumerations(etype.Id, TraversalDepth.Recursive))
            {
                sb.Append("\t '" + e.DisplayName + "' (" + e.Name + ")\n");
            }
            throw new ArgumentException(sb.ToString());
        }

        public static ManagementPackEnumeration GetEnum(string identifier, EnterpriseManagementGroup emg)
        {
            // This one is very strict - it will only match the name and really targeted at helper methods
            foreach (ManagementPackEnumeration e in emg.EntityTypes.GetEnumerations())
            {
                if (string.Compare(e.Name, identifier, true) == 0)
                {
                    return e;
                }
            }
            return null;
        }

        public static int DeterminePriority(ManagementPackEnumeration Urgency, ManagementPackEnumeration Impact, EnterpriseManagementGroup _mg)
        {
            // give it a good try, if something goes wrong, just return 5
            try
            {
                EnterpriseManagementGroup emg = Urgency.ManagementGroup;
                ManagementPackClass settingClass = SMHelpers.GetManagementPackClass(ClassTypes.System_WorkItem_Incident_GeneralSetting, SMHelpers.GetManagementPack(ManagementPacks.ServiceManager_IncidentManagement_Library, _mg), _mg);
                EnterpriseManagementObject settings = emg.EntityObjects.GetObject<EnterpriseManagementObject>(settingClass.Id, ObjectQueryOptions.Default);
                XmlDocument x = new XmlDocument();
                string xmlstring = settings[null, "PriorityMatrix"].Value as string;

                x.LoadXml(xmlstring);
                string u = Urgency.Id.ToString();
                string i = Impact.Id.ToString();
                string xpath = String.Format("/Matrix/U[@Id='{0}']/I[@Id='{1}']/P", u, i);
                XmlNode s = x.SelectSingleNode(xpath);
                string ts = s.InnerText;
                return Int32.Parse(ts);

            }
            catch
            {
                // Try to do something sensible, compute some sensible defaults
                try
                {
                    Dictionary<string,int> matrix = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
                    matrix.Add("Low",1);
                    matrix.Add("Medium",2);
                    matrix.Add("High",3);
                    int v = (3 * (matrix[Urgency.DisplayName] - 1)) + matrix[Impact.DisplayName];
                    return v;
                }
                catch // nevermind, return 5
                {
                    return 5;
                }
            }
        }

        public static void UpdateIncident(EnterpriseManagementGroup emg, ManagementPackClass clsIncident, EnterpriseManagementObjectProjection emop,
            string impact, string urgency, string status, string classification, string source, string supportGroup, string comment, string userComment, string description, string attachmentPath)
        {
            FileStream item = null;

            try
            {

                // get the BaseEnums, these are used later!
                ManagementPackEnumeration impactBase = SMHelpers.GetEnum("System.WorkItem.TroubleTicket.ImpactEnum", emg);
                ManagementPackEnumeration urgencyBase = SMHelpers.GetEnum("System.WorkItem.TroubleTicket.UrgencyEnum", emg);
                ManagementPackEnumeration statusBase = SMHelpers.GetEnum("IncidentStatusEnum", emg);
                ManagementPackEnumeration classificationBase = SMHelpers.GetEnum("IncidentClassificationEnum", emg);
                ManagementPackEnumeration sourceBase = SMHelpers.GetEnum("IncidentSourceEnum", emg);
                ManagementPackEnumeration supportGroupBase = SMHelpers.GetEnum("IncidentTierQueuesEnum", emg);

                ManagementPackEnumeration impactEnum = null;
                ManagementPackEnumeration urgencyEnum = null;
                ManagementPackEnumeration statusEnum = null;
                ManagementPackEnumeration classificationEnum = null;
                ManagementPackEnumeration sourceEnum = null;
                ManagementPackEnumeration supportGroupEnum = null;

                //If impact supplies, update the current value
                if (impact != null)
                {
                    impactEnum = SMHelpers.GetEnum(impact, impactBase);
                    emop.Object[clsIncident, "Impact"].Value = impactEnum.Id;
                }

                //If value supplied, update the current value
                if (urgency != null)
                {
                    urgencyEnum = SMHelpers.GetEnum(urgency, urgencyBase);
                    emop.Object[clsIncident, "Urgency"].Value = urgencyEnum.Id;
                }

                //If value supplied, update the current value
                if (status != null)
                {
                    statusEnum = SMHelpers.GetEnum(status, statusBase);
                    emop.Object[clsIncident, "Status"].Value = statusEnum.Id;
                    if (String.Compare(status, "Closed", true) == 0)
                    {
                        emop.Object[clsIncident, "ClosedDate"].Value = DateTime.Now.ToUniversalTime();
                    }
                    else if (String.Compare(status, "Resolved") == 0)
                    {
                        emop.Object[clsIncident, "ResolvedDate"].Value = DateTime.Now.ToUniversalTime();
                    }
                }

                //If source supplied, update the current value
                if (source != null)
                {
                    sourceEnum = SMHelpers.GetEnum(source, sourceBase);
                    emop.Object[clsIncident, "Source"].Value = sourceEnum.Id;
                }

                //If classification supplied, update the current value
                if (classification != null)
                {
                    classificationEnum = SMHelpers.GetEnum(classification, classificationBase);
                    emop.Object[clsIncident, "Classification"].Value = classificationEnum.Id;
                }

                if (supportGroup != null)
                {
                    supportGroupEnum = SMHelpers.GetEnum(supportGroup, supportGroupBase);
                    emop.Object[clsIncident, "TierQueue"].Value = supportGroupEnum.Id;
                }

                //Description supplied, update the current value
                if (description != null)
                {
                    emop.Object[clsIncident, "Description"].Value = description;
                }

                //If comment supplied, update the current value
                if (comment != null)
                {
                    ManagementPackClass analystCommentClass = GetManagementPackClass(ClassTypes.System_WorkItem_TroubleTicket_AnalystCommentLog, GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    CreatableEnterpriseManagementObject analystComment = new CreatableEnterpriseManagementObject(emg, analystCommentClass);
                    analystComment[analystCommentClass, "Id"].Value = Guid.NewGuid().ToString();// "comment-" + DateTime.Now.ToString(); //Had to change from date time dependent since the speed was an issues
                    analystComment[null, "Comment"].Value = comment;
                    analystComment[null, "EnteredBy"].Value = EnterpriseManagementGroup.CurrentUserName;
                    analystComment[null, "EnteredDate"].Value = DateTime.Now.ToUniversalTime();
                    ManagementPackRelationship incidentEmbedsAnalystComment = SMHelpers.GetManagementPackRelationship(RelationshipTypes.System_WorkItem_TroubleTicketHasAnalystComment, GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    emop.Add(analystComment, incidentEmbedsAnalystComment.Target);
                }

                //If comment supplied, update the current value
                if (userComment != null)
                {
                    ManagementPackClass userCommentClass = GetManagementPackClass(ClassTypes.System_WorkItem_TroubleTicket_UserCommentLog, GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    CreatableEnterpriseManagementObject userCommentObject = new CreatableEnterpriseManagementObject(emg, userCommentClass);
                    userCommentObject[userCommentClass, "Id"].Value = Guid.NewGuid().ToString();// "comment-" + DateTime.Now.ToString(); //Had to change from date time dependent since the speed was an issues
                    userCommentObject[null, "Comment"].Value = userComment;
                    userCommentObject[null, "EnteredBy"].Value = EnterpriseManagementGroup.CurrentUserName;
                    userCommentObject[null, "EnteredDate"].Value = DateTime.Now.ToUniversalTime();
                    ManagementPackRelationship incidentEmbedsUserComment = SMHelpers.GetManagementPackRelationship(RelationshipTypes.System_WorkItem_TroubleTicketHasUserComment, SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    emop.Add(userCommentObject, incidentEmbedsUserComment.Target);
                }

                //If file path for attachment supplied, attach file
                if (attachmentPath != null)
                {
                    ManagementPackClass fileAttachmentClass = GetManagementPackClass(ClassTypes.System_FileAttachment, GetManagementPack(ManagementPacks.System_SupportingItem_Library, emg), emg);
                    CreatableEnterpriseManagementObject fileAttachment = new CreatableEnterpriseManagementObject(emg, fileAttachmentClass);

                    item = new FileStream(attachmentPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    string fileName = Path.GetFileName(attachmentPath);

                    fileAttachment[fileAttachmentClass, "Id"].Value = Guid.NewGuid().ToString();
                    fileAttachment[fileAttachmentClass, "DisplayName"].Value = fileName;
                    fileAttachment[fileAttachmentClass, "Description"].Value = fileName;
                    fileAttachment[fileAttachmentClass, "Extension"].Value = Path.GetExtension(attachmentPath);
                    fileAttachment[fileAttachmentClass, "Size"].Value = (int)item.Length;
                    fileAttachment[fileAttachmentClass, "AddedDate"].Value = DateTime.Now.ToUniversalTime();
                    fileAttachment[fileAttachmentClass, "Content"].Value = item;

                    ManagementPackRelationship workItemHasFileAttachment = SMHelpers.GetManagementPackRelationship(
                        RelationshipTypes.System_WorkItemHasFileAttachment, 
                        SMHelpers.GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    emop.Add(fileAttachment, workItemHasFileAttachment.Target);

                    //File add Action log
                    ManagementPackClass logCommentClass = GetManagementPackClass(
                        ClassTypes.System_WorkItem_TroubleTicket_ActionLog, GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);
                    CreatableEnterpriseManagementObject logComment = new CreatableEnterpriseManagementObject(emg, logCommentClass);

                    logComment[logCommentClass, "Id"].Value = Guid.NewGuid().ToString();
                    logComment[logCommentClass, "Description"].Value = fileName;
                    logComment[logCommentClass, "EnteredDate"].Value = DateTime.Now.ToUniversalTime();

                    ManagementPackEnumeration enumeration = GetEnum(Enumerations.SystemWorkItemActionLogEnumFileAttached, emg);

                    logComment[logCommentClass, "ActionType"].Value = enumeration;
                    logComment[logCommentClass, "EnteredBy"].Value = Environment.UserName;
                    logComment[logCommentClass, "Title"].Value = enumeration.DisplayName;

                    ManagementPackRelationship fileAttachmentComment = SMHelpers.GetManagementPackRelationship(
                        RelationshipTypes.System_WorkItem_TroubleTicketHasActionLog,
                        GetManagementPack(ManagementPacks.System_WorkItem_Library, emg), emg);

                    emop.Add(logComment, fileAttachmentComment.Target);
                }

                if (urgencyEnum != null && impactEnum != null)
                {
                    int priority = SMHelpers.DeterminePriority(urgencyEnum, impactEnum, emg);
                    emop.Object[clsIncident, "Priority"].Value = priority;
                }

                //Commit all changes
                emop.Commit();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (item != null)
                {
                    item.Close();
                    item.Dispose();
                }
            }
        }

        public static void AddAffectedCI(EnterpriseManagementObjectProjection projection, EnterpriseManagementObject affectedCI,
            EnterpriseManagementGroup mg)
        {
            //Set the created by user to the user who used the web form.
            ManagementPackRelationship affectedItem = SMHelpers.GetManagementPackRelationship(RelationshipTypes.System_WorkItemAboutConfigItem, GetManagementPack(ManagementPacks.System_WorkItem_Library, mg), mg);
            projection.Add(affectedCI, affectedItem.Target);
        }

        public static void AddAffectedUser(EnterpriseManagementObjectProjection projection, string userIdentifier,
            EnterpriseManagementGroup mg)
        {
            EnterpriseManagementObject user = GetUser(mg, userIdentifier);

            //Set the created by user to the user who used the web form.
            ManagementPackRelationship affectedUser = SMHelpers.GetManagementPackRelationship(RelationshipTypes.System_WorkItemAffectedUser, GetManagementPack(ManagementPacks.System_WorkItem_Library, mg), mg);
            projection.Add(user, affectedUser.Target);
        }

        public static EnterpriseManagementObject GetUser(EnterpriseManagementGroup mg, string userIdentifier)
        {
            ManagementPack mpWindows = mg.ManagementPacks.GetManagementPack(SystemManagementPack.Windows);
            ManagementPackClass userClass = GetManagementPackClass(ClassTypes.Microsoft_AD_User, GetManagementPack(ManagementPacks.Microsoft_Windows_Library, mg), mg);

            //Create the criteria XML and criteria object
            string userCriteria = CreateUserCriteriaXml(mg, userIdentifier);
            EnterpriseManagementObjectCriteria criteria = new EnterpriseManagementObjectCriteria(userCriteria, userClass, mpWindows, mg);

            //Retrieve the user that corresponds to the criteria
            IEnumerable<EnterpriseManagementObject> users =
               (IEnumerable<EnterpriseManagementObject>)mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(criteria, ObjectQueryOptions.Default);

            //Get the enumerator
            IEnumerator<EnterpriseManagementObject> enumUsers = users.GetEnumerator();
            while (enumUsers.MoveNext())
            {
                return enumUsers.Current;
            }

            //If no user was found, throw an exception
            throw new Exception("No user with user identified by: " + userIdentifier + " found in Service Manager");
        }

        public static string CreateUserCriteriaXml(EnterpriseManagementGroup mg, string userIdentifier)
        {
            ManagementPack mpWindows = mg.ManagementPacks.GetManagementPack(SystemManagementPack.Windows);

            string userCriteria = string.Empty;
            // Check the format of the userName to make sure we create the correct filter
            if (userIdentifier.StartsWith("CN="))
            {
                // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.                  
                userCriteria = String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                   <Expression>
                    <SimpleExpression>
                      <ValueExpressionLeft>
                        <Property>$Target/Property[Type='Microsoft.AD.User']/DistinguishedName$</Property>
                      </ValueExpressionLeft>
                      <Operator>Equal</Operator>
                      <ValueExpressionRight>
                        <Value>{0}</Value>
                      </ValueExpressionRight>
                    </SimpleExpression>
                  </Expression>
                </Criteria>
                ", userIdentifier);
            }
            else if (userIdentifier.StartsWith("S-1-5"))
            {
                // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.                  
                userCriteria = String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                   <Expression>
                    <SimpleExpression>
                      <ValueExpressionLeft>
                        <Property>$Target/Property[Type='Microsoft.AD.User']/SID$</Property>
                      </ValueExpressionLeft>
                      <Operator>Equal</Operator>
                      <ValueExpressionRight>
                        <Value>{0}</Value>
                      </ValueExpressionRight>
                    </SimpleExpression>
                  </Expression>
                </Criteria>
                ", userIdentifier);
            }
            else
            {
                string[] userData = userIdentifier.Split('\\');
                // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.                  
                userCriteria = String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                   <Expression>
                    <And>
                    <Expression>
                    <SimpleExpression>
                      <ValueExpressionLeft>
                        <Property>$Target/Property[Type='Microsoft.AD.User']/UserName$</Property>
                      </ValueExpressionLeft>
                      <Operator>Equal</Operator>
                      <ValueExpressionRight>
                        <Value>{1}</Value>
                      </ValueExpressionRight>
                    </SimpleExpression>
                    </Expression>
                    <Expression>
                    <SimpleExpression>
                      <ValueExpressionLeft>
                        <Property>$Target/Property[Type='Microsoft.AD.User']/Domain$</Property>
                      </ValueExpressionLeft>
                      <Operator>Equal</Operator>
                      <ValueExpressionRight>
                        <Value>{0}</Value>
                      </ValueExpressionRight>
                    </SimpleExpression>
                    </Expression>
                    </And>
                  </Expression>
                </Criteria>
                ", userData[0], userData[1]);
            }

            return userCriteria;
        }

        public static string MakeMPElementSafeUniqueIdentifier(string strElementType)
        {
            string[] strDisallowedCharacters = new string[]{"!","@","#","$","%","^","&","*","(",")","_","-","+","=","{","}","[","]","|","\\",":","?","/","<",">",".","~",",","'","\""," "};
            string strSafeUniqueIdentifier = String.Format("{0}.{1}", strElementType, Guid.NewGuid().ToString("N"));
            foreach(string strDisallowedCharacter in strDisallowedCharacters)
            {
                strSafeUniqueIdentifier = strSafeUniqueIdentifier.Replace(strDisallowedCharacter, "");
            }
            return (strSafeUniqueIdentifier);
        }

        public static ManagementPack GetManagementPack(string id, EnterpriseManagementGroup emg)
        {
            ManagementPackCriteria mpcriteria = new ManagementPackCriteria(String.Format("Name = '{0}'", id));
            IList<ManagementPack> mps  = emg.ManagementPacks.GetManagementPacks(mpcriteria);
            //assuming there is only one - return the first one regardless
            if (mps.Count > 0)
                return (mps[0]);
            else
                return null;
        }

        public static ManagementPackRelationship GetManagementPackRelationship(string id, ManagementPack mp)
        {
            ManagementPackRelationshipCriteria mprcriteria = new ManagementPackRelationshipCriteria(String.Format("Name = '{0}'", id));
#if ( _SERVICEMANAGER_R2_ )
            IList<ManagementPackRelationship> mprelationships = mp.Store.EntityTypes.GetRelationshipClasses(mprcriteria);
#else
            MethodInfo method = mp.GetType().GetMethod("GetManagementGroupObject",BindingFlags.NonPublic|BindingFlags.Instance);
            EnterpriseManagementGroup emg = method.Invoke(mp,null) as EnterpriseManagementGroup;
            IList<ManagementPackRelationship> mprelationships = emg.EntityTypes.GetRelationshipClasses(mprcriteria);
#endif
            foreach (ManagementPackRelationship mprelationship in mprelationships)
            {
                if (mprelationship.GetManagementPack().Id == mp.Id)
                    return mprelationship;
            }
            //Didnt find any matches
            return (null);
        }

        public static ManagementPackRelationship GetManagementPackRelationship(string id, ManagementPack mp, EnterpriseManagementGroup emg)
        {
            ManagementPackRelationshipCriteria mprcriteria = new ManagementPackRelationshipCriteria(String.Format("Name = '{0}'", id));
            IList<ManagementPackRelationship> mprelationships = emg.EntityTypes.GetRelationshipClasses(mprcriteria);
            foreach (ManagementPackRelationship mprelationship in mprelationships)
            {
                if (mprelationship.GetManagementPack().Id == mp.Id)
                    return mprelationship;
            }
            //Didnt find any matches
            return (null);
        }

        public static ManagementPackClass GetManagementPackClass(string id, ManagementPack mp)
        {
            ManagementPackClassCriteria mpccriteria = new ManagementPackClassCriteria(String.Format("Name = '{0}'", id));
#if ( _SERVICEMANAGER_R2_ )
            IList<ManagementPackClass> mpclasses = mp.Store.EntityTypes.GetClasses(mpccriteria);
#else
            MethodInfo method = mp.GetType().GetMethod("GetManagementGroupObject",BindingFlags.NonPublic|BindingFlags.Instance);
            EnterpriseManagementGroup emg = method.Invoke(mp,null) as EnterpriseManagementGroup;
            IList<ManagementPackClass> mpclasses = emg.EntityTypes.GetClasses(mpccriteria);
#endif
            foreach (ManagementPackClass mpclass in mpclasses)
            {
                if (mpclass.GetManagementPack().Id == mp.Id)
                    return (mpclass);
            }
            //Didn't find any matches
            return (null);
        }

        public static ManagementPackClass GetManagementPackClass(string id, ManagementPack mp, EnterpriseManagementGroup emg)
        {
            ManagementPackClassCriteria mpccriteria = new ManagementPackClassCriteria(String.Format("Name = '{0}'", id));
            IList<ManagementPackClass> mpclasses = emg.EntityTypes.GetClasses(mpccriteria);
            foreach (ManagementPackClass mpclass in mpclasses)
            {
                if(mpclass.GetManagementPack().Id == mp.Id)
                    return(mpclass);
            }
            //Didn't find any matches
            return (null);
        }

        public static ManagementPackTypeProjection GetManagementPackTypeProjection(string id, ManagementPack mp)
        {
            ManagementPackTypeProjectionCriteria mptpcriteria = new ManagementPackTypeProjectionCriteria(String.Format("Name = '{0}'", id));
#if ( _SERVICEMANAGER_R2_ )
            IList<ManagementPackTypeProjection> mptps = mp.Store.EntityTypes.GetTypeProjections(mptpcriteria);
#else
            MethodInfo method = mp.GetType().GetMethod("GetManagementGroupObject",BindingFlags.NonPublic|BindingFlags.Instance);
            EnterpriseManagementGroup emg = method.Invoke(mp,null) as EnterpriseManagementGroup;
            IList<ManagementPackTypeProjection> mptps = emg.EntityTypes.GetTypeProjections(mptpcriteria);
#endif
            foreach (ManagementPackTypeProjection mptp in mptps)
            {
                if (mptp.GetManagementPack().Id == mp.Id)
                    return (mptp);
            }
            //Didn't find any matches
            return (null);
        }

        public static ManagementPackTypeProjection GetManagementPackTypeProjection(string id, ManagementPack mp, EnterpriseManagementGroup emg)
        {
            ManagementPackTypeProjectionCriteria mptpcriteria = new ManagementPackTypeProjectionCriteria(String.Format("Name = '{0}'", id));
            IList<ManagementPackTypeProjection> mptps = emg.EntityTypes.GetTypeProjections(mptpcriteria);
            foreach (ManagementPackTypeProjection mptp in mptps)
            {
                if (mptp.GetManagementPack().Id == mp.Id)
                    return (mptp);
            }
            //Didn't find any matches
            return (null);
        }

        public static ManagementPackObjectTemplate GetObjectTemplateFromRequestOffering(EnterpriseManagementObjectProjection requestOffering)
        {
            string TemplateIdentifier = requestOffering.Object[null, "TargetTemplate"].Value.ToString();
            string TemplateName = TemplateIdentifier.Split('|')[3];
            ManagementPackObjectTemplateCriteria c = new ManagementPackObjectTemplateCriteria("Name = '" + TemplateName + "'");
            return requestOffering.Object.ManagementGroup.Templates.GetObjectTemplates(c)[0];
        }
    }
}
