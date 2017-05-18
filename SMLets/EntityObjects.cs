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
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using Microsoft.EnterpriseManagement.ConnectorFramework;
using System.Text.RegularExpressions;

namespace SMLets
{

public class ObjectCmdletHelper : SMCmdletBase
{
    // For the Object cmdlets, they all pretty much can use ObjectQueryOptions, so
        // we'll just make it part of the base class
        private ObjectQueryOptions _queryOption = ObjectQueryOptions.Default;
        [Parameter]
        public ObjectQueryOptions QueryOption
        {
            get { return _queryOption; }
            set { _queryOption = value; }
        }

        public void AssignNewValues(EnterpriseManagementObject o, Hashtable ht)
        {
            foreach(string s in ht.Keys)
            {
                bool found = false;
                WriteDebug("Attempting to find property " + s);
                foreach(ManagementPackProperty p in o.GetProperties())
                {
                    int CompareResult = String.Compare(p.Name,s,ic);
                    WriteDebug("PROPERTY " + p.Name + " == " + s + " Result: " + CompareResult);
                    if ( CompareResult == 0 )
                    {
                        found = true;
                        WriteDebug("Assigning " + ht[s] + " to " + p.Name ); 
                        try
                        {
                            AssignNewValue(p, o[p], ht[s]);
                        }
                        catch (Exception e )
                        {
                            string errmsg = "Assigning " + ht[s] + " to " + p.Name;
                            WriteError(new ErrorRecord(e, errmsg, ErrorCategory.InvalidOperation, ht[s]));
                        }
                        break;
                    }
                }
                if ( ! found )
                {
                    WriteDebug("Could not find property " + s + " on object");
                }
            }
        }

        // in PowerShell, string comparisons are done with case ignored
        public StringComparison ic = StringComparison.OrdinalIgnoreCase;
        // Assign a value to a managementpack property
        // This code is clever enough to handle nearly all of the types that 
        // a management pack property can be
        // if you attempt to assign something which is an enum type
        // we'll go looking for the appropriate enumeration and use that
        // TODO: handle the binary property type (it should take a stream)
        public void AssignNewValue(ManagementPackProperty p, EnterpriseManagementSimpleObject so, object newValue)
        {
            string PropertyType = p.Type.ToString();
            WriteVerbose("Want to set " + p.Name + "(" + PropertyType + ") to " + newValue);
            // so, if new value is null, set and return immediately
            if (newValue == null)
            {
                try
                {
                    so.Value = null;
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, "Could not assign " + p.Name + " to null", ErrorCategory.InvalidOperation, newValue));
                }
                return;
            }
            switch (PropertyType)
            {
                case "richtext" :
                case "string" :
                    try
                    {
                        so.Value = newValue;
                    }
                    catch (InvalidSimpleObjectValueException)
                    {
                        WriteWarning("Converting new value ('" + newValue.GetType().ToString() + ":" + newValue.ToString() + "') to string");
                        so.Value = newValue.ToString();
                    }
                    break;
                case "double":
                case "int":
                case "decimal":
                case "bool":
                    so.Value = newValue;
                    break;
                case "guid":
                    // This should be done in a try/catch
                    // otherwise it will fail poorly
                    try
                    {
                        so.Value = new Guid(newValue.ToString());
                    }
                    catch ( Exception e )
                    {
                        WriteError(new ErrorRecord(e, "Could not assign guid", ErrorCategory.InvalidOperation, newValue));
                    }
                    break;
                case "enum":
                    WriteDebug("Looking for Enumeration: " + newValue);
                    ManagementPackEnumeration mpe;
                    // We might have gotten an enum, try the assignment
                    if ( newValue is ManagementPackEnumeration )
                    {
                        WriteVerbose("Actually an enumeration");
                        try
                        {
                            mpe = (ManagementPackEnumeration)newValue;
                            so.Value = mpe;
                        }
                        catch ( Exception e )
                        {
                            WriteError(new ErrorRecord(e, "Could not assign enum", ErrorCategory.InvalidOperation, newValue));
                        }
                    }
                    else
                    {
                        WriteVerbose("Looking Enum via string " + newValue.ToString() + " in " + p.EnumType.GetElement());
                        try
                        {
                            mpe = SMHelpers.GetEnum(newValue.ToString(), p.EnumType.GetElement());
                            WriteVerbose("found enum: " + mpe.ToString());
                            so.Value = mpe;
                            WriteVerbose("set the value");
                        }
                        catch ( Exception e )
                        {
                            WriteError(new ErrorRecord(e, "Could not assign enum ", ErrorCategory.ObjectNotFound, newValue));
                        }
                    }
                    break;
                case "datetime":
                    // TODO: handle failure gracefully
                    try
                    {
                        //AG: we no need to parse string if newValue is already DateTime
                        if (newValue is DateTime)
                        {
                            so.Value = newValue;
                        }
                        else
                        {
                            // AG: what reason to convert DateTime to string?
                            //so.Value = DateTime.Parse(newValue.ToString(), CultureInfo.CurrentCulture).ToString();
                            so.Value = DateTime.Parse(newValue.ToString(), CultureInfo.CurrentCulture);
                        }
                    }
                    catch ( Exception e )
                    {
                        WriteError(new ErrorRecord(e, "Could not assign date ", ErrorCategory.ObjectNotFound, newValue));
                    }
                    break;
                case "binary":
                    // TODO: HANDLE filename
                    FileStream myStream = null;
                    // Deal with the case that we got a PSObject
                    if ( newValue is PSObject )
                    {
                        myStream = ((PSObject)newValue).BaseObject as FileStream;
                    }
                    else
                    {
                        // see if we get lucky
                        myStream = newValue as FileStream;
                    }
                    if (myStream != null)
                    {
                        so.Value = myStream;
                    }
                    else 
                    {
                        WriteError(new ErrorRecord(new ArgumentException(newValue.ToString()),
                           String.Format("Property must be a file stream, received a {0}", newValue.GetType().FullName), ErrorCategory.InvalidArgument, newValue));
                    }
                    break;
                default:
                    WriteVerbose("Could not find type setter for " + PropertyType);
                    WriteError(new ErrorRecord(new ItemNotFoundException("PropertySetterNotFound"), "No such property setter", ErrorCategory.ObjectNotFound, PropertyType));
                    break;
            }
            
        }

        // All this does is convert the PowerShell filter language to the criteria syntax
        public string ConvertFilterToGenericCriteria(string filter)
        {
            Dictionary<string,string> OpToOp = new Dictionary<string,string>();
            Regex re;
            // "-gt","-ge","-lt","-le","-eq","-ne","-like","-notlike","-match","-notmatch"
            // Add -isnull and -isnotnull, even though they aren't PowerShell operators
            OpToOp.Add("-and", "and");
            OpToOp.Add("-or", "or");
            OpToOp.Add("-eq","=");
            OpToOp.Add("-ne","!=");
            OpToOp.Add("-lt","<");
            OpToOp.Add("-gt",">");
            OpToOp.Add("-le","<=");
            OpToOp.Add("-ge",">=");
            OpToOp.Add("-like","like");
            OpToOp.Add("-notlike","! like");
            OpToOp.Add("-isnull", "is null");
            OpToOp.Add("-isnotnull", "is not null");
            re = new Regex("\\*");
            filter = re.Replace(filter, "%");
            re = new Regex("\\?");
            filter = re.Replace(filter,"_");
            re = new Regex("\"");
            filter = re.Replace(filter, "'");
            foreach(string k in OpToOp.Keys)
            {
                re = new Regex(k, RegexOptions.CultureInvariant|RegexOptions.IgnoreCase);
                filter = re.Replace(filter, OpToOp[k]);
            }
            return filter;
        }
        public string FixUpPropertyNames(string filter, ManagementPackClass mpClass)
        {
            Dictionary<string,string> propertyFixes = new Dictionary<string,string>();
            Regex re;
            foreach (ManagementPackProperty p in mpClass.GetProperties(BaseClassTraversalDepth.Recursive)) 
            {
                re = new Regex(p.Name, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                filter = re.Replace(filter, p.Name);
            }
            WriteDebug("returning: " + filter);
            return filter;
        }

        public string ConvertFilterToCriteriaString( ManagementPackClass mpClass, string filter, bool isProjection )
        {
            WriteVerbose("ConvertFilterToCriteriaString: " + filter);
            StringBuilder sb = new StringBuilder();
            List<PropertyOperatorValue> POVs = new List<PropertyOperatorValue>();
            foreach (string subFilter in Regex.Split(filter, "-or", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                WriteVerbose("SubFilter => " + subFilter);
                try
                {
                    POVs.Add(new PropertyOperatorValue(subFilter));
                }
                catch
                {
                    WriteError(new ErrorRecord(new ObjectNotFoundException("criteria"), "Bad Filter", ErrorCategory.NotSpecified, filter));
                    return null;
                }
            }

            // Construct the Criteria XML
            // This should really be done with an XML DOM methods
            sb.Append("<Criteria xmlns='http://Microsoft.EnterpriseManagement.Core.Criteria/'>");
            sb.Append("<Reference Id='");
            sb.Append(mpClass.GetManagementPack().Name);
            sb.Append("' Version='");
            sb.Append(mpClass.GetManagementPack().Version.ToString());
            sb.Append("'");
            if ( mpClass.GetManagementPack().KeyToken != null)
            {
                sb.Append(" PublicKeyToken='");
                sb.Append(mpClass.GetManagementPack().KeyToken);
                sb.Append("'");
            }
            sb.Append(" Alias='myMP' />");
            // JWT START OF EXPRESSION
            // CHECK FOR AND/OR HERE
            if (POVs.Count > 1) 
            { 
                sb.Append("<Expression>");
                sb.Append("<Or>");
            }
            foreach (PropertyOperatorValue POV in POVs)
            {

                sb.Append("<Expression>");
                sb.Append("<SimpleExpression>");
                // check to be sure the property exists on the class
                // do this with a creatable EMO as *all* the properties are presented
                // If the class is abstract, you can't create it. This means you have 
                // to use the properties on the class as you get it.
                List<ManagementPackProperty> proplist = new List<ManagementPackProperty>();
                if (mpClass.Abstract)
                {
                    foreach (ManagementPackProperty p in mpClass.PropertyCollection)
                    {
                        proplist.Add(p);
                    }
                }
                else
                {
                    // The proper way to get at the properties of a class
                    foreach (ManagementPackProperty p in mpClass.GetProperties(BaseClassTraversalDepth.Recursive, PropertyExtensionMode.All))
                    {
                        proplist.Add(p);
                    }
                }

                bool foundproperty = false;
                foreach (ManagementPackProperty p in proplist)
                {
                    if (String.Compare(POV.Property, p.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        sb.Append("<ValueExpressionLeft>");
                        if (isProjection)
                        {
                            // projection not quite supported
                            sb.Append("<Property>$Target/Property[Type='myMP!" + mpClass.Name + "']/" + p.Name + "$</Property>");
                        }
                        else
                        {
                            sb.Append("<Property>$Target/Property[Type='myMP!" + mpClass.Name + "']/" + p.Name + "$</Property>");
                        }

                        sb.Append("</ValueExpressionLeft>");
                        foundproperty = true;
                        break;
                    }
                }
                // perhaps the provided property is a generic property
                if (!foundproperty)
                {
                    foreach (GenericProperty p in GenericProperty.GetGenericProperties())
                    {
                        if (String.Compare(POV.Property, p.PropertyName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            sb.Append("<ValueExpressionLeft>");
                            // sb.Append("<GenericProperty>$Target/Property[Type='myMP!" + mpClass.Name + "']/" + p.PropertyName + "$</GenericProperty>");
                            sb.Append("<GenericProperty>" + p.PropertyName + "</GenericProperty>");
                            sb.Append("</ValueExpressionLeft>");
                            foundproperty = true;
                        }
                    }
                }
                if (!foundproperty)
                {
                    WriteError(new ErrorRecord(new ObjectNotFoundException("property"), "Property Not Found", ErrorCategory.NotSpecified, filter));
                    return null;
                }

                // Now add the operator
                sb.Append("<Operator>" + POV.Operator + "</Operator>");
                // Finally, the value - no checking here, just add it
                sb.Append("<ValueExpressionRight><Value>");
                // TODO: HANDLE ENUMS
                sb.Append(POV.Value);
                sb.Append("</Value></ValueExpressionRight>");
                sb.Append("</SimpleExpression>");
                sb.Append("</Expression>");
            }
            if (POVs.Count > 1) { sb.Append("</Or>"); sb.Append("</Expression>"); }
            // JWT END OF EXPRESSION
            sb.Append("</Criteria>");
            WriteVerbose(sb.ToString());
            return sb.ToString();
        }

    // This doesn't completely work yet
    // TODO: handle generic property queries
    // I think it will look like:
    // PROPERTY OPERATOR VALUE
    // so, you could do:
    // DISPLAYNAME -EQ 'Boo ya!'
    // and you'll get the filter back
    // Also need to support Enumeration values, so user can provide a string rather than a guid for an enum
    // value
    public EnterpriseManagementObjectCriteria ConvertFilterToObjectCriteria ( ManagementPackClass mpClass, string filter)
    {
        EnterpriseManagementObjectCriteria myCriteria = null;
        string filterString = null;
        // First attempt to use the simple constructor for the ObjectCriteria, just the filter and the class
        // First replace all the PowerShell operators
        // this will return a criteria if we have success
        try
        {
            WriteVerbose("Original Filter: " + filter);
            filterString = ConvertFilterToGenericCriteria(filter);
            filterString = FixUpPropertyNames(filterString, mpClass);
            WriteVerbose("Fixed Filter: " + filterString);
            myCriteria = new EnterpriseManagementObjectCriteria(filterString, mpClass);
            WriteVerbose("Using " + filterString + " as criteria");
            return myCriteria;
        }
        catch // This is non-catastrophic - it's our first attempt
        {
            WriteDebug("failed: " + filter);
        }

        try
        {
            filterString = ConvertFilterToCriteriaString(mpClass, filter, false);
            myCriteria = new EnterpriseManagementObjectCriteria(filterString, mpClass, _mg);
        }
        catch (InvalidCriteriaException e)
        {
            ThrowTerminatingError(new ErrorRecord(e, "Bad Filter", ErrorCategory.InvalidOperation, filterString));
        }
        catch (Exception e)
        {
            ThrowTerminatingError(new ErrorRecord(e, "Bad Filter", ErrorCategory.InvalidOperation, filter));
        }
        return myCriteria;
    }
    public ObjectProjectionCriteria ConvertFilterToProjectionCriteria ( ManagementPackTypeProjection projection, string filter )
    {
        XmlDocument x = new XmlDocument();
        try
        {
            // if you can create an ObjectProjectionCriteria because you got some XML, groovy
            x.LoadXml(filter);
            // if we get to here, then we know we at least have some well formed XML
            // now try to create an ObjectProjectionCriteria
            WriteVerbose(filter);
            ObjectProjectionCriteria opc = new ObjectProjectionCriteria(filter, projection, _mg);
            return opc;
        }
        // don't do anything here, just keep going as an XML exception
        // means that we probably got a real filter rather than some XML Criteria
        catch (XmlException) { ; } 
        // OK, we got valid XML but a bad criteria, notify and bail
        catch (InvalidCriteriaException e)
        {
            ThrowTerminatingError( new ErrorRecord(e, "InvalidCriteria", ErrorCategory.InvalidData, filter));
        }

        string filterString = ConvertFilterToCriteriaString ( projection.TargetType, filter, true );
        ObjectProjectionCriteria myCriteria = new ObjectProjectionCriteria(filterString, projection, _mg);
        return myCriteria;
    }

}

public class FilterCmdletBase : ObjectCmdletHelper
    {
        protected string _filter = null;
        [Parameter]
        public string Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
        private Int32 _maxCount = Int32.MaxValue;
        [Parameter]
        public Int32 MaxCount
        {
            get { return _maxCount; }
            set { _maxCount = value; }
        }

        // By default, we'll collect the projections base on 
        // when they were created
        private string _sortBy = "TimeAdded";
        [Parameter]
        public string SortBy
        {
            get { return _sortBy; }
            set { _sortBy = value; }
        }
        internal const string xmlns = "xmlns=\"http://Microsoft.EnterpriseManagement.Core.Sorting\"";
        internal const string ascending = "Ascending";
        internal const string descending = "Descending";
        internal string Order = ascending;

        internal void addSortProperty(ObjectQueryOptions options, string sortProperty, ManagementPackClass c)
        {
            SortingOrder sOrder = SortingOrder.Ascending;
            if (sortProperty == null || c == null || options == null) { return; }
            if (sortProperty[0] == '-')
            {
                sOrder = SortingOrder.Descending;
                sortProperty = SortBy.Substring(1);
            }
            WriteVerbose("look for instance properties first");

            foreach (ManagementPackProperty mpp in c.GetProperties(BaseClassTraversalDepth.Recursive))
            {
                if (string.Compare(mpp.Name, sortProperty, true) == 0)
                {
                    options.AddSortProperty(mpp, sOrder);
                    return;
                }
            }

            WriteVerbose("now look for generic properties");

            foreach (GenericProperty gp in GenericProperty.GetGenericProperties())
            {
                if (string.Compare(gp.PropertyName, sortProperty, true) == 0)
                {
                    Type t = typeof(EnterpriseManagementObjectGenericPropertyName);
                    EnterpriseManagementObjectGenericPropertyName pn = (EnterpriseManagementObjectGenericPropertyName)Enum.Parse(t, gp.PropertyName);
                    EnterpriseManagementObjectGenericProperty gProperty = new EnterpriseManagementObjectGenericProperty(pn);
                    options.AddSortProperty(gProperty, sOrder);
                    return;
                }
            }

        }

        internal string makeSortCriteriaString(string sortProperty, ManagementPackClass c)
        {
            WriteDebug("makeSortCriteriaString with MP Class");
            Order = ascending;
            // Now that we have a projection, create the sort order
            if (sortProperty[0] == '-')
            {
                Order = descending;
                sortProperty = SortBy.Substring(1);
            }

            WriteVerbose("checking for targettype property");
            foreach (ManagementPackProperty mpp in c.GetProperties(BaseClassTraversalDepth.Recursive))
            {
                WriteVerbose("Checking TargetType properties: " + mpp.Name + " - " + sortProperty);
                if (string.Compare(mpp.Name, sortProperty, true) == 0)
                {
                    WriteVerbose("Sort Property: " + mpp.Name);
                    return String.Format(
                        "<Sorting {0}><SortProperty SortOrder=\"{1}\">$Target/Property[Type='{2}']/{3}$</SortProperty></Sorting>",
                        xmlns, Order, c.Id, mpp.Name
                        );
                }
            }

            // OK, we'll check targettype properties first
            foreach (GenericProperty gp in GenericProperty.GetGenericProperties())
            {
                if (string.Compare(gp.PropertyName, sortProperty, true) == 0)
                {
                    WriteVerbose("Sort property: " + gp.PropertyName);
                    return String.Format(
                        "<Sorting {0}><GenericSortProperty SortOrder=\"{1}\">{2}</GenericSortProperty></Sorting>",
                        xmlns, Order, gp.PropertyName
                        );
                }
            }

            return null;
        }

        internal string makeSortCriteriaString(string sortProperty, ManagementPackTypeProjection p)
        {
            WriteDebug("start makeSortCriteriaString");
            Order = ascending;
            // Now that we have a projection, create the sort order
            if (SortBy[0] == '-')
            {
                Order = descending;
                sortProperty = SortBy.Substring(1);
            }
            // OK, we'll check generic properties first
            WriteVerbose("makeSortCriteriaString");
            foreach (GenericProperty gp in GenericProperty.GetGenericProperties())
            {
                if (string.Compare(gp.PropertyName, sortProperty, true) == 0)
                {
                    WriteVerbose("Sort property: " + gp.PropertyName);
                    return String.Format(
                        "<Sorting {0}><GenericSortProperty SortOrder=\"{1}\">{2}</GenericSortProperty></Sorting>",
                        xmlns, Order, gp.PropertyName
                        );
                }
            }
            WriteVerbose("checking further");
            foreach (ManagementPackProperty mpp in p.TargetType.GetProperties(BaseClassTraversalDepth.Recursive))
            {
                WriteVerbose("Checking TargetType properties: " + mpp.Name + " - " + sortProperty);
                if (string.Compare(mpp.Name, sortProperty, true) == 0)
                {
                    WriteVerbose("Sort Property: " + mpp.Name);
                    return String.Format(
                        "<Sorting {0}><SortProperty SortOrder=\"{1}\">$Target/Property[Type='{2}']/{3}$</SortProperty></Sorting>",
                        xmlns, Order, p.TargetType.Id, mpp.Name
                        );
                }
            }
            return null;
        }

    }

public sealed class ServiceManagerObjectHelper
{
    // a helper class  to read a stream and adapt an EMO

    // We handle the retrival of binary types
    // if it's a binary property type, we'll return an array of
    // bytes
    private static Byte[] GetBytes(Stream s)
    {
        // Why SM binary fields don't have a valid length property is a mystery
        if ( s == null ) { return null; }
        byte[] buffer = new byte[4096];
        Collection<Byte[]> balist = new Collection<Byte[]>();
        int count;
        int totalLength = 0;
        while ( (count = s.Read(buffer,0,4096)) != 0 )
        {
            totalLength += count;
            byte[] tbyte = new byte[count];
            Array.Copy(buffer, tbyte, count);
            balist.Add(tbyte);
        }
        byte[] ReturnBytes = new byte[totalLength];
        int offset = 0;
        foreach(byte[]b in balist)
        {
            b.CopyTo(ReturnBytes,offset);
            offset += b.Length;
        }
        return ReturnBytes;
    }

    public static PSObject AdaptProjection(Cmdlet myCmdlet, EnterpriseManagementObjectProjection p, string projectionName)
    {
        myCmdlet.WriteVerbose("Adapting " + p);
        /*
         * We can't just wrap a type projection because it is Enumerable. This means that we would only see the
         * components of the projection in the output so we have to construct this artificial wrapper. It would be easier if
         * projections weren't Enumerable, which means that PowerShell wouldn't treat a projection as a collection, or if 
         * PowerShell understood that certain collections shouldn't be unspooled, but that's not how PowerShell works.
         * Neither of those two options are available, so we adapt the object and present a PSObject with all the component
         * parts.
         */
        PSObject o = new PSObject();
        o.Members.Add(new PSNoteProperty("__base", p));
        o.Members.Add(new PSScriptMethod("GetAsXml", ScriptBlock.Create("[xml]($this.__base.CreateNavigator().OuterXml)")));
        o.Members.Add(new PSNoteProperty("Object", AdaptManagementObject(myCmdlet, p.Object)));
        // Now promote all the properties on Object
        foreach (EnterpriseManagementSimpleObject so in p.Object.Values)
        {
            try
            {
                o.Members.Add(new PSNoteProperty(so.Type.Name, so.Value));
            }
            catch
            {
                myCmdlet.WriteWarning("could not promote: " + so.Type.Name);
            }
        }

        o.TypeNames[0] = String.Format(CultureInfo.CurrentCulture, "EnterpriseManagementObjectProjection#{0}", projectionName);
        o.TypeNames.Insert(1, "EnterpriseManagementObjectProjection");
        o.Members.Add(new PSNoteProperty("__ProjectionType", projectionName));

        foreach (KeyValuePair<ManagementPackRelationshipEndpoint, IComposableProjection> helper in p)
        {
            // EnterpriseManagementObject myEMO = (EnterpriseManagementObject)helper.Value.Object;
            myCmdlet.WriteVerbose("Adapting related objects: " + helper.Key.Name);
            String myName = helper.Key.Name;
            PSObject adaptedEMO = AdaptManagementObject(myCmdlet, helper.Value.Object);
            // If the MaxCardinality is greater than one, it's definitely a collection
            // so start out that way
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
                    myCmdlet.WriteVerbose("Readapting relationship object -> collection :" + e.Message);
                    // We should really only get this exception if we
                    // try to add a create a new property which already exists
                    Object currentPropertyValue = o.Properties[myName].Value;
                    ArrayList newValue = new ArrayList();
                    newValue.Add(currentPropertyValue);
                    newValue.Add(adaptedEMO);
                    o.Properties[myName].Value = newValue;
                    // TODO
                    // If this already exists, it should be converted to a collection
                }
            }
        }
        return o;
    }

    // Adapt the EnterpriseManagementObject
    // We need to do this because the interest bits of the EMO (from our perspective)
    // are in the values collection, we promote them to NoteProperties so displaying 
    // the contents work better. This should really be done with a Type adapter, but
    // this is ok.
    public static PSObject AdaptManagementObject(Cmdlet myCmdlet, EnterpriseManagementObject managementObject)
    {
        PSObject PromotedObject = new PSObject(managementObject);
        PromotedObject.TypeNames.Insert(1, managementObject.GetType().FullName);
        PromotedObject.TypeNames[0] = String.Format(CultureInfo.CurrentCulture, "EnterpriseManagementObject#{0}",managementObject.GetLeastDerivedNonAbstractClass().Name);
        // loop through the properties and promote them into the PSObject we're going to return
        foreach ( ManagementPackProperty p in managementObject.GetProperties())
        {
            try
            {
                if ( p.SystemType.ToString() == "System.IO.Stream" )
                {
                    if ( managementObject[p].Value != null )
                    {
                        PSObject StreamObject = new PSObject(managementObject[p].Value);
                        Byte[] Data = GetBytes(managementObject[p].Value as Stream);
                        StreamObject.Members.Add(new PSNoteProperty("Data", Data));
                        PromotedObject.Members.Add(new PSNoteProperty(p.Name, StreamObject));
                    }
                    else
                    {
                        PromotedObject.Members.Add(new PSNoteProperty(p.Name, new Byte[0]));
                    }
                }
                else
                {
                    PromotedObject.Members.Add(new PSNoteProperty(p.Name, managementObject[p].Value));
                }
            }
            catch ( ExtendedTypeSystemException ets)
            {
                myCmdlet.WriteWarning(String.Format("The property '{0}' already exists, skipping.\nException: {1}", p.Name,ets.Message));
            }
            catch ( Exception e )
            { 
                myCmdlet.WriteError(new ErrorRecord(e, "Property", ErrorCategory.NotSpecified, p.Name));
            }
        }

        PromotedObject.Members.Add(new PSNoteProperty("__InternalId", managementObject.Id));
        return PromotedObject;

    }

    // This overload is so we can call the adapter from the script layer 
    // where we may not have a cmdlet reference. In the case of errors, we'll 
    // just let it get thrown
    public static PSObject AdaptManagementObject(EnterpriseManagementObject managementObject)
    {
        PSObject PromotedObject = new PSObject(managementObject);
        PromotedObject.TypeNames.Insert(1, managementObject.GetType().FullName);
        PromotedObject.TypeNames[0] = String.Format(CultureInfo.CurrentCulture, "EnterpriseManagementObject#{0}",managementObject.GetLeastDerivedNonAbstractClass().Name);
        // loop through the properties and promote them into the PSObject we're going to return
        foreach ( ManagementPackProperty p in managementObject.GetProperties())
        {
            try
            {
                if ( p.SystemType.ToString() == "System.IO.Stream" )
                {
                    if ( managementObject[p].Value != null )
                    {
                        PSObject StreamObject = new PSObject(managementObject[p].Value);
                        Byte[] Data = GetBytes(managementObject[p].Value as Stream);
                        StreamObject.Members.Add(new PSNoteProperty("Data", Data));
                        PromotedObject.Members.Add(new PSNoteProperty(p.Name, StreamObject));
                    }
                    else
                    {
                        PromotedObject.Members.Add(new PSNoteProperty(p.Name, new Byte[0]));
                    }
                }
                else
                {
                    PromotedObject.Members.Add(new PSNoteProperty(p.Name, managementObject[p].Value));
                }
            }
            catch ( ExtendedTypeSystemException ets)
            {
                throw(new InvalidOperationException(String.Format("The property '{0}' already exists, skipping.\nException: {1}", p.Name,ets.Message)));
            }
            catch ( Exception e )
            { 
                throw(e);
            }
        }
        PromotedObject.Members.Add(new PSNoteProperty("__InternalId", managementObject.Id));
        return PromotedObject;

    }
    private ServiceManagerObjectHelper() { ; }
}

#region SCSMObject cmdlets

[Cmdlet(VerbsCommon.New, "SCSMObject", SupportsShouldProcess = true, DefaultParameterSetName = "name")]
public class NewSMObjectCommand : ObjectCmdletHelper
{
    private ManagementPackClass _class = null;
    [Parameter(Position = 0, Mandatory = true, ParameterSetName = "class")]
    public ManagementPackClass Class
    {
        get { return _class; }
        set { _class = value; }
    }

    private Hashtable _property;
    [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
    public Hashtable PropertyHashtable
    {
        get { return _property; }
        set { _property = value; }
    }

    private ManagementPackObjectTemplate _template = null;
    [Parameter]
    public ManagementPackObjectTemplate Template
    {
        get { return _template; }
        set { _template = value; }
    }

    private SwitchParameter _passthru;
    [Parameter]
    public SwitchParameter PassThru
    {
        get { return _passthru; }
        set { _passthru = value; }
    }

    // On NoCommit, don't commit, just return
    // the created object. This is needed for those
    // operations that require that an instance not
    // already exist
    private SwitchParameter _noCommit;
    [Parameter]
    public SwitchParameter NoCommit
    {
        get { return _noCommit; }
        set { _noCommit = value; }
    }

    private IncrementalDiscoveryData pendingChanges;
    private int batchSize = 200;
    private int toCommit = 0;
    private SwitchParameter _bulk;
    [Parameter]
    public SwitchParameter Bulk
    {
        get { return _bulk; }
        set { _bulk = value; }
    }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        if (Bulk) { pendingChanges = new IncrementalDiscoveryData(); }
        if (Class == null)
        {
            ThrowTerminatingError(new ErrorRecord(new ArgumentNullException("Class"), "Class Not Found", ErrorCategory.ObjectNotFound, "Class"));
        }
    }
    // We're going to call commit for each object passed to us
    // TODO: Create an array of objects and commit them in
    // one operation
    protected override void ProcessRecord()
    {
        // Create an object
        CreatableEnterpriseManagementObject o = new CreatableEnterpriseManagementObject(_mg, Class);
        // Apply the template if needed
        if (Template != null) { o.ApplyTemplate(Template); }
        // Just to make things easier to deal with, we'll create a hash table of 
        // the properties in the object.
        //
        // TODO: ADD GENERIC PROPERTIES
        // Create a hashtable of the properties, ignore case so if we find one
        // we can grab the property and assign the new value!
        Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase);
        foreach (ManagementPackProperty prop in o.GetProperties())
        {
            try
            {
            ht.Add(prop.Name, prop);
            }
            catch ( Exception e )
            {
                WriteError(new ErrorRecord(e, "property '" + prop.Name + "' has already been added to collection", ErrorCategory.InvalidOperation, prop));
            }
        }

        // now go through the hashtable that has the values we we want to use and
        // assigned them into the new values
        foreach (string s in PropertyHashtable.Keys)
        {
            if (!ht.ContainsKey(s))
            {
                WriteError(new ErrorRecord(new ObjectNotFoundException(s), "property not found on object", ErrorCategory.NotSpecified, o));
            }
            else
            {
                ManagementPackProperty p = ht[s] as ManagementPackProperty;
                AssignNewValue(p, o[p], PropertyHashtable[s]);
            }
        }

        // Now that we're done, we can commit it
        // TODO: if we get an exception indicating we're disconnected
        // Reconnect and try again.
        if (ShouldProcess(Class.Name))
        {
            try
            {
                if (Bulk)
                {
                    toCommit++;
                    pendingChanges.Add(o);
                    if (toCommit >= batchSize)
                    {
                        toCommit = 0;
                        pendingChanges.Commit(_mg);
                        pendingChanges = new IncrementalDiscoveryData();
                    }
                }
                else
                {
                    if (NoCommit)
                    {
                        WriteObject(o);
                    }
                    else
                    {
                        o.Commit();
                        // on PassThru get the ID and call GetObject
                        // we don't want to hand back the CreatableEnterpriseObject, but rather the thing
                        // that was saved.
                        if (_passthru)
                        {
                            WriteObject(
                                ServiceManagerObjectHelper.AdaptManagementObject(
                                 this, _mg.EntityObjects.GetObject<EnterpriseManagementObject>(o.Id, ObjectQueryOptions.Default)
                                 )
                             );
                        }
                    }

                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Could not save new object", ErrorCategory.InvalidOperation, o));
            }
        }
    }
    protected override void EndProcessing()
    {
        base.EndProcessing();
        if (Bulk)
        {
            try
            {
                pendingChanges.Commit(_mg);
            }
            catch (Exception e)
            {
                WriteWarning("Commit failed, trying Overwrite: " + e.Message);
                try
                {
                    pendingChanges.Overwrite(_mg);
                }
                catch (Exception x)
                {
                    WriteError(new ErrorRecord(x, "Could not save new object with commit or overwrite", ErrorCategory.InvalidOperation, pendingChanges));
                }
            }
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SCSMObject", DefaultParameterSetName = "Class")]
public class GetSMObjectCommand : FilterCmdletBase
{
    // Note: Four parameter sets so you can retrieve by class, guid or criteria

    private ManagementPackClass _class = null;
    [Parameter(ParameterSetName = "Class", Position = 0, Mandatory = true, ValueFromPipeline = true)]
    [Parameter(ParameterSetName = "Statistic", Mandatory = true, ValueFromPipeline = true)]
    public ManagementPackClass Class
    {
        get { return _class; }
        set { _class = value; }
    }
    private Guid _id = Guid.Empty;
    [Parameter(ParameterSetName = "Guid", Position = 0, Mandatory = true)]
    public Guid Id
    {
        get { return _id; }
        set { _id = value; }
    }

    private EnterpriseManagementObjectCriteria _criteria;
    [Parameter(ParameterSetName = "Criteria", Mandatory = true)]
    public EnterpriseManagementObjectCriteria Criteria
    {
        get { return _criteria; }
        set { _criteria = value; }
    }

    // If set, don't wrap the EMO
    private SwitchParameter _noAdapt;
    [Parameter]
    public SwitchParameter NoAdapt
    {
        get { return _noAdapt; }
        set { _noAdapt = value; }
    }

    // Only retrieve statistics
    private SwitchParameter _statistic;
    [Parameter(ParameterSetName = "Statistic")]
    public SwitchParameter Statistic
    {
        get { return _statistic; }
        set { _statistic = value; }
    }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        if (Statistic)
        {
            QueryOption = new ObjectQueryOptions();
            QueryOption.DefaultPropertyRetrievalBehavior = ObjectPropertyRetrievalBehavior.None;
            QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.NonBuffered;
            return;
        }
        else
        {
            QueryOption = new ObjectQueryOptions();
            QueryOption.DefaultPropertyRetrievalBehavior = ObjectPropertyRetrievalBehavior.All;
            if (MaxCount != Int32.MaxValue)
            {
                QueryOption.MaxResultCount = MaxCount;
                QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.NonBuffered;
            }
        }
        string sortProperty = SortBy;
    }

    protected override void ProcessRecord()
    {
        if (Id != Guid.Empty)
        {
            WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, _mg.EntityObjects.GetObject<EnterpriseManagementObject>(Id, ObjectQueryOptions.Default)));
            return;
        }
        // If someone provides us a filter, we'll use that instead of a criteria
        if (Filter != null)
        {
            Criteria = ConvertFilterToObjectCriteria(Class, Filter);
        }
        if (Class == null && Criteria != null)
        {
            Class = Criteria.ManagementPackClass;
        }
        try
        {
            addSortProperty(QueryOption, SortBy, Class);
        }
        catch
        {
            ;
        }
        int count = 0;
        if (Criteria == null)  // no criteria and no filter, get all the instances of the class
        {
            // If getting statistics, don't do anything
            if (Statistic)
            {
                WriteObject(new ItemStatistics(Class, Class.Name, _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(Class, QueryOption).Count));
                return;
            }
            else
            {
                foreach (EnterpriseManagementObject o in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(Class, QueryOption))
                {
                    count++;
                    if (NoAdapt)
                    {
                        WriteObject(o);
                    }
                    else
                    {
                        WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, o));
                    }
                    if (count >= MaxCount) { break; }
                }
            }
        }
        else // OK, we got a criteria - we'll use that
        {
            if (Statistic)
            {
                WriteObject(new ItemStatistics(Class, Class.Name, _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(Criteria, QueryOption).Count));
                return;
            }
            foreach (EnterpriseManagementObject o in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(Criteria, QueryOption))
            {
                count++;
                if (NoAdapt)
                {
                    WriteObject(o);
                }
                else
                {
                    WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, o));
                }
                if (count >= MaxCount) { break; }
            }
        }
    }

}

public enum Change { Property, Relationship, Instance };
public enum ChangeType { Modify, Insert, Delete };
public class PropertyChange
{
    public Change WhatChanged;
    public ChangeType TypeOfChange;
    public string Name;
    public object OldValue;
    public object NewValue;
    public PropertyChange() { ; }
    public PropertyChange(Change type,ChangeType operation, string name, object oldval, object newval)
    {
        WhatChanged = type;
        TypeOfChange = operation;
        Name = name;
        OldValue = oldval;
        NewValue = newval;
    }
}
public class ObjectChange
{
    public List<PropertyChange> Changes;
    public DateTime LastModified;
    public string UserName;
    public string Connector;
    public ObjectChange()
    {
        Changes = new List<PropertyChange>();
    }
}
public class SCSMHistory
{
    public EnterpriseManagementObject Instance;
    public List<ObjectChange> History;
    private List<EnterpriseManagementObjectHistoryTransaction> __HistoryData;
    public List<EnterpriseManagementObjectHistoryTransaction> get_RawHistoryData()
    {
        return this.__HistoryData;
    }
    private SCSMHistory()
    {
        History = new List<ObjectChange>();
        __HistoryData = new List<EnterpriseManagementObjectHistoryTransaction>();
    }
    public SCSMHistory(EnterpriseManagementObject emo)
    {
        History = new List<ObjectChange>();
        __HistoryData = new List<EnterpriseManagementObjectHistoryTransaction>();
        Instance = emo;
        foreach (EnterpriseManagementObjectHistoryTransaction ht in emo.ManagementGroup.EntityObjects.GetObjectHistoryTransactions(emo))
        {
            __HistoryData.Add(ht);
            ObjectChange pc = new ObjectChange();
            pc.LastModified = ht.DateOccurred.ToLocalTime();
            pc.UserName = ht.UserName;
            pc.Connector = ht.ConnectorDisplayName;
            bool addToHistory = false;
            foreach (KeyValuePair<Guid, EnterpriseManagementObjectHistory> h in ht.ObjectHistory)
            {
                foreach (EnterpriseManagementObjectClassHistory ch in h.Value.ClassHistory)
                {
                    foreach (KeyValuePair<ManagementPackProperty, Pair<EnterpriseManagementSimpleObject, EnterpriseManagementSimpleObject>> hpc in ch.PropertyChanges)
                    {
                        addToHistory = true;
                        pc.Changes.Add(new PropertyChange(Change.Property, ChangeType.Modify, hpc.Key.DisplayName, hpc.Value.First, hpc.Value.Second));
                    }
                }
                foreach (EnterpriseManagementObjectRelationshipHistory rh in h.Value.RelationshipHistory)
                {
                    addToHistory = true;
                    ManagementPackRelationship mpr = emo.ManagementGroup.EntityTypes.GetRelationshipClass(rh.ManagementPackRelationshipTypeId);
                    pc.Changes.Add(new PropertyChange(Change.Relationship, ChangeType.Modify, mpr.DisplayName, rh.SourceObjectId, rh.TargetObjectId));
                }
                
            }
            if (addToHistory) { History.Add(pc); }
        }
    }

}

[Cmdlet(VerbsCommon.Get, "SCSMObjectHistory")]
public class GetSCSMObjectHistoryCommand : ObjectCmdletHelper
{
    private EnterpriseManagementObject[] _object;
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public EnterpriseManagementObject[] Object
    {
        get { return _object; }
        set { _object = value; }
    }
    protected override void ProcessRecord()
    {
        foreach (EnterpriseManagementObject emo in Object)
        {
            WriteObject(new SCSMHistory(emo));
        }
    }
}

[Cmdlet(VerbsCommon.Set,"SCSMObject", SupportsShouldProcess=true)]
public class SetSMObjectCommand : ObjectCmdletHelper
{
    // Set properties on an EMO
    // this takes a hashtable where the 
    // KEY => PropertyName
    // VALUE => new value for the property

    // The adapted EMO
    private PSObject _smobject;
    [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true)]
    public PSObject SMObject
    {
        set { _smobject = value; }
        get { return _smobject; }
    }
    // the property/value pairs
    private Hashtable _propertyValueHashTable;
    [Parameter(ParameterSetName="hashtable",Position=1,Mandatory=true)]
    [Alias("PH")]
    public Hashtable PropertyHashtable
    {
        get { return _propertyValueHashTable; }
        set { _propertyValueHashTable = value; }
    }
    // The following two parameters are a short-cut, 
    // if you only want to set a single property
    // You don't need the hashtable
    private string _property;
    [Parameter(ParameterSetName="pair",Position=1,Mandatory=true)]
    public string Property
    {
        set { _property = value; }
        get { return _property; }
    }

    private List<Guid> objectList;
    private SwitchParameter _passThru;
    [Parameter]
    public SwitchParameter PassThru
    {
        get { return _passThru; }
        set { _passThru = value; }
    }
    private string _value;
    [Parameter(ParameterSetName="pair",Position=2,Mandatory=true)]
    [AllowEmptyString]
    [AllowNull]
    public string Value
    {
        set { _value = value; }
        get { return _value; }
    }
    // By default, we use IncrementalDiscoveryData to reduce the round
    // trips to the CMDB
    private SwitchParameter _noBulkOperation;
    [Parameter]
    public SwitchParameter NoBulkOperation
    {
        get { return _noBulkOperation; }
        set { _noBulkOperation = value; }
    }
    private IncrementalDiscoveryData pendingChanges;

    private Hashtable ht;

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        objectList = new List<Guid>();
        if ( ParameterSetName == "pair" )
        {
            ht = new Hashtable();
            ht.Add(Property,Value);
        }
        else
        {
            ht = new Hashtable(PropertyHashtable);
        }
        // if we're doing bulk operations, we'll need this
        if ( ! NoBulkOperation )
        {
            pendingChanges = new IncrementalDiscoveryData();
        }
    }

    protected override void ProcessRecord()
    {
        // EnterpriseManagementObject o = (EnterpriseManagementObject)SMObject.Members["__base"].Value;
        // Coerce the 
        EnterpriseManagementObject o = (EnterpriseManagementObject)SMObject.BaseObject;
        AssignNewValues ( o, ht );

        // If we're not doing bulk operations, we'll need to call this for each object
        if ( NoBulkOperation )
        {
            if ( ShouldProcess("Commit Change for " + o.Id))
            {
                o.Overwrite();
                if ( PassThru )
                {
                    WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, _mg.EntityObjects.GetObject<EnterpriseManagementObject>(o.Id, ObjectQueryOptions.Default)));
                }

            }
        }
        else
        {
            // One could argue that ShouldProcess is called here,
            // but to reduce verbosity, I do it below
            WriteVerbose("Adding " + o.Id + " to change list");
            objectList.Add(o.Id);
            pendingChanges.Add(o);
        }
    }
    protected override void EndProcessing()
    {
        // If we're doing bulk operations
        if ( ! NoBulkOperation )
        {
            if(ShouldProcess("SMObjects")) 
            { 
                pendingChanges.Overwrite(_mg); 
                if(PassThru)
                {
                    foreach(EnterpriseManagementObject emo in _mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(objectList, ObjectQueryOptions.Default))
                    {
                        WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, emo));
                    }
                }
            }
        }
    }
}

[Cmdlet(VerbsCommon.Remove, "SCSMObject", SupportsShouldProcess = true)]
public class RemoveSMObjectCommand : ObjectCmdletHelper
{
    // The adapted EMO
    private PSObject _smobject;
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    public PSObject SMObject
    {
        set { _smobject = value; }
        get { return _smobject; }
    }

    private SwitchParameter _force;
    [Parameter]
    public SwitchParameter Force
    {
        set { _force = value; }
        get { return _force; }
    }

    private SwitchParameter _progress;
    [Parameter]
    public SwitchParameter Progress
    {
        get { return _progress; }
        set { _progress = value; }
    }

    private ManagementPackEnumeration pendingDelete = null;

    // Remove is done via IncrementalDiscoveryData
    private IncrementalDiscoveryData idd;
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        pendingDelete = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System).GetEnumeration("System.ConfigItem.ObjectStatusEnum.PendingDelete");
        idd = new IncrementalDiscoveryData();
    }
    protected override void ProcessRecord()
    {
        EnterpriseManagementObject orig = (EnterpriseManagementObject)SMObject.BaseObject;
        if (ShouldProcess(orig.Name))
        {
            try
            {
                if (Progress) { WriteProgress(new ProgressRecord(0, "Remove Instance", orig.Name)); }
                if (Force)
                {
                    idd.Remove(orig);
                }
                else
                {
                    try
                    {
                        orig[null, "ObjectStatus"].Value = pendingDelete;
                        idd.Add(orig);
                    }
                    catch (NullReferenceException e)
                    {
                        ErrorRecord er = new ErrorRecord(e, "ObjectStatus Property", ErrorCategory.ObjectNotFound, orig);
                        ErrorDetails ed = new ErrorDetails("This object cannot be marked for deletion because it is not derived from System.ConfigItem");
                        ed.RecommendedAction = "Use the -Force parameter to remove this object";
                        er.ErrorDetails = ed;
                        WriteError(er);
                    }
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Object", ErrorCategory.NotSpecified, orig.Name));
            }
        }
    }

    // after you've added all the instances to the incrementaldiscoverydata
    // commit the changes
    protected override void EndProcessing()
    {
        if (ShouldProcess("Commit"))
        {
            try
            {
                if (Progress) { WriteProgress(new ProgressRecord(0, "Remove Instance", "Committing Removal")); }
                idd.Commit(_mg);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Object", ErrorCategory.NotSpecified, "Commit"));
            }
        }
    }
}

#endregion

#region SCSMRelationshipObject cmdlets

[Cmdlet(VerbsCommon.Remove,"SCSMRelationshipObject", SupportsShouldProcess=true)]
public class RemoveSMRelationshipObjectCommand : ObjectCmdletHelper
{
    // The adapted EMO
    private PSObject _smobject;
    [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true)]
    public PSObject SMObject
    {
        set { _smobject = value; }
        get { return _smobject; }
    }

    // Remove is done via IncrementalDiscoveryData
    private IncrementalDiscoveryData idd;
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        // pendingDelete = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System).GetEnumeration("System.ConfigItem.ObjectStatusEnum.PendingDelete");
        idd = new IncrementalDiscoveryData();
    }
    protected override void ProcessRecord()
    {
        EnterpriseManagementRelationshipObject<EnterpriseManagementObject> orig = (EnterpriseManagementRelationshipObject<EnterpriseManagementObject>)SMObject.BaseObject;
        if ( ShouldProcess(orig.Id.ToString()))
        {
            try
            {
                idd.Remove(orig);
            }
            catch ( Exception e )
            {
                WriteError(new ErrorRecord(e, "Object", ErrorCategory.NotSpecified, orig.Id));
            }
        }
    }

    // after you've added all the instances to the incrementaldiscoverydata
    // commit the changes
    protected override void EndProcessing()
    {
        if ( ShouldProcess("Commit"))
        {
            try
            {
                idd.Commit(_mg);
            }
            catch ( Exception e )
            {
                WriteError(new ErrorRecord(e, "Object", ErrorCategory.NotSpecified, "Commit"));
            }
        }
    }
}

[Cmdlet(VerbsCommon.New, "SCSMRelationshipObject", SupportsShouldProcess = true)]
public class NewSCSMRelationshipObject : ObjectCmdletHelper
{
    private ManagementPackRelationship _relationship;
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public ManagementPackRelationship Relationship
    {
        get { return _relationship; }
        set { _relationship = value; }
    }
    private Hashtable _properties;
    [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
    public Hashtable Properties
    {
        get { return _properties; }
        set { _properties = value; }
    }

    private SwitchParameter _passThru;
    [Parameter]
    public SwitchParameter PassThru
    {
        get { return _passThru; }
        set { _passThru = value; }
    }

    private EnterpriseManagementObject _source;
    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public EnterpriseManagementObject Source
    {
        get { return _source; }
        set { _source = value; }
    }

    private EnterpriseManagementObject _target;
    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public EnterpriseManagementObject Target
    {
        get { return _target; }
        set { _target = value; }
    }

    private SwitchParameter _bulk;
    [Parameter(ParameterSetName = "bulk")]
    public SwitchParameter Bulk
    {
        get { return _bulk; }
        set { _bulk = value; }
    }
    private IncrementalDiscoveryData idd = null;

    private SwitchParameter _noCommit;
    [Parameter(ParameterSetName = "NoCommit")]
    public SwitchParameter NoCommit
    {
        get { return _noCommit; }
        set { _noCommit = value; }
    }

    private SwitchParameter _progress;
    [Parameter(ParameterSetName = "bulk")]
    public SwitchParameter Progress
    {
        get { return _progress; }
        set { _progress = value; }
    }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        if (Bulk)
        {
            idd = new IncrementalDiscoveryData();
        }
    }

    private int count = 0;
    protected override void ProcessRecord()
    {
        CreatableEnterpriseManagementRelationshipObject ro = new Microsoft.EnterpriseManagement.Common.CreatableEnterpriseManagementRelationshipObject(_mg, Relationship);
        try
        {
            ro.SetSource(Source);
        }
        catch (Exception e)
        {
            ThrowTerminatingError(new ErrorRecord(e, "SourceError", ErrorCategory.InvalidOperation, ro));
        }
        try
        {
            ro.SetTarget(Target);
        }
        catch (Exception e)
        {
            ThrowTerminatingError(new ErrorRecord(e, "TargetError", ErrorCategory.InvalidOperation, ro));
        }
        IList<ManagementPackProperty> props = ro.GetProperties();
        if (Properties != null)
        {
            foreach (string s in Properties.Keys)
            {
                try
                {
                    WriteVerbose("looking for property " + s);
                    foreach (ManagementPackProperty p in props)
                    {
                        if (String.Compare(p.Name, s, true) == 0)
                        {
                            WriteVerbose("Setting " + s + " to " + Properties[s]);
                            AssignNewValue(p, ro[p], Properties[s]);
                            // ro[p].Value = Properties[s];
                            break;
                        }
                    }
                }
                catch
                {
                    WriteError(new ErrorRecord(new ItemNotFoundException(s), "Value " + s + " is null", ErrorCategory.ObjectNotFound, Properties));
                }
            }
        }
        if (ShouldProcess("Commit changes"))
        {
            try
            {
                if (Bulk)
                {
                    WriteVerbose("Adding " + ro.RelationshipId.ToString() + " to IDD");
                    if (Progress)
                    {
                        count++;
                        WriteProgress(new ProgressRecord(1, "Adding to incremental discovery data", ro.TargetObject.DisplayName));
                    }
                    idd.Add(ro);
                }
                else if (NoCommit)
                {
                    WriteObject(ro);
                }
                else
                {
                    ro.Commit();
                }
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Relationshipship Error", ErrorCategory.InvalidOperation, ro));
            }
        }
        if (PassThru && !NoCommit) { WriteObject(ro); }
        // WriteObject(ro);
    }
    protected override void EndProcessing()
    {
        base.EndProcessing();
        if (Bulk)
        {
            if (ShouldProcess("Commit Relationship Object"))
            {
                if (Progress)
                {
                    WriteProgress(new ProgressRecord(1, "Committing Relationships", count + " instances"));
                }
                idd.Commit(_mg);
            }
        }
    }
}

#endregion

#region SCSMObjectProjection cmdlets

[Cmdlet(VerbsCommon.New, "SCSMObjectProjection", SupportsShouldProcess = true)]
public class NewSCSMObjectProjectionCommand : ObjectCmdletHelper
{
    private string _type = null;
    [Parameter(Mandatory = true, Position = 0)]
    public String Type
    {
        get { return _type; }
        set { _type = value; }
    }

    /*
** A projection is represented as a complicated hashtable which has the following layout
** @{
**      __CLASS = <classname of seed object>
**      __OBJECT = @{
**          <property value pairs of seed object>
**          }
**      # ALIASNAME is the RELATIONSHIP alias as known by the projection
**      ALIASNAME = @{ 
**          __CLASS = <classname of the object in the relationship>
**          # this could be an array, but unlike the case of the seed object,
**          # this object may already exist, so we have to support a combination
**          # of EMOs or HashTables (for example, in an incident we may want to associate
**          # a number of comment logs with the incident, but there's no reason that we shouldn't
**          # create the comments in real time
**          __OBJECT = EMO1,EMO2,@{
**              __CLASS = <classname of target>
**              __OBJECT = @{
**                  <property value pairs of target object
**                  }
**              },EMO3,etc
**          }
**  }
*/
    private Hashtable _projection = null;
    [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
    public Hashtable Projection
    {
        get { return _projection; }
        set { _projection = value; }
    }

    private ManagementPackObjectTemplate _template;
    [Parameter]
    public ManagementPackObjectTemplate Template
    {
        get { return _template; }
        set { _template = value; }
    }
    private SwitchParameter _passThru;
    [Parameter]
    public SwitchParameter PassThru
    {
        get { return _passThru; }
        set { _passThru = value; }
    }

    private SwitchParameter _bulk;
    [Parameter]
    public SwitchParameter Bulk
    {
        get { return _bulk; }
        set { _bulk = value; }
    }

    private SwitchParameter _noCommit;
    [Parameter]
    public SwitchParameter NoCommit
    {
        get { return _noCommit; }
        set { _noCommit = value; }
    }

    private ManagementPackTypeProjection emop = null;
    private List<string> aliasCollection = null;
    private Hashtable aliasHT;

    private int count = 0;
    private int batchSize = 200;
    private IncrementalDiscoveryData pendingChanges;

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        if (Bulk && !NoCommit)
        {
            pendingChanges = new IncrementalDiscoveryData();
        }
        if (NoCommit) { Bulk = false; }
        // Find the projection we need
        Regex r = new Regex(Type, RegexOptions.IgnoreCase);
        foreach (ManagementPackTypeProjection p in _mg.EntityTypes.GetTypeProjections())
        {
            if (r.Match(p.Name).Success)
            {
                emop = p;
            }
        }
        if (emop == null)
        {
            ThrowTerminatingError(new ErrorRecord(new ItemNotFoundException("Projection"), "No such projection", ErrorCategory.ObjectNotFound, Type));
        }
        aliasHT = new Hashtable();
        aliasCollection = new List<string>();
        foreach (ManagementPackTypeProjectionComponent pc in emop.ComponentCollection)
        {
            aliasCollection.Add(pc.Alias);
            // Is there a way to handle SeedRole='Target' here?
            aliasHT.Add(pc.Alias, pc.TargetEndpoint);
            // WriteVerbose("Adding Alias: " + pc.Alias);
        }
        // WriteObject(aliasHT);
    }

    // emop guaranteed to be a valid projection type
    private EnterpriseManagementObjectProjection p = null;
    protected override void ProcessRecord()
    {
        if (Projection.ContainsKey("__SEED"))
        {
            // WriteVerbose("SEED");
            if (Projection["__SEED"] is PSObject)
            {
                WriteVerbose("Seed is PSObject");
                PSObject o = (PSObject)Projection["__SEED"];
                WriteVerbose("Type of seed: " + o.GetType());
                if (o.ImmediateBaseObject is EnterpriseManagementObject)
                {
                    WriteVerbose("Attempting to cast");
                    EnterpriseManagementObject seed = (EnterpriseManagementObject)o.ImmediateBaseObject;
                    WriteVerbose("Attempting to create projection");
                    WriteVerbose("Seed is a " + seed.GetType());
                    p = new EnterpriseManagementObjectProjection(seed);
                    WriteVerbose("Created Projection");
                }
                else
                {
                    ThrowTerminatingError(new ErrorRecord(new NullReferenceException("Projection"), "Bad Projection", ErrorCategory.InvalidArgument, o));
                }
            }
            else if (Projection["__SEED"] is EnterpriseManagementObject)
            {
                WriteVerbose("Seed is EMO");
                EnterpriseManagementObject seed = (EnterpriseManagementObject)Projection["__SEED"];
                p = new EnterpriseManagementObjectProjection(seed);
            }
            else
            {
                ThrowTerminatingError(new ErrorRecord(new NullReferenceException("Projection"), "Bad Projection", ErrorCategory.InvalidArgument, Projection["__SEED"]));
            }
        }
        else
        {
            // Construct the projection seed
            if (!Projection.ContainsKey("__CLASS"))
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("__CLASS"), "Hashtable Failure", ErrorCategory.InvalidArgument, Projection));
            }
            // WriteVerbose("Seed Class is " + (string)Projection["__CLASS"]);
            ManagementPackClass c = getClassFromName((string)Projection["__CLASS"]);
            if (c == null)
            {
                ThrowTerminatingError(new ErrorRecord(new ItemNotFoundException("CLASS"), "No such class", ErrorCategory.ObjectNotFound, Projection["__CLASS"]));
            }
            if (!Projection.ContainsKey("__OBJECT"))
            {
                // WriteObject(Projection);
                // ThrowTerminatingError( new ErrorRecord(new ArgumentException("__OBJECT"), "Hashtable Failure", ErrorCategory.InvalidArgument, Projection));
                WriteError(new ErrorRecord(new ArgumentException("__OBJECT"), "Hashtable Failure", ErrorCategory.InvalidArgument, Projection));
            }
            // CreatableEnterpriseManagementObject cemo = new CreatableEnterpriseManagementObject(c.ManagementGroup,c);
            Hashtable seedHash = (Hashtable)Projection["__OBJECT"];
            // WriteVerbose("__OBJECT is " + seedHash);
            p = new EnterpriseManagementObjectProjection(_mg, c);
            AssignNewValues(p.Object, seedHash);
        }
        WriteVerbose("Null Projection? " + (p == null).ToString());
        // OK - the seed is now complete - so work on the rest of the projection. 
        // go through the projection again - since hash tables are
        // TODO:: HANDLE CASE INSENSITIVE COMPARISON
        foreach (string k in Projection.Keys)
        {
            // skip the __CLASS (we did it above)
            if (
                String.Compare(k, "__CLASS", true) == 0 ||
                String.Compare(k, "__OBJECT", true) == 0 ||
                String.Compare(k, "__SEED", true) == 0)
            {
                continue;
            }
            WriteVerbose("Hunting for key: " + k);
            if (aliasCollection.Contains(k))
            {
                // WriteVerbose(">>>> setting up alias " + k);
                // TODO: Check the endpoint take the source or target endpoint where needed.
                ManagementPackRelationshipEndpoint endpoint = (ManagementPackRelationshipEndpoint)aliasHT[k];
                // ok - we've got something that is pointed to by an alias
                if (Projection[k] is Array)
                {
                    // WriteVerbose("**** inspecting array ****");
                    foreach (Object o in (Array)Projection[k])
                    {
                        if (o is PSObject)
                        {
                            PSObject hashValue = (PSObject)o;
                            // WriteVerbose("  PSObject is of type: " + ((PSObject)o).ImmediateBaseObject.GetType());
                            if (hashValue.ImmediateBaseObject is EnterpriseManagementObject)
                            {
                                try
                                {
                                    // WriteVerbose("   Adding EMO to projection");
                                    EnterpriseManagementObject target = (EnterpriseManagementObject)hashValue.ImmediateBaseObject;
                                    p.Add(target, endpoint);
                                }
                                catch (Exception e)
                                {
                                    WriteError(new ErrorRecord(e, "Could not add EMO to projection", ErrorCategory.InvalidOperation, Projection[k]));
                                }
                            }
                            else if (hashValue.ImmediateBaseObject is EnterpriseManagementObjectProjection)
                            {
                                try
                                {
                                    // WriteVerbose("   Adding EMOP to projection");
                                    EnterpriseManagementObjectProjection target = (EnterpriseManagementObjectProjection)hashValue.ImmediateBaseObject;
                                    p.Add(target, endpoint);
                                }
                                catch (Exception e)
                                {
                                    WriteError(new ErrorRecord(e, "Could not add EMOP to projection", ErrorCategory.InvalidOperation, Projection[k]));
                                }
                            }
                        }
                        else if (o is Hashtable)
                        {
                            // WriteVerbose("  Subelement is hash, creating CEMO");
                            Hashtable v = (Hashtable)o;
                            // WriteObject(v);
                            CreatableEnterpriseManagementObject cEMO = MakecEMOFromHash(v);
                            // WriteVerbose("    Adding cEMO to projection for " + k);
                            p.Add(cEMO, endpoint);
                        }
                        else
                        {
                            // WriteVerbose("   Object is of type: " + o.GetType() + " IGNORING THIS OBJECT");
                        }
                    }
                    // WriteVerbose("**** Done inspecting array ****");
                }
                else if (Projection[k] is EnterpriseManagementObject)
                {
                    // WriteVerbose("Object is an EMO: " + Projection[k]);

                    try
                    {
                        // WriteVerbose("Adding to projection");
                        p.Add((EnterpriseManagementObject)Projection[k], (ManagementPackRelationshipEndpoint)aliasHT[k]);
                    }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "foo", ErrorCategory.InvalidOperation, Projection[k]));
                    }

                }
                else if (Projection[k] is PSObject)
                {
                    PSObject pso = (PSObject)Projection[k];
                    // WriteVerbose("PSObject ImmediateBase: " + pso.ImmediateBaseObject.GetType());
                    if (pso.ImmediateBaseObject is EnterpriseManagementObject)
                    {
                        EnterpriseManagementObject target = (EnterpriseManagementObject)pso.ImmediateBaseObject;
                        try
                        {
                            // WriteVerbose("Adding " + target + " to projection as " + target);
                            p.Add(target, endpoint);
                        }
                        catch (Exception e)
                        {
                            WriteError(new ErrorRecord(e, "foo", ErrorCategory.InvalidOperation, Projection[k]));
                        }
                    }
                }
                else if (Projection[k] is EnterpriseManagementObjectProjection)
                {
                    // WriteVerbose("Object is an EMOP: " + Projection[k]);
                    try
                    {
                        EnterpriseManagementObjectProjection target = (EnterpriseManagementObjectProjection)Projection[k];
                        p.Add(target, endpoint);
                    }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "foo", ErrorCategory.InvalidOperation, Projection[k]));
                    }
                }
                else if (Projection[k] is Hashtable)
                {
                    // WriteVerbose("got a hashtable - will need to build EMO");
                    try
                    {
                        Hashtable relHash = (Hashtable)Projection[k];
                        CreatableEnterpriseManagementObject cEMO = MakecEMOFromHash(relHash);
                        p.Add(cEMO, endpoint);
                    }
                    catch (Exception e)
                    {
                        WriteError(new ErrorRecord(e, "foo", ErrorCategory.InvalidOperation, Projection[k]));
                    }
                    // new CreatableEnterpriseManagementObject(_mg,mpc);
                }
                else if (Projection[k] == null)
                {
                    WriteWarning("!!!! " + k + " value is null, ignoring");
                }
                else
                {
                    WriteVerbose("Got something strange : " + Projection[k].GetType());
                }
                // WriteObject(emop[k]);
                // WriteObject(cEMO);
                // emop[k];
                // now attach the object to the projection
                // p.Add(cEMO, k);
                // foreach(ManagementPackRelationship o in _mg.EntityTypes.GetRelationshipClasses())
                // {
                // ok, we've got a match, so let's add the build and add the object
                // if ( String.Compare(o.Alias, k, true)) { p.Add(cEMO, o.Target); }
                // }
            }
            else
            {
                WriteError(new ErrorRecord(new ObjectNotFoundException(k), "Alias not found on projection", ErrorCategory.NotSpecified, k));
            }
        }
        if (Template != null) { p.ApplyTemplate(Template); }
        // WriteObject(p);
        // WriteObject(emop);
        // WriteVerbose("So far so good!");
        // WriteObject(p);
        // NoCommit is used in those cases where you need a projection
        // as an element of another projection
        if (ShouldProcess("projection batch"))
        {
            if (NoCommit)
            {
                WriteObject(p);
            }
            else if (Bulk)
            {
                WriteVerbose("!!!! Adding projection to IDD");
                pendingChanges.Add(p);
                count++;
                if (count >= batchSize)
                {
                    WriteVerbose("!!!! committing " + count + " projections");
                    pendingChanges.Commit(_mg);
                    pendingChanges = new IncrementalDiscoveryData();
                    count = 0;
                }
            }
            else
            {
                try
                {
                    p.Commit();
                }
                catch (Exception e)
                {
                    WriteError(new ErrorRecord(e, "projection commit failure", ErrorCategory.InvalidOperation, p));

                }
            }
        }
        if (PassThru) { WriteObject(p); }
    }
    protected override void EndProcessing()
    {
        base.EndProcessing();
        if (ShouldProcess("!!!! Commit last batch of " + count + " projections"))
        {
            if (Bulk && count > 0)
            {
                WriteVerbose("!!!! Committing last batch of " + count + " incidents");
                pendingChanges.Commit(_mg);
            }
        }
    }
    private CreatableEnterpriseManagementObject MakecEMOFromHash(Hashtable ht)
    {
        ManagementPackClass mpc = getClassFromName((string)ht["__CLASS"]);
        WriteDebug("type of __OBJECT: " + ht["__OBJECT"].GetType().FullName);
        Hashtable relValues = (Hashtable)ht["__OBJECT"];
        CreatableEnterpriseManagementObject cEMO = new CreatableEnterpriseManagementObject(_mg, mpc);
        WriteDebug("Created new cEMO based on " + mpc.Name);
        AssignNewValues(cEMO, relValues);
        return cEMO;
    }
    private ManagementPackClass getClassFromName(string name)
    {
        foreach (ManagementPackClass c in _mg.EntityTypes.GetClasses())
        {
            if (String.Compare(name, c.Name, true) == 0) { return c; }
        }
        return null;
    }
}
    
[Cmdlet(VerbsCommon.Get, "SCSMObjectProjection", DefaultParameterSetName = "Wrapped")]
public class GetSMObjectProjectionCommand : FilterCmdletBase
{

    private PSObject _projectionObject;
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Wrapped")]
    [Parameter(ParameterSetName = "Statistics", ValueFromPipeline = true, Mandatory = true)]
    public PSObject ProjectionObject
    {
        get { return _projectionObject; }
        set { _projectionObject = value; }
    }

    private ManagementPackTypeProjection _projection;
    [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Raw", ValueFromPipeline = true)]
    public ManagementPackTypeProjection Projection
    {
        get { return _projection; }
        set { _projection = value; }
    }

    private string _projectionName = null;
    [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Name")]
    public string ProjectionName
    {
        get { return _projectionName; }
        set { _projectionName = value; }
    }

    private ObjectProjectionCriteria _criteria = null;
    [Parameter(ParameterSetName = "Criteria", Mandatory = true)]
    public ObjectProjectionCriteria Criteria
    {
        get { return _criteria; }
        set { _criteria = value; }
    }

    private SwitchParameter _noSort;
    [Parameter]
    public SwitchParameter NoSort
    {
        get { return _noSort; }
        set { _noSort = value; }
    }

    private SwitchParameter _statistic;
    [Parameter(ParameterSetName = "Statistics", Mandatory = true)]
    public SwitchParameter Statistic
    {
        get { return _statistic; }
        set { _statistic = value; }
    }

    // This is used for instream projection
    // creation. needed for those projections whose
    // alias targets are themselves a projection
    // (such as ResolutionAndBillableLog 
    // alias BillableLogs while requires a billabletime 
    // and workeduponbyuser
    private SwitchParameter _noCommit;
    [Parameter]
    public SwitchParameter NoCommit
    {
        get { return _noCommit; }
        set { _noCommit = value; }
    }

    protected override void BeginProcessing()
    {

        base.BeginProcessing();

        if (Statistic)
        {
            WriteDebug("Getting Statistics");
            QueryOption = new ObjectQueryOptions();
            QueryOption.DefaultPropertyRetrievalBehavior = ObjectPropertyRetrievalBehavior.None;
            QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.Buffered;
            return;
        }

        string sortProperty = SortBy;
        QueryOption = new ObjectQueryOptions();
        QueryOption.DefaultPropertyRetrievalBehavior = ObjectPropertyRetrievalBehavior.All;
        QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.NonBuffered;
        if (MaxCount != Int32.MaxValue)
        {
            QueryOption.MaxResultCount = MaxCount;
            QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.NonBuffered;
        }

        if (ProjectionName != null)
        {
            foreach (ManagementPackTypeProjection p in _mg.EntityTypes.GetTypeProjections())
            {
                if (String.Compare(p.Name, ProjectionName, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    Projection = p;
                    break;
                }
            }
            if (Projection == null)
            {
                ThrowTerminatingError(new ErrorRecord(new ArgumentNullException("No Projection found"), "Need Projection", ErrorCategory.InvalidOperation, "projection"));
            }
        }

        // Only build the sortCriteria if the Projection is not null
        // Current architecture means that we can't sort if we pipe a TypeProjection
        // TODO: AddSortProperty to QueryOptions for each projection seen on the pipeline
        if (Projection != null && !NoSort)
        {
            WriteVerbose("Sort property is: " + sortProperty);
            // sort the results
            // string sortCriteria = String.Format("<Sorting {0}><GenericSortProperty SortOrder=\"{1}\">{2}</GenericSortProperty></Sorting>", xmlns, Order, pName);
            string sortCriteria = null;
            try
            {
                sortCriteria = makeSortCriteriaString(sortProperty, Projection.TargetType);
                WriteDebug("sorting criteria : " + sortCriteria);
                QueryOption.AddSortProperty(sortCriteria, Projection, _mg);
            }
            catch (Exception e) // It's not a failure
            {
                WriteError(new ErrorRecord(e, "Sort Failure", ErrorCategory.InvalidArgument, sortCriteria));
            }
        }
        else
        {
            WriteDebug("Not Sorting");
        }
    }

    private int count = 0;
    protected override void ProcessRecord()
    {

        ObjectProjectionCriteria myCriteria = null;
        // If we got a wrapped object, unwrap it
        if (ProjectionObject != null)
        {
            WriteDebug("unwrapping PSObject to get projection");
            Projection = (ManagementPackTypeProjection)ProjectionObject.Properties["__base"].Value;
        }
        if (Statistic)
        {
            // Should this just be a call to get the seed?
            WriteVerbose("Getting Statistics");
            WriteDebug("Before Criteria: " + DateTime.Now.ToString());
            ObjectProjectionCriteria StatisticCriteria = new ObjectProjectionCriteria(Projection);
            WriteDebug("Before Reader: " + DateTime.Now.ToString());
            IObjectProjectionReader<EnterpriseManagementObject> reader = _mg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(StatisticCriteria, QueryOption);
            WriteDebug("After Reader: " + DateTime.Now.ToString());
            WriteObject(new ItemStatistics(Projection, Projection.Name, reader.Count));
            WriteDebug("After Reader.Count: " + DateTime.Now.ToString());
            return;
        }

        // Create the criteria
        // first, by checking whether there's a filter (and no criteria)
        // This has to be created for each object in the pipeline because we may have gotten a 
        // heterogenous collection of projections
        if (Criteria != null)
        {
            myCriteria = Criteria;
        }
        if (Filter != null && Criteria == null)
        {
            WriteDebug("converting filter to criteria");
            myCriteria = ConvertFilterToProjectionCriteria(Projection, Filter);
        }
        // ok - neither criteria, nor filter was provided, build a criteria from the projection
        if (myCriteria == null)
        {
            WriteDebug("null criteria");
            myCriteria = new ObjectProjectionCriteria(Projection);
        }
        QueryOption.ObjectRetrievalMode = ObjectRetrievalOptions.Buffered;
        // QueryOption.DefaultPropertyRetrievalBehavior = ObjectPropertyRetrievalBehavior.None;
        WriteDebug("Retrieval Mode: " + QueryOption.ObjectRetrievalMode.ToString());
        WriteDebug("Before projectionReader: " + DateTime.Now.ToString());

        IObjectProjectionReader<EnterpriseManagementObject> projectionReader =
            _mg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(myCriteria, QueryOption);
        WriteDebug("After projectionReader: " + DateTime.Now.ToString() + " Count is :" + projectionReader.Count);
        // Set the page size to a small number to decrease initial time to results
        projectionReader.PageSize = 1;
        WriteDebug("MaxCount = " + projectionReader.MaxCount);
        WriteDebug("Enter foreach: " + DateTime.Now.ToString());
        // while(projectionReader.
        // EnterpriseManagementObjectProjection p = projectionReader.First<EnterpriseManagementObjectProjection>();
        // for(int i=0;i < 1; i++) 
        foreach (EnterpriseManagementObjectProjection p in projectionReader)
        {
            count++;
            WriteDebug("Current count: " + count + " at " + DateTime.Now.ToString());
            if (count > MaxCount) { break; }
            WriteVerbose("Adapting " + p);
            /*
             * We can't just wrap a type projection because it is Enumerable. This means that we would only see the
             * components of the projection in the output so we have to construct this artificial wrapper. It would be easier if
             * projections weren't Enumerable, which means that PowerShell wouldn't treat a projection as a collection, or if 
             * PowerShell understood that certain collections shouldn't be unspooled, but that's not how PowerShell works.
             * Neither of those two options are available, so we adapt the object and present a PSObject with all the component
             * parts.
             */ 
            PSObject o = new PSObject();
            o.Members.Add(new PSNoteProperty("__base", p));
            o.Members.Add(new PSScriptMethod("GetAsXml", ScriptBlock.Create("[xml]($this.__base.CreateNavigator().OuterXml)")));
            o.Members.Add(new PSNoteProperty("Object", ServiceManagerObjectHelper.AdaptManagementObject(this, p.Object)));
            // Now promote all the properties on Object
            foreach (EnterpriseManagementSimpleObject so in p.Object.Values)
            {
                try
                {
                    o.Members.Add(new PSNoteProperty(so.Type.Name, so.Value));
                }
                catch
                {
                    WriteWarning("could not promote: " + so.Type.Name);
                }
            }

            o.TypeNames[0] = String.Format(CultureInfo.CurrentCulture, "EnterpriseManagementObjectProjection#{0}", myCriteria.Projection.Name);
            o.TypeNames.Insert(1, "EnterpriseManagementObjectProjection");
            o.Members.Add(new PSNoteProperty("__ProjectionType", myCriteria.Projection.Name));

            foreach (KeyValuePair<ManagementPackRelationshipEndpoint, IComposableProjection> helper in p)
            {
                // EnterpriseManagementObject myEMO = (EnterpriseManagementObject)helper.Value.Object;
                WriteVerbose("Adapting related objects: " + helper.Key.Name);
                String myName = helper.Key.Name;
                PSObject adaptedEMO = ServiceManagerObjectHelper.AdaptManagementObject(this, helper.Value.Object);
                // If the MaxCardinality is greater than one, it's definitely a collection
                // so start out that way
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
                        // TODO
                        // If this already exists, it should be converted to a collection
                    }
                }
            }


            WriteObject(o);
        }
    }
}

[Cmdlet(VerbsCommon.Set, "SCSMObjectProjection", SupportsShouldProcess = true)]
public class SetSCSMObjectProjectionCommand : ObjectCmdletHelper
{
    private PSObject _projection = null;
    [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
    public PSObject Projection
    {
        get { return _projection; }
        set { _projection = value; }
    }

    private Hashtable _propertyValues = null;
    [Parameter(Mandatory = true, Position = 1)]
    [Alias("ph")]
    public Hashtable PropertyValues
    {
        get { return _propertyValues; }
        set { _propertyValues = value; }
    }

    private SwitchParameter _passThru;
    [Parameter]
    public SwitchParameter PassThru
    {
        get { return _passThru; }
        set { _passThru = value; }
    }

    protected override void ProcessRecord()
    {
        EnterpriseManagementObjectProjection p = Projection.Members["__base"].Value as EnterpriseManagementObjectProjection;
        // EnterpriseManagementObject o = (EnterpriseManagementObject)SMObject.Members["__base"].Value;
        if (p != null)
        {
            EnterpriseManagementObject o = p.Object;
            // create a hashtable of management pack properties
            Hashtable ht = new Hashtable(StringComparer.OrdinalIgnoreCase);
            Hashtable valuesToUse = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (ManagementPackProperty prop in o.GetProperties())
            {
                ht.Add(prop.Name, prop);
            }
            // TODO: Add support for relationships
            foreach (string s in PropertyValues.Keys)
            {
                if (!ht.ContainsKey(s))
                {
                    WriteError(new ErrorRecord(new ObjectNotFoundException(s), "property not found on object", ErrorCategory.NotSpecified, o));
                }
                else
                {
                    valuesToUse.Add(s, PropertyValues[s]);
                }
            }
            AssignNewValues(o, valuesToUse);
            if (ShouldProcess("Save changes to projection"))
            {
                p.Commit();
            }
            if (PassThru) { WriteObject(p); }
        }
        else
        {
            WriteError(new ErrorRecord(new ArgumentException("SetProjection"), "object was not a projection", ErrorCategory.InvalidOperation, Projection));
        }
    }
}

#endregion

[Cmdlet(VerbsCommon.Get,"SCSMRelatedObject", DefaultParameterSetName="Wrapped")]
public class GetSMRelatedObjectCommand : ObjectCmdletHelper
{
    private EnterpriseManagementObject _smobject;
    [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true,ParameterSetName="Wrapped")]
    public EnterpriseManagementObject SMObject
    {
        get { return _smobject; }
        set { _smobject = value; }
    }

    private ManagementPackRelationship _relationship;
    [Parameter]
    public ManagementPackRelationship Relationship
    {
        get { return _relationship; }
        set { _relationship = value; }
    }

    private TraversalDepth _depth = TraversalDepth.OneLevel;
    [Parameter]
    public TraversalDepth Depth
    {
        get { return _depth; }
        set { _depth = value; }
    }

    protected override void ProcessRecord()
    {
        if ( Relationship != null)
        {
            foreach(EnterpriseManagementObject o in 
            _mg.EntityObjects.GetRelatedObjects<EnterpriseManagementObject>(SMObject.Id, Relationship, Depth, QueryOption))
            {
                WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, o));
            }
        }
        else
        {
            foreach(EnterpriseManagementObject o in 
                _mg.EntityObjects.GetRelatedObjects<EnterpriseManagementObject>(SMObject.Id, Depth, QueryOption))
            {
                WriteObject(ServiceManagerObjectHelper.AdaptManagementObject(this, o));
            }
        }
    }

}

[Cmdlet(VerbsCommon.Get, "SCSMRelationshipObject", DefaultParameterSetName="ID")]
public class GetSCSMRelationshipObjectCommand : ObjectCmdletHelper
{
    private Guid _id = Guid.Empty;
    [Parameter(Position = 0, ParameterSetName="ID", Mandatory = true)]
    public Guid Id
    {
        get { return _id; }
        set { _id = value; }
    }

    private ManagementPackRelationship[] _relationship = null;
    [Parameter(ParameterSetName="RELATIONSHIP",Mandatory=true,ValueFromPipeline=true,Position=0)]
    public ManagementPackRelationship[] Relationship
    {
        get { return _relationship; }
        set { _relationship = value; }
    }
    private ManagementPackRelationship _trelationship;
    [Parameter(ParameterSetName = "TARGETANDRELATIONSHIP", Mandatory = true)]
    public ManagementPackRelationship TargetRelationship
    {
        get { return _trelationship; }
        set { _trelationship = value; }
    }

    private ManagementPackClass _target = null;
    [Parameter(ParameterSetName="TARGET",Mandatory=true)]
    public ManagementPackClass Target
    {
        get { return _target; }
        set { _target = value; }
    }

    private ManagementPackClass _source = null;
    [Parameter(ParameterSetName="SOURCE",Mandatory=true)]
    public ManagementPackClass Source
    {
        get { return _source; }
        set { _source = value; }
    }

    private EnterpriseManagementObject _byTarget;
    [Parameter(ParameterSetName="TARGETOBJECT",Mandatory=true)]
    public EnterpriseManagementObject ByTarget
    {
        get { return _byTarget; }
        set { _byTarget = value; }
    }

    [Parameter(ParameterSetName = "TARGETANDRELATIONSHIP", Mandatory = true)]
    public EnterpriseManagementObject TargetObject
    {
        get { return _byTarget; }
        set { _byTarget = value; }
    }

    private EnterpriseManagementObject _bySource;
    [Parameter(ParameterSetName="SOURCEOBJECT",Mandatory=true)]
    public EnterpriseManagementObject BySource
    {
        get { return _bySource; }
        set { _bySource = value; }
    }

    private string _filter = null;
    [Parameter(ParameterSetName = "SOURCEOBJECT")]
    [Parameter(ParameterSetName = "TARGET")]
    [Parameter(ParameterSetName = "SOURCE")]
    [Parameter(ParameterSetName = "RELATIONSHIP")]
    [Parameter(ParameterSetName = "FILTER", Mandatory=true)]
    public string Filter
    {
        get { return _filter; }
        set { _filter = value; }
    }


    private bool _recursive = true;
    [Parameter(ParameterSetName = "SOURCEOBJECT")]
    [Parameter(ParameterSetName = "TARGET")]
    [Parameter(ParameterSetName = "SOURCE")]
    [Parameter(ParameterSetName = "RELATIONSHIP")]
    [Parameter(ParameterSetName = "FILTER")]
    public bool Recursive
    {
        get { return _recursive; }
        set { _recursive = value; }
    }
    private enum QueryBy { TargetClass, SourceClass, Target, Source, Relationship, TargetAndRelationship };

    #region GetRelationshipsHelper
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(ManagementPackRelationship r)                             { return GetRelationshipObjects(null, null, r, QueryBy.Relationship, null); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(ManagementPackRelationship r, string filter)              { return GetRelationshipObjects(null, null, r, QueryBy.Relationship, filter); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(string filter)                                            { return GetRelationshipObjects(null, null, null, QueryBy.Relationship, filter); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(ManagementPackClass c, QueryBy q)                         { return GetRelationshipObjects(c, null, null, q, null); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(ManagementPackClass c, QueryBy q, string filter)          { return GetRelationshipObjects(c, null, null, q, filter); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(EnterpriseManagementObject emo, QueryBy q)                { return GetRelationshipObjects(null, emo, null, q, null); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(EnterpriseManagementObject emo, QueryBy q, string filter) { return GetRelationshipObjects(null, emo, null, q, filter); }
    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(EnterpriseManagementObject emo, ManagementPackRelationship r) { return GetRelationshipObjects(null, emo, r, QueryBy.TargetAndRelationship, null); }

    private IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> GetRelationshipObjects(ManagementPackClass classType, EnterpriseManagementObject emo, ManagementPackRelationship r, QueryBy q, string filter)
    {
        EnterpriseManagementRelationshipObjectGenericCriteria criteria = null;
        IList<EnterpriseManagementRelationshipObject<EnterpriseManagementObject>> Results = null;
        WriteVerbose("Retrieving Relationship Objects. QueryBy is:" + q.ToString()  );
        WriteVerbose("Recursive:" + this.Recursive);
        if (filter != null)
        {
            WriteVerbose(" Using Filter: " + filter);
            Regex re;
            foreach (string s in EnterpriseManagementRelationshipObjectGenericCriteria.GetValidPropertyNames())
            {
                re = new Regex(s, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                filter = re.Replace(filter, s);
            }
            WriteVerbose(" After property name substitution: " + filter);
            try
            {
                string convertedFilter = ConvertFilterToGenericCriteria(filter);
                WriteVerbose(" Converted filter is: " + convertedFilter);
                criteria = new EnterpriseManagementRelationshipObjectGenericCriteria(convertedFilter);
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "CreateRelationshipCriteria", ErrorCategory.InvalidOperation, filter));
            }
        }
        try
        {
            switch (q)
            {
                case QueryBy.TargetClass:
                    {
                        if (criteria != null)
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsByTargetClass<EnterpriseManagementObject>(criteria, classType, ObjectQueryOptions.Default);
                        }
                        else
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsByTargetClass<EnterpriseManagementObject>(classType, ObjectQueryOptions.Default);
                        }
                        break;
                    }
                case QueryBy.SourceClass:
                    {
                        if (criteria != null)
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsBySourceClass<EnterpriseManagementObject>(criteria, classType, ObjectQueryOptions.Default);
                        }
                        else
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsBySourceClass<EnterpriseManagementObject>(classType, ObjectQueryOptions.Default);
                        }
                        break;
                    }
                case QueryBy.Target:
                    {
                        Results = _mg.EntityObjects.GetRelationshipObjectsWhereTarget<EnterpriseManagementObject>(emo.Id, ObjectQueryOptions.Default);
                        break;
                    }
                case QueryBy.TargetAndRelationship:
                    {
                        Results = _mg.EntityObjects.GetRelationshipObjectsWhereTarget<EnterpriseManagementObject>(emo.Id, r, DerivedClassTraversalDepth.Recursive, this.Recursive ? TraversalDepth.Recursive : TraversalDepth.OneLevel, ObjectQueryOptions.Default);
                        break;
                    }
                case QueryBy.Source:
                    {
                        if (criteria != null)
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsWhereSource<EnterpriseManagementObject>(emo.Id, criteria, this.Recursive ? TraversalDepth.Recursive : TraversalDepth.OneLevel, ObjectQueryOptions.Default);
                        }
                        else
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjectsWhereSource<EnterpriseManagementObject>(emo.Id, this.Recursive ? TraversalDepth.Recursive : TraversalDepth.OneLevel, ObjectQueryOptions.Default);
                        }
                        break;
                    }
                case QueryBy.Relationship:
                    {
                        if (criteria != null)
                        {
                            WriteVerbose("Relationship with criteria");
                            Results = _mg.EntityObjects.GetRelationshipObjects<EnterpriseManagementObject>(criteria, ObjectQueryOptions.Default);
                        }
                        else
                        {
                            Results = _mg.EntityObjects.GetRelationshipObjects<EnterpriseManagementObject>(r, DerivedClassTraversalDepth.Recursive, ObjectQueryOptions.Default);
                            WriteVerbose("Relationship via r: " + r.Id.ToString() + ". Count = " + Results.Count.ToString());
                        }
                        break;
                    }
                default:
                    {
                        ThrowTerminatingError(new ErrorRecord(new InvalidOperationException("No relationship query type specified"), "BadRelationshipQueryRequest", ErrorCategory.InvalidOperation, this));
                        break;
                    }
            }
        }
        catch (Exception e)
        {
            ThrowTerminatingError(new ErrorRecord(e, "RelationshipQuery", ErrorCategory.InvalidOperation, this));
        }
        foreach (EnterpriseManagementRelationshipObject<EnterpriseManagementObject> o in Results) { WriteVerbose("ID: " + o.Id.ToString()); }
        return Results; 
    }
#endregion GetRelationshipHelper

    protected override void ProcessRecord()
    {
        if ( this.ParameterSetName == "TARGET" )
        {
            WriteObject(GetRelationshipObjects(Target, QueryBy.Target, Filter), true);
        }
        else if ( this.ParameterSetName == "RELATIONSHIP" )
        {
            foreach (ManagementPackRelationship r in Relationship)
            {
                WriteObject(GetRelationshipObjects(r, Filter), true);
            }
        }
        else if ( this.ParameterSetName == "SOURCE" )
        {
            WriteObject(GetRelationshipObjects(Source, QueryBy.Source, Filter), true);
        }
        else if ( this.ParameterSetName == "TARGETOBJECT" )
        {
            WriteObject(GetRelationshipObjects(ByTarget, QueryBy.Target), true);
        }
        else if (this.ParameterSetName == "TARGETANDRELATIONSHIP")
        {
            WriteObject(GetRelationshipObjects(TargetObject, TargetRelationship));
        }
        else if (this.ParameterSetName == "SOURCEOBJECT")
        {
            WriteObject(GetRelationshipObjects(BySource, QueryBy.Source, Filter), true);
        }
        else if (this.ParameterSetName == "FILTER")
        {
            WriteObject(GetRelationshipObjects(Filter), true);
        }
        else
        {
            WriteObject(_mg.EntityObjects.GetRelationshipObject<EnterpriseManagementObject>(Id, ObjectQueryOptions.Default));
            WriteVerbose("By Id: " + Id.ToString());
        }
    }
}

[Cmdlet(VerbsCommon.Get, "SCSMConfigItem", SupportsShouldProcess = true)]
public class SCSMConfigItemGet : SMCmdletBase
{
    private String _DisplayName = null;
    private String _TargetProjection = "8ab27adb-13b1-2b7b-56e6-91598417cbee";

    [Parameter(Position = 0,
    Mandatory = true,
    ValueFromPipelineByPropertyName = true,
    HelpMessage = "The display name of the config item.")]

    [ValidateNotNullOrEmpty]
    public string DisplayName
    {
        get { return _DisplayName; }
        set { _DisplayName = value; }
    }

    [Parameter(Position = 1,
    Mandatory = false,
    ValueFromPipelineByPropertyName = true,
    HelpMessage = "The display name of the config item.")]

    [ValidateNotNullOrEmpty]
    public string TargetProjection
    {
        get { return _TargetProjection; }
        set { _TargetProjection = value; }
    }

    protected override void ProcessRecord()
    {
        ManagementPackTypeProjection targetProjection = _mg.EntityTypes.GetTypeProjection(new Guid(TargetProjection));

        ManagementPack trgProjMp = targetProjection.GetManagementPack();

        ManagementPack systemMp = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);

        WriteVerbose("Starting to build search criteria...");
        List<string> criterias = new List<string>();

        // Define the query criteria string. 
        // This is XML that validates against the Microsoft.EnterpriseManagement.Core.Criteria schema.              
        StringBuilder configCriteria = new StringBuilder(String.Format(@"
                <Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">
                  <Reference Id=""System.Library"" PublicKeyToken=""{0}"" Version=""{1}"" Alias=""targetMp"" />
                      <Expression>", systemMp.KeyToken, systemMp.Version.ToString()));

        if (this._DisplayName != null)
        {
            WriteVerbose(string.Format("Adding \"DisplayName like {0}\" to search criteria", this.DisplayName));
            criterias.Add(@"<SimpleExpression>
                                    <ValueExpressionLeft>
                                    <Property>$Context/Property[Type='targetMp!System.ConfigItem']/DisplayName$</Property>
                                    </ValueExpressionLeft>
                                    <Operator>Like</Operator>
                                    <ValueExpressionRight>
                                    <Value>" + this.DisplayName + @"</Value>
                                    </ValueExpressionRight>
                                </SimpleExpression>");
        }

        if (criterias.Count > 1)
        {
            for (int i = 0; i < criterias.Count; i++)
            {
                criterias[i] = "<Expression>" + criterias[i] + "</Expression>";
            }
        }

        if (criterias.Count > 1)
        {
            configCriteria.AppendLine("<And>");
        }

        foreach (var item in criterias)
        {
            configCriteria.AppendLine(item);
        }

        if (criterias.Count > 1)
        {
            configCriteria.AppendLine("</And>");
        }

        configCriteria.AppendLine(@"</Expression>
                </Criteria>");

        WriteDebug("Search criteria: " + configCriteria.ToString());

        // Define the criteria object by using one of the criteria strings.
        ObjectProjectionCriteria criteria = new ObjectProjectionCriteria(configCriteria.ToString(),
            targetProjection, _mg);

        // For each retrieved type projection, display the properties.
        List<EnterpriseManagementObjectProjection> result = new List<EnterpriseManagementObjectProjection>();
        foreach (EnterpriseManagementObjectProjection projection in
            _mg.EntityObjects.GetObjectProjectionReader<EnterpriseManagementObject>(criteria, ObjectQueryOptions.Default))
        {
            WriteVerbose(String.Format("Adding config item \"{0}\" to the pipeline", projection.Object.DisplayName));
            WriteObject(projection, false);
        }
    }
}

}
