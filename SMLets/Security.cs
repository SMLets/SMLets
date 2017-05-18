using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Security;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using System.Text.RegularExpressions;

namespace SMLets
{
    #region SCSMUserRole cmdlets

    [Cmdlet(VerbsCommon.New, "SCSMUserRole")]
    public class NewSCSMUserRoleCommand : SMCmdletBase
    {
        /* TODO
         * Add support for passing in ADUser objects
         * Add support for passing in a collection of domain\username objects
        */

        # region Private Properties
        private string _displayname;
        private string _description;
        private Profile _profile;
        private EnterpriseManagementObject[] _objects;
        private EnterpriseManagementObject[] _scsmusers;
        private ManagementPackTemplate[] _templates;
        private ManagementPackClass[] _classes;
        private ManagementPackView[] _views;
        private ManagementPackConsoleTask[] _consoletasks;
        private Boolean _alltemplates;
        private Boolean _allconsoletasks;
        private Boolean _allviews;
        private Boolean _allclasses;
        private Boolean _allobjects;

        # endregion Private Properties

        #region Parameters

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public String DisplayName
        {
            get { return _displayname; }
            set { _displayname = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = true)]
        public Profile Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public String Description
        {
            get { return _description; }
            set { _description = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public EnterpriseManagementObject[] Objects
        {
            get { return _objects; }
            set { _objects = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public ManagementPackTemplate[] Templates
        {
            get { return _templates; }
            set { _templates = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public ManagementPackClass[] Classes
        {
            get { return _classes; }
            set { _classes = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public ManagementPackView[] Views
        {
            get { return _views; }
            set { _views = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public ManagementPackConsoleTask[] ConsoleTasks
        {
            get { return _consoletasks; }
            set { _consoletasks = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public EnterpriseManagementObject[] SCSMUsers
        {
            get { return _scsmusers; }
            set { _scsmusers = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public SwitchParameter AllTemplates
        {
            get { return _alltemplates; }
            set { _alltemplates = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public SwitchParameter AllObjects
        {
            get { return _allobjects; }
            set { _allobjects = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public SwitchParameter AllClasses
        {
            get { return _allclasses; }
            set { _allclasses = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public SwitchParameter AllViews
        {
            get { return _allviews; }
            set { _allviews = value; }
        }

        [Parameter(ValueFromPipeline = false, Mandatory = false)]
        public SwitchParameter AllConsoleTasks
        {
            get { return _allconsoletasks; }
            set { _allconsoletasks = value; }
        }

        #endregion Parameters

        protected override void BeginProcessing()
        {
            //This will set the _mg which is the EnterpriseManagementGroup object for the connection to the server
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            //Create a new user role and set its properties based on what the user passed in
            UserRole ur = new UserRole();
            ur.DisplayName = _displayname;
            ur.Profile = _profile;
            ur.Name = SMHelpers.MakeMPElementSafeUniqueIdentifier("UserRole");
            if (_description != null) { ur.Description = _description; };

            ManagementPackClass classUser = SMHelpers.GetManagementPackClass(ClassTypes.Microsoft_AD_User, SMHelpers.GetManagementPack(ManagementPacks.Microsoft_Windows_Library, _mg), _mg);

            //Add the users
            if (_scsmusers != null)
            {
                foreach (EnterpriseManagementObject emo in _scsmusers)
                {
                    ur.Users.Add(emo[classUser, ClassProperties.System_Domain_User__Domain] + "\\" + emo[classUser, ClassProperties.System_Domain_User__UserName]);
                }
            }

            //Set the security scopes
            if (_alltemplates) { ur.Scope.Templates.Add(UserRoleScope.RootTemplateId); }
            else { if (_templates != null) { foreach (ManagementPackTemplate template in _templates) { ur.Scope.Templates.Add(template.Id); } } }

            if (_allobjects) { ur.Scope.Objects.Add(UserRoleScope.RootObjectId); }
            else { if (_objects != null) { foreach (EnterpriseManagementObject emo in _objects) { ur.Scope.Objects.Add(emo.Id); } } }

            if (_allclasses) { ur.Scope.Classes.Add(UserRoleScope.RootClassId); }
            else { if (_classes != null) { foreach (ManagementPackClass mpclass in _classes) { ur.Scope.Classes.Add(mpclass.Id); } } }

            if (_allconsoletasks) { ur.Scope.ConsoleTasks.Add(UserRoleScope.RootConsoleTaskId); }
            else { if (_consoletasks != null) { foreach (ManagementPackConsoleTask consoletask in _consoletasks) { ur.Scope.ConsoleTasks.Add(consoletask.Id); } } }

            if (_allviews) 
            { 
                Pair<Guid, Boolean> pairView = new Pair<Guid, Boolean>(UserRoleScope.RootViewId, false);
                ur.Scope.Views.Add(pairView);
            }
            else
            {
                if (_views != null)
                {
                    foreach (ManagementPackView view in _views)
                    {
                        if (view != null)
                        {
                            Pair<Guid, Boolean> pairView = new Pair<Guid, Boolean>(view.Id, false);
                            ur.Scope.Views.Add(pairView);
                        }
                    }
                }
            }
            _mg.Security.InsertUserRole(ur);
        }
    }
    
    [Cmdlet(VerbsCommon.Get,"SCSMUserRole",DefaultParameterSetName="name")]
    public class GetSCSMUserRole : SMCmdletBase
    {
        private string _name = ".*";
        [Parameter(Position=0,ParameterSetName="name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        private Guid _id = Guid.Empty;
        [Parameter(Position=0,ParameterSetName="id")]
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }

        Regex r = null;
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            r = new Regex(Name, RegexOptions.IgnoreCase);
        }
        protected override void ProcessRecord()
        {
            if ( Id != Guid.Empty )
            {
                return;
            }
            foreach(UserRole role in _mg.Security.GetUserRoles())
            {
                if ( r.Match(role.Name).Success || r.Match(role.DisplayName).Success)
                {
                    WriteObject(role);
                }
            }
        }

        protected override void EndProcessing()
        {
            if ( Id != Guid.Empty )
            {
                WriteObject(_mg.Security.GetUserRole(Id));
            }
        }

    }

    [Cmdlet(VerbsCommon.Remove, "SCSMUserRole", SupportsShouldProcess=true)]
    public class RemoveSCSMUserRole : SMCmdletBase
    {
        private UserRole[] _userroles;
        [Parameter(Mandatory = true,
            ValueFromPipeline = true)]
        public UserRole[] UserRole
        {
            get { return _userroles; }
            set { _userroles = value; }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            if (_userroles != null)
            {
                foreach (UserRole userrole in _userroles)
                {
                    if (!userrole.IsSystem)
                    {
                        string userInfo = userrole.Name;
                        if (userrole.DisplayName != null)
                        {
                            userInfo = userrole.DisplayName;
                        }
                        if (ShouldProcess(userInfo))
                        {
                            _mg.Security.DeleteUserRole(userrole);
                        }
                    }
                }
            }

        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
        }

    }

    #endregion

    #region SCSMRunAsAccount cmdlets

    [Cmdlet(VerbsCommon.Get,"SCSMRunAsAccount")]
    public class GetRunAsAccountsCommand : SMCmdletBase
    {
        
        private ActionAccountSecureData aasd = null;
        private IList<ManagementPackOverride> overrides =  null;
        private Hashtable sdHash = null;
        private Regex r;
        private string _name = ".*";
        [Parameter(Position=0)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            r = new Regex(Name, RegexOptions.IgnoreCase);
            overrides = _mg.Overrides.GetOverrides();
            sdHash = new Hashtable();
            foreach(SecureDataHealthServiceReference sr in _mg.Security.GetSecureDataHealthServiceReferences())
            {
                aasd = _mg.Security.GetSecureData(sr.SecureDataId) as ActionAccountSecureData;
                if ( aasd != null)
                {
                    break;
                }
            }
            
            foreach (ManagementPackOverride mpOverride in overrides)
            {
                ManagementPackSecureReferenceOverride secRefOverride = mpOverride as ManagementPackSecureReferenceOverride;
                if (secRefOverride != null )
                {
                    Guid secrefid = secRefOverride.SecureReference.Id;
                    int i = 0, x = 0;
                    byte[] bytes = new byte[(secRefOverride.Value.Length) / 2];
                    while (secRefOverride.Value.Length > i + 1)
                    {
                        long lngDecimal = Convert.ToInt32(secRefOverride.Value.Substring(i, 2), 16);
                        bytes[x] = Convert.ToByte(lngDecimal);
                        i = i + 2;
                        ++x;
                    }
                    SecureData secureData = _mg.Security.GetSecureData(bytes);
                    WindowsCredentialSecureData credential = secureData as WindowsCredentialSecureData;
                    if (credential != null)
                    {
                        if(! sdHash.ContainsKey(secrefid))
                        {
                            sdHash.Add(secrefid, String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", credential.Domain, credential.UserName));
                        }
                    }
                    else
                    {
                        ActionAccountSecureData actionAccount = secureData as ActionAccountSecureData;
                        if (actionAccount != null)
                        {
                            if(! sdHash.ContainsKey(secrefid))
                            {
                                sdHash.Add(secrefid, String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", actionAccount.Domain, actionAccount.UserName));
                            }
                        }
                    }
                }
            }
        }

        // This is a bit fragile and relies on current SM 1.0 behaviors
        // and definitions
        protected bool GetIsVisible(ManagementPackSecureReference sr)
        {
            // bail right away if we don't have any categories
            if ( sr.GetCategories().Count == 0 ) { return false; }
            foreach(ManagementPackCategory c in sr.GetCategories())
            {
                try {
                    string s = _mg.EntityTypes.GetEnumeration(c.Value.Id).Name;
                    // This is fragile - changes in the underlying system can cause
                    // misbehavior
                    if ( s == "VisibleToUser" ) { return true; }
                }
                catch { ; }
            }
            return false;
        }

        protected override void EndProcessing()
        {
            foreach(ManagementPackSecureReference sr in _mg.Security.GetSecureReferences())
            {
                if ( r.Match(sr.Name).Success || r.Match(sr.DisplayName).Success)
                {
                    bool IsVisible = GetIsVisible(sr);
                    PSObject o = new PSObject(sr);
                    o.Members.Add(new PSNoteProperty("DomainUser",GetUserName(sr)));
                    o.Members.Add(new PSNoteProperty("IsVisible", IsVisible));
                    o.Members.Add(new PSNoteProperty("ManagementPack", sr.GetManagementPack().FriendlyName));
                    WriteVerbose(GetUserName(sr));
                    WriteObject(o);
                }
            }
            
        }

        protected string GetUserName(ManagementPackSecureReference sr)
        {
            if ( sdHash.ContainsKey(sr.Id) )
            {
                return sdHash[sr.Id].ToString();
            }
            if ( aasd != null )
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", aasd.Domain, aasd.UserName);
            }
            return "unknown";
        }
        
    }

    [Cmdlet(VerbsCommon.Set, "SCSMRunAsAccount", SupportsShouldProcess = true)]
    public class SetRunAsAccountsCommand : SMCmdletBase
    {
        /* Created by Travis Wright (twright@microsoft.com, radtravis@hotmail.com) Jan 12 2011
         * 
         * REALLY LONG EXPLANATION OF HOW RUN AS ACCOUNTS WORK IN SERVICE MANAGER
         * This long explanation is provided for anybody that ever has to do something like this again
         * so they don't have to go through the pain I did to figure this out.  There is no way you could 
         * figure this out without first hand knowledge of how the product was built and access to the product
         * source code for reference.
         * 
         * In the System Center common platform which underlies SCSM, SCOM, and SCE there is a concept of a "Run As Account".
         * 
         * There are really three things at play here though:
         *      There is a "Run As Account" (aka SecureData) 
         *      There is a "Run As Profile" (aka SecureReference)
         *      There is a "Run As Account override" (aka SecureReferenceOverride)
         *      
         * In SCOM and SCE you can see all of these concepts in the UI, but in SCSM we obscured this because it wasnt necessary so you only see "Run As Accounts".
         * 
         * For the rest of this explanation I will only talk in coding terms since that's what we are doing here.  Just be aware of the mapping to the terms in the UI.
         * 
         * SecureData
         * ====================
         * A SecureData is where we store the credentials of a given "account".  There are different types of SecureData:
         * WindowsCredentialSecureData - stores domain\username & password for a Windows/AD user account
         * SimpleCredentialSecureData - stores simple login/password credentials for non-Windows stuff like Unix,SNMP traps etc, 
         * 
         * For our purposes we typically just need to worry about WindowsCredentialSecureData.
         * 
         * When a SecureData object is created it is stored on the CredentialManagerSecureStorage table in the database.  The password is encrypted using a fancy
         * public/private/symmetric key system.  I won't go into that.
         * 
         * SecureData objects cannot be declared in management packs.  They only exist in the database and therefore are not transportable from one management group to another.
         * 
         * 
         * SecureReference
         * =====================
         * SecureReferences can be declared in management packs.  They are declared in a section inside of TypeDefinitions (just before ModuleTypes if it exists)
         * <TypeDefinitions>
                <SecureReferences>
                    <SecureReference ID="ExchangeAdmin" Accessibility="Public" Context="System!System.Entity"/>
                </SecureReferences>
            </TypeDefinitions>

         * A SecureReference is a level of indirection between where the SecureData is defined and where it is used.  It allows a management pack author to create a "placeholder" 
         * for where an administrator needs to provide some specific credentials.  For example, let's say that I have a management pack that monitors Exchange.  In order to monitor Exchange
         * effectively I need to have some of my rules and monitors running under the security context of a user that is an Exchange administrator.  So - I could create a SecureReference called 
         * ExchangeAdmin and then tell different ModuleTypes in my MP that when then run they need to run under the SecureData that is associated with the ExchangeAdmin SecureReference like this:
         
         * <WriteActionModuleType ID="blah" Accessibility="Public" RunAs="ExchangeAdmin" Batching="false">
         * 
         * Now when the customer administrator imports this management he needs to specify which SecureData will be associated with that SecureReference.
         * 
         * In SCOM and SCE that is easy to do because we made that possible in the UI.  In SCSM we tried to hide the complexity of this level of indirection thinking that most people would never
         * need a custom Run As Account outside of using them in Connectors.  We handle all of this logic for the customer in the Connector wizards so it is simple.
         * 
         * In SCSM there is a 1:1 mapping of a SecureData to a SecureReference but the infrastructure allows for more than one SecureData to be associated to a SecureReference.
         * 
         * This cmdlet (Set-SCSMRunAsAccount) will provide the administrator a means to set the SecureData for a given SecureReference.  The cmdlet assumes the same 1:1 mapping we have for all other SecureReferences.
         * 
         * This cmdlet assumes that a SecureReference has already been defined in a management pack and imported into SCSM and now the administrator is going to just run Get-SCSMRunAsAccount | Set-SCSMRunAsAccount 
         * to set the SecureData for that SecureReference.
         * 
         * SecureReferences are stored on the SecureReference table in the database.
         * 
         * SecureReferenceOverride
         * =======================
         * 
         * A SecureReferenceOverride tells Service Manager to use a particular SecureData for a particular context.  For example, given the Exchange scenario above, let's say that I wanted to use
         * CredentialA (mydomain\user1) for some of my exchange servers and CredentialB (mydomain\user2) for the other Exchange servers. I can do that by using SecureReferenceOverrides.
         * 
         * This level of control is really only needed in SCOM situations.
         * 
         * In SCSM we just need to tell SCSM to use the same SecureData everywhere so this cmdlet will set the scope of the override to System.Entity.
         * 
         * SecureReferenceOverrides are stored in MPs and on the SecureReferenceOverride table in the database.
         * 
         * Conclusion
         * =======================
         * This cmdlet does 3 things:
         * 1) Creates a WindowsCredentialSecureData provided the credentials that are passed in.
         * 2) Creates a SecureReferenceOverride to tell SCSM to use the provided WindowsCredentialSecureData everywhere
         * 3) Grants all the health services (aka System Center Management services) permission to download the credentials and decrypt the password.
         * 
         */

        private WindowsCredentialSecureData _credentialSecureData = new WindowsCredentialSecureData();
        private PSCredential _credential;
        private ManagementPackSecureReference[] _secureReferences = null;
        
        //The user needs to pipe a ManagementPackSecureReference from Get-SCSMRunAsAccount.
        [Parameter(ValueFromPipeline = true,
                    Mandatory = true,
                    ParameterSetName = "ByObject")]
        public ManagementPackSecureReference[] SecureReferences
        {
            get { return _secureReferences; }
            set { _secureReferences = value; }
        }

        //The user must supply a PSCredential.  For example:
        //$cred = Get-Credential  <-- This will pop up a dialog the user can enter credentials into.  The password will be stored in a SecureString in $cred.
        //Then the user can call Get-SCSMRunAsAccount -Name "My Run As Account" | Set-SCSMRunAsAccount $cred
        [Parameter( Position = 0, 
                    Mandatory=true)]
        public PSCredential WindowsCredential
        {
            get { return _credential; }
            set { _credential = value; }
        }
        
        protected override void BeginProcessing()
        {
            //This will set the _mg which is the EnterpriseManagementGroup object for the connection to the server
            base.BeginProcessing();

            //Set the name of the secure data equal to the domain\username provided in the credential passed in.
            _credentialSecureData.Name = _credential.UserName;

            //Set the properties of the WindowsCredentialSecureData object
            _credentialSecureData.UserName = _credential.GetNetworkCredential().UserName;
            _credentialSecureData.Domain = _credential.GetNetworkCredential().Domain;
            _credentialSecureData.Data = _credential.Password;

            //Saving the WindowsCredentialSecureData.  This goes into the CredentialManagerSecureStorage table in the database.
            if (ShouldProcess(_credentialSecureData.Domain + "\\" + _credentialSecureData.UserName))
            {
                _mg.Security.InsertSecureData(_credentialSecureData);
            }
            
        }

        protected override void ProcessRecord()
        {
            foreach (ManagementPackSecureReference secureReference in _secureReferences)
            {
                //First get the MP that the Secure Reference that was passed in is stored in so that we can create some SecureReferenceOverrides in it.
                ManagementPack mpSecureReferenceMP = secureReference.GetManagementPack();

                //Before we create a new SecureReferenceOverride we need to check to see if one already exists.
                bool boolSecureRefOverrideAlreadyExists = false;

                //Loop through each Override in the MP...
                ManagementPackElementCollection<ManagementPackOverride> listOverrides = mpSecureReferenceMP.GetOverrides();
                foreach (ManagementPackOverride mpOverride in listOverrides)
                {
                    //...if it is a ManagementPackSecureReferenceOverride...
                    if (mpOverride is ManagementPackSecureReferenceOverride)
                    {
                        //...then cast it to a ManagementPackSecureReferenceOverride...
                        ManagementPackSecureReferenceOverride mpSecRefOverride = mpOverride as ManagementPackSecureReferenceOverride;
                        //...and then compare it to the SecureReference that was passed in...
                        if (mpSecRefOverride.SecureReference.Id == secureReference.Id)
                        {
                            //...if it is the same one then get a list of all the SecureData objects so we can compare with those...
                            IList<SecureData> secureDataList = _mg.Security.GetSecureData();
                            foreach (SecureData secureData in secureDataList)
                            {
                                //...by comparing the SecureStorageID of each of the existing and the .Value of the SecureData we just created...
                                if (String.Compare
                                            (BitConverter.ToString(secureData.SecureStorageId, 0, secureData.SecureStorageId.Length).Replace("-", ""),
                                                mpSecRefOverride.Value,
                                                StringComparison.Ordinal
                                            ) == 0
                                   )
                                {
                                    //...and if you find a match...
                                    WindowsCredentialSecureData windowsCred = secureData as WindowsCredentialSecureData;
                                    if (windowsCred != null)
                                    {
                                        //...then set the bool to true so we know that there is already a SecureReferenceOverride with this same exact SecureData
                                        // so we dont need to create a new SecureReferenceOverride in this case.
                                        boolSecureRefOverrideAlreadyExists = true;
                                    }
                                }
                            }
                        }
                    }
                }

                //Do we need to create a new SecureReferenceOverride?
                if (!boolSecureRefOverrideAlreadyExists)
                {
                    //Yes, we need to create a new SecureReferenceOverride...

                    //First create the SecureReferenceOverride object by setting its ID
                    ManagementPackSecureReferenceOverride secureOverride = new ManagementPackSecureReferenceOverride(mpSecureReferenceMP, String.Format("SecureReferenceOverride.{0}", Guid.NewGuid().ToString("N")));

                    //Then tell it that it's scope is for all objects by setting the class context to System.Entity
                    secureOverride.Context = _mg.EntityTypes.GetClass(SystemClass.Entity);

                    //Set the SecureReference equal to the SecureReference that was passed in.
                    secureOverride.SecureReference = secureReference;

                    //Give it a display name - doesnt need to be anything fancy since it doesnt show anywhere in the UI.
                    secureOverride.DisplayName = "SecureReferenceOverride_" + Guid.NewGuid().ToString();

                    //Convert to a byte array
                    secureOverride.Value = BitConverter.ToString(_credentialSecureData.SecureStorageId, 0, _credentialSecureData.SecureStorageId.Length).Replace("-", "");

                    //Now allow this SecureData to be downloaded to all the management servers
                    ApprovedHealthServicesForDistribution<EnterpriseManagementObject> approved = new ApprovedHealthServicesForDistribution<EnterpriseManagementObject>();
                    approved.Result = ApprovedHealthServicesResults.All;
                    
                    //Tell SCSM that we are going to update (or submit new) this SecureReferenceOverride
                    secureReference.Status = ManagementPackElementStatus.PendingUpdate;

                    //Post it to the database.  This will show up on the SecureReferenceOverride table in the database and in the <Overrides> section of the MP XML
                    string secureReferenceInfo = secureReference.Name;
                    if (secureReference.DisplayName != null)
                    {
                        secureReferenceInfo = secureReference.DisplayName;
                    }

                    if(ShouldProcess(secureReferenceInfo))
                    {
                        _mg.Security.SetApprovedHealthServicesForDistribution<EnterpriseManagementObject>(_credentialSecureData, approved);
                        mpSecureReferenceMP.AcceptChanges();
                    }
                }
            }
        }
    }

    #endregion

    [Cmdlet(VerbsCommon.Get, "SCSMUserRoleProfile")]
    public class GetSCUserRoleProfileCommand : SMCmdletBase
    {
        private string _name = null;
        private Regex r;

        [Parameter(ValueFromPipeline = false)]
        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected override void BeginProcessing()
        {
            //This will set the _mg which is the EnterpriseManagementGroup object for the connection to the server
            base.BeginProcessing();
            if (Name != null)
            {
                r = new Regex(Name, RegexOptions.IgnoreCase);
            }
        }

        protected override void EndProcessing()
        {
            foreach (Profile profile in _mg.Security.GetProfiles())
            {
                if (_name == null)
                {
                    PSObject o = new PSObject(profile);
                    WriteObject(o);
                }
                else
                {
                    if (r.Match(profile.Name).Success || r.Match(profile.DisplayName).Success)
                    {
                        PSObject o = new PSObject(profile);
                        WriteObject(o);
                    }
                }
            }

        }
        
    }

}