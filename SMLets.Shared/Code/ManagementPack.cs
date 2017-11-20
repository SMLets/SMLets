using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Security;
using System.Threading;
using System.Resources;
using System.Xml.XPath;
using Microsoft.CSharp;
using System.Xml.Schema;
using System.Reflection;
using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.CodeDom.Compiler;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Packaging;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Configuration.IO;
using SMLets.MPBMaker;

namespace SMLets
{
    #region GetSCManagementPackElement
    [Cmdlet("Get","SCManagementPackElement")]
    public class GetManagementPackElementCommand : SMCmdletBase
    {
        // Parameters

        private Guid _id;
        [Parameter(Position=0,Mandatory=true,ValueFromPipelineByPropertyName=true)]
        public Guid Id
        {
            get {return _id; }
            set { _id = value; }
        }

        protected override void ProcessRecord()
        {
            Type t = Type.GetType("Microsoft.EnterpriseManagement.Configuration.ManagementPackElementReference`1, Microsoft.EnterpriseManagement.Core, Version=7.0.5000.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            Type [] Targs = { _mg.GetType(), typeof(Guid) };
            Type t2 = t.MakeGenericType(typeof(ManagementPackElement));
            ConstructorInfo ci = t2.GetConstructor(BindingFlags.NonPublic|BindingFlags.Instance,null,Targs,null);
            object[] myargs = { _mg, Id };
            ManagementPackElement r = ((ManagementPackElementReference<ManagementPackElement>)ci.Invoke(myargs)).GetElement();
            WriteObject(r);
        }
    }
    #endregion

    #region GetSCManagementPack
    // Get-ManagementPack Cmdlet
    [Cmdlet("Get","SCManagementPack",DefaultParameterSetName="Name")]
    public class GetManagementPackCommand : SMCmdletBase
    {
        // Parameters

        private string _name = ".*";
        [Parameter(Position=0,ValueFromPipeline=true,ParameterSetName="Name")]
        public string Name
        {
            get {return _name; }
            set { _name = value; }
        }
        
        private Guid[] _id;
        [Parameter(Position=0,ValueFromPipeline=true,ParameterSetName="ID")]
        public Guid[] Id
        {
            get { return _id; }
            set { _id = value; }
        }

        protected override void ProcessRecord()
        {
            if ( ParameterSetName == "Name" )
            {
                Regex r = new Regex(Name, RegexOptions.IgnoreCase);
                foreach(ManagementPack mp in _mg.ManagementPacks.GetManagementPacks())
                {
                    if ( r.Match(mp.Name).Success )
                    {
                        WriteObject(mp);
                    }
                }
            }
            if ( ParameterSetName == "ID")
            {
                foreach(Guid g in Id)
                {
                    WriteVerbose("Looking for id: " + g.ToString());
                    try
                    {
                        WriteObject(_mg.ManagementPacks.GetManagementPack(g));
                    }
                    catch ( ObjectNotFoundException e )
                    {
                        WriteError( new ErrorRecord(e, "ManagementPack id '" + g + "' does not exist", ErrorCategory.ObjectNotFound, g.ToString()) );
                    }
                    catch ( Exception e )
                    {
                        WriteError( new ErrorRecord(e, "ManagementPack id '" + g + "' does not exist", ErrorCategory.ObjectNotFound, g.ToString()) );
                    }
                }
            }
        }
    }
    #endregion

    #region RemoveSCManagementPack
    // Implementation of Remove-ManagementPack
    // This removes a managementpack from the system
    [Cmdlet("Remove","SCManagementPack",SupportsShouldProcess = true)]
    public class RemoveManagementPackCommand : SMCmdletBase
    {
        // Private data
        private ManagementPack _mp;
        [Parameter(Position=0,Mandatory=true,ValueFromPipeline=true)]
        public ManagementPack ManagementPack
        {
            get { return _mp; }
            set { _mp = value; }
        }

        protected override void ProcessRecord()
        {
            string mpInfo = _mp.Name;
            if(_mp.DisplayName != null)
            {
                mpInfo = _mp.DisplayName;
            }
            if(ShouldProcess(mpInfo))
            {
                _mg.ManagementPacks.UninstallManagementPack(ManagementPack); 
            }
        
        }
    }
    #endregion

    #region NewSCManagementPack
    // Implementation of New-ManagementPack
    // This does not import an MP, just creates an MP object based on a file
    // which can then be used by Export-ManagementPack
    [Cmdlet(VerbsCommon.New,"SCManagementPack",DefaultParameterSetName="FromFile")]
    public class NewManagementPackCommand : SMCmdletBase
    {
        // Private data
        private FileInfo _mpFile;
        private ManagementPack _mp;
        // Parameters
        private string _fullname;
        [Parameter(Position=0,Mandatory=true,ValueFromPipelineByPropertyName=true,ParameterSetName="FromFile")]
        public string FullName
        {
            get { return _fullname; }
            set { _fullname = value; }
        }

        private string _managementPackName;
        [Parameter(Mandatory=true,ParameterSetName="NoFile")]
        [Alias("Name")]
        public string ManagementPackName
        {
            get { return _managementPackName; }
            set { _managementPackName = value; }
        }

        private string _friendlyName;
        [Parameter(ParameterSetName = "NoFile")]
        public string FriendlyName
        {
            get { return _friendlyName; }
            set { _friendlyName = value; }
        }

        private string _displayName;
        [Parameter(ParameterSetName = "NoFile")]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }
        /*
        private EnterpriseManagementGroup _emg;
        [Parameter(ParameterSetName = "NoFile", Mandatory = true)]
        public EnterpriseManagementGroup EMG
        {
            get { return _emg; }
            set { _emg = value; }
        }
         * */
        private Version _version;
        [Parameter(ParameterSetName = "NoFile")]
        public Version Version
        {
            get { return _version; }
            set { _version = value; }
        }

        private SwitchParameter _verify;
        [Parameter]
        public SwitchParameter Verify
        {
            get { return _verify; }
            set { _verify = value; }
        }

        private SwitchParameter _passThru;
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }
            set { _passThru = value; }
        }

        private List<String> _dependencyDirectory;
        [Parameter]
        private List<String> DependencyDirectory
        {
            get { return _dependencyDirectory; }
            set { _dependencyDirectory = value; }
        }
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (Version == null)
            {
                Version = new Version("1.0.0.0");
            }
            DependencyDirectory = new List<String>();
        }

        private List<ManagementPack> ImportMPFromFile()
        {
            List<ManagementPack> mpList = new List<ManagementPack>();
            ProviderInfo providerInfo;
            foreach(string file in GetResolvedProviderPathFromPSPath(FullName,out providerInfo))
            {
                // TODO: Ensure FullName
                // Be sure to bail before the MG is created, if the FileInfo
                // can't be created, there's no reason to continue
                _mpFile = new FileInfo(file);
                if ( _mpFile.Exists )
                {
                    // Build the MP
                    try
                    {
                        ManagementPackFileStore mpStore = new ManagementPackFileStore();
                        mpStore.AddDirectory(_mpFile.Directory);
                        if ( DependencyDirectory != null )
                        {
                            foreach(string dir in DependencyDirectory ) { mpStore.AddDirectory(dir); }
                        }
                        _mp = new ManagementPack(_mpFile.FullName, mpStore);
                        mpList.Add(_mp);
                    }
                    catch (Exception e)
                    {
                        WriteError( new ErrorRecord(e, "ManagementPack creation failed", ErrorCategory.NotSpecified, _mpFile.FullName) );
                    }
                    // OK, we have an MP, call verify if needed
                    if ( Verify )
                    {
                        try
                        {
                            _mp.Verify();
                        }
                        catch (Exception e)
                        {
                            WriteError( new ErrorRecord(e, "Verification of management pack failed", ErrorCategory.NotSpecified, _mpFile.FullName));
                        }
                    }
                }
                else
                {
                    WriteError( new ErrorRecord(new FileNotFoundException(file), "Failed to create management pack", ErrorCategory.ObjectNotFound, FullName));
                }
            }
            return mpList;
        }

        private ManagementPack ImportMPFromName()
        {
            if (FriendlyName == null)
            {
                FriendlyName = "Friendly name for " + ManagementPackName;
            }
            _mp = new ManagementPack(ManagementPackName, FriendlyName, Version, _mg);
            ManagementPack p;
            p = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.System);
            _mp.References.Add(p.Name.Replace('.', '_'), p);
            p = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.Windows);
            _mp.References.Add(p.Name.Replace('.', '_'), p);
            p = _mg.ManagementPacks.GetManagementPack(SystemManagementPack.SystemCenter);
            _mp.References.Add(p.Name.Replace('.', '_'), p);
            if ( DisplayName != null )
            {
                _mp.DisplayName = DisplayName;
            }
            else
            {
                _mp.DisplayName = ManagementPackName;
            }
            _mp.AcceptChanges();
            _mg.ManagementPacks.ImportManagementPack(_mp);
            return _mp;
        }

        protected override void ProcessRecord()
        {
            if ( ParameterSetName == "FromFile")
            {
                foreach (ManagementPack m in ImportMPFromFile())
                {
                    if (PassThru)
                    {
                        WriteObject(m);
                    }
                }
            }
            else
            {
                ManagementPack m = ImportMPFromName();
                if (PassThru)
                {
                    WriteObject(m);
                }
            }

        }
    }
    #endregion

    #region ImportSCManagementPack
    // Implementation of Import-ManagementPack
    [Cmdlet("Import","SCManagementPack", SupportsShouldProcess=true)]
    public class ImportManagementPackCommand : SMCmdletBase
    {
        // Parameters
        private string _fullname;
        [Parameter(ParameterSetName="FullName",Position=0,Mandatory=true,ValueFromPipelineByPropertyName=true)]
        public string FullName
        {
            get { return _fullname; }
            set { _fullname = value; }
        }

        private ManagementPack _managementpack;
        [Parameter(ParameterSetName="MPInstance",Position=0,Mandatory=true,ValueFromPipeline=true)]
        public ManagementPack ManagementPack
        {
            get { return _managementpack; }
            set { _managementpack = value; }
        }

        private SwitchParameter _noTokenCheck;
        [Parameter()]
        public SwitchParameter NoTokenCheck
        {
            set { _noTokenCheck = value; }
            get { return _noTokenCheck; }
        }

        private SwitchParameter _noSealCheck;
        [Parameter()]
        public SwitchParameter NoSealCheck
        {
            set { _noSealCheck = value; }
            get { return _noSealCheck; }
        }

        private SwitchParameter _noVersionCheck;
        [Parameter()]
        public SwitchParameter NoVersionCheck
        {
            set { _noVersionCheck = value; }
            get { return _noVersionCheck; }
        }

        private List<ManagementPack> InstalledMPs;
        private List<ManagementPack> InstallationOrder;
        private List<ManagementPack> MPsToInstall;
        private Hashtable FailureHash = new Hashtable();
        private ManagementPackBundleReader mpbr;
        
        protected override void BeginProcessing()
        {

            base.BeginProcessing();
            WriteDebug("InstalledMPs");
            InstalledMPs = new List<ManagementPack>();
            foreach(ManagementPack p in _mg.ManagementPacks.GetManagementPacks())
            {
                InstalledMPs.Add(p);
            }
            WriteDebug("InstallationOrder");
            InstallationOrder = new List<ManagementPack>();
            WriteDebug("MPsToInstall");
            MPsToInstall = new List<ManagementPack>();
            // We may need this, just created it 
            mpbr = ManagementPackBundleFactory.CreateBundleReader();
        }

        protected override void ProcessRecord()
        {
            if ( ManagementPack != null )
            {
                MPsToInstall.Add( ManagementPack );
            }
            if ( FullName != null )
            {
                ProviderInfo pi;
                foreach (string providerPath in GetResolvedProviderPathFromPSPath(FullName, out pi))
                {
                    FileInfo mpFile = ResolvePath(providerPath);
                    // FileInfo mpFile = new FileInfo(FullName);
                    if (mpFile.Exists)
                    {
                        if (mpFile.Extension.ToUpperInvariant() == ".MPB")
                        {
                            WriteVerbose("MPB: " + mpFile.FullName);
                            ManagementPackBundle mpb = mpbr.Read(mpFile.FullName, _mg);
                            if (ShouldProcess(mpFile.FullName))
                            {
                                _mg.ManagementPacks.ImportBundle(mpb);
                            }
                        }
                        else if (mpFile.Extension.ToUpperInvariant() == ".XML" || mpFile.Extension.ToUpperInvariant() == ".MP")
                        {
                            foreach (ManagementPack mp in MpFromFullName(mpFile.FullName))
                            {
                                MPsToInstall.Add(mp);
                            }
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new FileLoadException(mpFile.Name),
                                "Cannot Import " + mpFile.Name + ". REASON: Bad extension",
                                ErrorCategory.InvalidType,
                                mpFile.Name));
                        }
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            GetInstallationOrder();
            if ( MPsToInstall.Count > 0 )
            {
                foreach ( ManagementPack mp in MPsToInstall)
                {
                    WriteError(new ErrorRecord(new FileLoadException(mp.Name),
                        "Cannot Import " + mp.Name + ". REASON: " + FailureHash[mp], 
                        ErrorCategory.ObjectNotFound, 
                        mp.Name));
                }
            }
            foreach(ManagementPack mp in InstallationOrder)
            {
                if(ShouldProcess("Import management pack " + mp.Name))
                {
                    _mg.ManagementPacks.ImportManagementPack(mp);
                }
            }
        }
        // TODO: REDO FOR COLLECTION
        private FileInfo ResolvePath(string potentialFile)
        {
            // this handles the case that we got a fullpath
            if ( File.Exists(potentialFile)) { return new FileInfo(potentialFile); }
            else
            {
                // go look for the file
                ProviderInfo pid;
                Collection<string> results = GetResolvedProviderPathFromPSPath(potentialFile, out pid);
                if ( results == null || results.Count == 0)
                {
                    // ok - return a fileinfo object, 
                    // we know it's bad, but the code above will handle
                    return new FileInfo(potentialFile);
                }
                // BLECH!!!
                return new FileInfo(results[0]);
            }
        }

        private void GetInstallationOrder()
        {
            int offset = 0;
            string ReasonForFailure = "";
            while(offset < MPsToInstall.Count)
            {
                WriteDebug("OFFSET: " + offset + ", COUNT: " + MPsToInstall.Count);
                List<ManagementPackReference> mpr = new List<ManagementPackReference>();
                // List<string>keys = (List<string>)MPsToInstall[offset].References.Keys;
                // foreach(string key in keys) { mpr.Add(MPsToInstall[offset].References[key]); }
                foreach(string key in MPsToInstall[offset].References.Keys)
                {
                    mpr.Add(MPsToInstall[offset].References[key]);
                }
                bool OkToAdd = true;
                foreach(ManagementPackReference reference in mpr )
                {
                    if ( ! CheckReferenceIsInstalled(reference) ) 
                    { 
                        ReasonForFailure = String.Format(CultureInfo.CurrentCulture, "Referenced ManagementPack '{0}' is not installed", reference.Name);
                        OkToAdd = false;
                        break;
                    }
                    else
                    { 
                        if ( ! DoVersionCheck(reference) )
                        {
                            ReasonForFailure = String.Format(CultureInfo.CurrentCulture, "Version Mismatch '{0}' <> '{1}'",MPsToInstall[offset].Version,reference.Version);
                            OkToAdd = false;
                            break;
                        }
                        if ( ! CheckIsSealed(reference) )
                        {
                            ReasonForFailure = String.Format(CultureInfo.CurrentCulture, "Referenced ManagementPack '{0}' is unsealed", reference.Name);
                            OkToAdd = false;
                            break;
                        }
                        if ( ! DoKeyTokenCheck(reference) )
                        {
                            ReasonForFailure = String.Format(CultureInfo.CurrentCulture, "KeyToken Mismatch '{0}' <> '{1}'",MPsToInstall[offset].KeyToken,reference.KeyToken);
                            OkToAdd = false;
                            break;
                        }
                    }
                }
                if ( OkToAdd ) 
                { 
                    InstallationOrder.Add(MPsToInstall[offset]); 
                    InstalledMPs.Add(MPsToInstall[offset]);
                    MPsToInstall.Remove(MPsToInstall[offset]);
                    offset = 0;
                }
                else
                {
                    if ( FailureHash.ContainsKey(MPsToInstall[offset]))
                    {
                        FailureHash[MPsToInstall[offset]] = ReasonForFailure;
                    }
                    else
                    {
                        FailureHash.Add(MPsToInstall[offset], ReasonForFailure);
                    }
                    offset++;
                }
            }
        }

        private ManagementPack GetManagementPackByName(string name)
        {
            foreach(ManagementPack mp in InstalledMPs)
            {
                if ( mp.Name == name ) { return mp; }
            }
            return null;
        }

        private bool CheckReferenceIsInstalled(ManagementPackReference reference) 
        { 
            if ( GetManagementPackByName(reference.Name) != null) { return true; } else { return false; }
        }
        private bool DoVersionCheck(ManagementPackReference reference) 
        { 
            if ( NoVersionCheck ) { return true; }
            if ( (GetManagementPackByName(reference.Name).Version >= reference.Version) )
            {
                return true; 
            }
            else
            {
                return false;
            }
        }
        private bool CheckIsSealed(ManagementPackReference reference) 
        { 
            if ( NoSealCheck ) { return true; }
            if ( GetManagementPackByName(reference.Name).Sealed )
            {
                return true; 
            }
            else
            {
                return false;
            }
        }
        private bool DoKeyTokenCheck(ManagementPackReference reference) 
        { 
            if ( NoTokenCheck ) { return true; }
            if ( GetManagementPackByName(reference.Name).KeyToken == reference.KeyToken )
            {
            return true; 
            }
            else
            {
            return false;
            }
        }

        private List<ManagementPack> MpFromFullName(string FullName)
        {
            ProviderInfo providerInfo;
            FileInfo _mpFile;
            ManagementPack _theMp;
            List<ManagementPack> mplist = new List<ManagementPack>();
            foreach(string file in GetResolvedProviderPathFromPSPath(FullName,out providerInfo))
            {
                // TODO: Ensure FullName
                // Be sure to bail before the MG is created, if the FileInfo
                // can't be created, there's no reason to continue
                _mpFile = new FileInfo(file);
                if ( _mpFile.Exists )
                {
                    // Build and import the MP
                    try
                    {
                        ManagementPackFileStore mpStore = new ManagementPackFileStore();
                        mpStore.AddDirectory(_mpFile.Directory);
                        _theMp = new ManagementPack(_mpFile.FullName, mpStore);
                        mplist.Add( _theMp );
                        // MPsToInstall.Add( _theMp );
                        // _mg.ManagementPacks.ImportManagementPack(_theMp);
                    }
                    catch (Exception e)
                    {
                        ThrowTerminatingError(
                                new ErrorRecord(e, "ManagementPack creation failed",
                                    ErrorCategory.NotSpecified, _mpFile.FullName)
                                );
                    }
                }
                else
                {
                    WriteError(
                            new ErrorRecord(new FileNotFoundException(file),
                                "Import Failed",
                                ErrorCategory.ObjectNotFound, FullName)
                            );
                }
            }
            return mplist;
        }

    }
    #endregion

    #region ExportSCManagementPack
    // Implementation of Export-ManagementPack
    [Cmdlet("Export","SCManagementPack",SupportsShouldProcess=true)]
    public class ExportManagementPackCommand : PSCmdlet
    {
        // Private data
        // Parameters
        private ManagementPack _mp;
        private string _outputFileName;
        [Parameter(Position=1,Mandatory=true,ValueFromPipeline=true)]
        public ManagementPack ManagementPack
        {
            get { return _mp; }
            set { _mp = value; }
        }

        private DirectoryInfo _target;
        [Parameter(Position=0,Mandatory=true)]
        public DirectoryInfo TargetDirectory
        {
            get { return _target; }
            set { _target = value; }
        }
        protected override void BeginProcessing()
        {
            if ( ! TargetDirectory.Exists )
            {
                ThrowTerminatingError(
                        new ErrorRecord((new ItemNotFoundException()), "Target Directory does not exist",
                            ErrorCategory.ObjectNotFound, TargetDirectory.FullName)
                        );
            }
        }
        protected override void ProcessRecord()
        {
            ExportPack();
        }
        private void ExportPack( )
		{
            if ( ShouldProcess("Export Management Pack " + ManagementPack.Name))
            {
                try
                {
                    WriteVerbose("exporting " + ManagementPack.Name);
                    _outputFileName = TargetDirectory + "/" + ManagementPack.Name + ".xml";
                    Stream mpStream = new FileStream(_outputFileName, FileMode.Create);
                    XmlWriter writer = XmlWriter.Create(mpStream);
                    ManagementPackXmlWriter mpWriter = new ManagementPackXmlWriter(writer);
                    mpWriter.WriteManagementPack(ManagementPack);
                    mpStream.Close();
                }
                catch ( Exception e)
                {
                    WriteError(
                            new ErrorRecord(e, "Failed to export",
                                ErrorCategory.WriteError, _outputFileName)
                            );
                }
            }
		}
    }
    #endregion

    #region NewSCSealedManagementPack
    /// <summary>
    /// This class is an exectuable wrapper to seal a ManagementPack
    /// </summary>
    [Cmdlet("New", "SCSealedManagementPack")]
    public class NewSealedManagementPackCommand : PSCmdlet
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private string _fullName;
        private string _keyfilePath;
        private string _companyName;
        private string _copyright;
        private string _outputDirectory;
        private SwitchParameter _delaySign;

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value; }
        }
        [Parameter(Mandatory = true)]
        public string KeyFilePath
        {
            get { return _keyfilePath; }
            set { _keyfilePath = value; }
        }
        [Parameter(Mandatory = true)]
        public string CompanyName
        {
            get { return _companyName; }
            set { _companyName = value; }
        }
        [Parameter(Mandatory = true)]
        public string Copyright
        {
            get { return _copyright; }
            set { _copyright = value; }
        }
        [Parameter(Mandatory = true)]
        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { _outputDirectory = value; }
        }
        public SwitchParameter DelaySign
        {
            get { return _delaySign; }
            set { _delaySign = value; }
        }

        protected override void ProcessRecord()
        {
            try
            {
                if (string.IsNullOrEmpty(FullName))
                {
                    throw new ArgumentNullException("FullName");
                }

                //is this an Xml file?
                if (!(FullName.EndsWith(FastAssemblyWriter.XmlExtension, StringComparison.OrdinalIgnoreCase) ||
                        FullName.EndsWith(FastAssemblyWriter.MpbExtension, StringComparison.OrdinalIgnoreCase))
                    )
                {
                    ThrowTerminatingError(new ErrorRecord(null, "Invalid file extension", ErrorCategory.InvalidOperation, FullName));
                }
                if (FullName.EndsWith(FastAssemblyWriter.MpbExtension, StringComparison.OrdinalIgnoreCase))
                {
                    FastAssemblyWriter.isMpb = true;
                }


                //create the assembly writer settings object
                FastAssemblyWriterSettings settings = new FastAssemblyWriterSettings(CompanyName, KeyFilePath, DelaySign);
                settings.Copyright = Copyright;
                settings.OutputDirectory = OutputDirectory;


                //write assembly file
                FastAssemblyWriter assemblywriter = new FastAssemblyWriter(settings);
                if (FastAssemblyWriter.isMpb)
                {
                    string outfile = assemblywriter.WriteMPB(FullName);
                    WriteVerbose("ManagementPack name is " + outfile);
                }
                else
                {
                    string outfile = assemblywriter.WriteManagementPack(FullName);
                    WriteVerbose("MPB name is " + outfile);
                }

            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "foo", ErrorCategory.InvalidOperation, FullName));
            }

        }

    }
    /// <summary>
    /// This class is used to create sealed ManagementPacks on disk(.DLL files)
    /// </summary>
    public class FastAssemblyWriter
    {
        #region Constructors
        public FastAssemblyWriter(FastAssemblyWriterSettings settings)
        {
            #region Validate Settings
            //validate settings
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            //validate keyfile string
            if (string.IsNullOrEmpty(settings.KeyFilePath))
            {
                throw new ArgumentNullException("settings");
            }

            //validate that the specified keyfile exists
            FileInfo finfo = null;
            try
            {
                finfo = new FileInfo(settings.KeyFilePath);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("settings");
            }

            if (finfo == null || (finfo.Exists == false))
            {
                throw new FileNotFoundException("Specified Keyfile does not exits. Cannot find file : " + settings.KeyFilePath);
            }
            #endregion

            this._settings = settings;
        }
        #endregion


        #region Private Members
        private FastAssemblyWriterSettings _settings = null;
        #endregion

        #region WriteMPB
        public string WriteMPB(string fileName)
        {
            //validate mp
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            // since this is an MPB, just write it out

            #region Read and Compress ManagementPack Xml
            //get MP contents
            byte[] buffer = File.ReadAllBytes(fileName);

            //stream for compressed output
            MemoryStream msout = new MemoryStream();

            //compress the buffer copy
            using (MemoryStream msin = new MemoryStream(buffer))
            {

                //compress the management pack contents to memory
                Compress(msin, msout);
            }
            #endregion

            #region Write Assembly
            //write the assembly resource file
            WriteResource(msout.ToArray());

            //Create the Assembly
            string mpoutputfilename = String.Format("Sealed_{0}", fileName);
            return (CreateAssemblyFile(this._settings, mpoutputfilename));
            #endregion
        }
        #endregion

        #region WriteManagementPack Method
        /// <summary>
        /// This method creates a sealed ManagementPack (signed .NET Assembly with .DLL file extension)
        /// in the specified directory. 
        /// 
        /// The file name for the output file obtained from the Manifest section of ManagementPack (value of the <ID> tag)
        /// (and is not changeable)
        /// </summary>
        /// <param name="mp">The ManagementPack to write as an assembly</param>
        public string WriteManagementPack(string fileName)
        {
            //validate mp
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            #region Get Information from MP
            //open document
            XPathDocument doc = null;
            doc = new XPathDocument(fileName);
            XPathNavigator nav = doc.CreateNavigator();

            string mpoutputfilename = string.Empty;

            //read Name name
            XPathNavigator namenav = nav.SelectSingleNode("/ManagementPack/Manifest/Identity/ID");
            if (namenav != null)
            {
                //set the Version field
                mpoutputfilename = namenav.Value;
            }
            else
            {
                throw new ItemNotFoundException("Cannot read ManagementPack Name from the file specified");
            }

            //read version
            XPathNavigator versionnav = nav.SelectSingleNode("/ManagementPack/Manifest/Identity/Version");
            if (versionnav != null)
            {
                //set the Version field
                this._settings.AssemblyVersion = new Version(versionnav.Value);
            }
            else
            {
                throw new ItemNotFoundException("Cannot read Management Pack Version from the file specified");
            }

            //read product name
            XPathNavigator productnav = nav.SelectSingleNode("/ManagementPack/Manifest/Name");
            if (productnav != null)
            {
                //set the Version field
                this._settings.ProductName = productnav.Value;
            }
            else
            {
                throw new ItemNotFoundException("Cannot read ManagementPack Name from the file specified");
            }
            #endregion

            #region Read and Compress ManagementPack Xml
            //get MP contents
            string mpcontents = nav.OuterXml;

            //store in a byte buffer
            byte[] buffer = Encoding.Unicode.GetBytes(mpcontents);

            //stream for compressed output
            MemoryStream msout = new MemoryStream();

            //compress the buffer copy
            using (MemoryStream msin = new MemoryStream(buffer))
            {

                //compress the management pack contents to memory
                Compress(msin, msout);
            }
            #endregion

            #region Write Assembly
            //write the assembly resource file
            WriteResource(msout.ToArray());

            //Create the Assembly
            return (CreateAssemblyFile(this._settings, mpoutputfilename));
            #endregion
        }
        #endregion

        #region Helper Methods
        private static void Compress(Stream inputstream, Stream outputstream)
        {
            //compress in 4k chunks
            byte[] buffer = new byte[ChunkSize];
            int n;
            using (GZipStream gzipCompressionStream = new GZipStream(outputstream, CompressionMode.Compress))
            {
                //read in chunks from the inputstream
                while ((n = inputstream.Read(buffer, 0, ChunkSize)) != 0)
                {
                    gzipCompressionStream.Write(buffer, 0, n);
                }
            }
        }

        //write out the resource file
        private static void WriteResource(byte[] mpbytes)
        {
            //write out the resource
            using (ResourceWriter resourcewriter = new ResourceWriter(FastAssemblyWriter.AssemblyManagementPackResourcePackageName))
            {
                resourcewriter.AddResource(FastAssemblyWriter.AssemblyManagementPackResourceName, mpbytes);
            }
        }

        //create assembly
        // [SerializableAttribute]
        // [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
        // [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
        [PermissionSetAttribute(SecurityAction.Assert, Name = "LinkDemand")]
        private static string CreateAssemblyFile(FastAssemblyWriterSettings settings, string mpoutputname)
        {
            #region Compiler options
            //set options for assembly creation
            CompilerParameters options = new CompilerParameters();
            options.GenerateExecutable = false;
            options.GenerateInMemory = false;
            options.TreatWarningsAsErrors = false;
            options.EmbeddedResources.Add(AssemblyManagementPackResourcePackageName);
            if (isMpb)
            {
                options.OutputAssembly = Path.Combine(settings.OutputDirectory, mpoutputname + FastAssemblyWriter.MpbExtension);
            }
            else
            {
                options.OutputAssembly = Path.Combine(settings.OutputDirectory, mpoutputname + AssemblyExtension);
            }

            StringBuilder compilerOptions = new StringBuilder();

            if (settings.DelaySign)
            {
                compilerOptions.Append("/DelaySign+ ");
            }
            compilerOptions.AppendFormat("/keyfile:{0}", settings.KeyFilePath);
            options.CompilerOptions = compilerOptions.ToString();
            #endregion

            #region Assembly Attributes
            string[] sources = new string[1];
            //create the formatted csharp code string that sets all assembly attributes
            string AssemblyDescriptionSubstitutedSourceCode =
                String.Format(CultureInfo.CurrentCulture, AssemblyAttributesCodeTemplate, settings.AssemblyVersion.ToString(),
                settings.CompanyName,
                settings.ProductName,
                settings.Copyright);

            //set source csharp code to this formatted string
            sources[0] = AssemblyDescriptionSubstitutedSourceCode;
            #endregion

            #region Build Assembly
            //create a new code provider
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();

            //run build
            CompilerResults cr = codeProvider.CompileAssemblyFromSource(options, sources);

            //any errors during compilation?
            if (cr.Errors.Count > 0)
            {
                // Display compilation errors
                StringBuilder fullErrorText = new StringBuilder();

                foreach (CompilerError error in cr.Errors)
                {
                    fullErrorText.AppendLine(error.ToString());
                }

                // throw new InvalidOperationException("ManagementPack sealing failed with error :" + fullErrorText.ToString());
                throw new InvalidOperationException(mpoutputname);
            }

            codeProvider.Dispose();
            return (options.OutputAssembly);
            #endregion
        }

        internal const int ChunkSize = 4096;
        internal const string AssemblyManagementPackResourceName = "ManagementPack";
        internal const string AssemblyManagementPackResourcePackageName = "MPResources.resources";

        private static string AssemblyAttributesCodeTemplate =
                @"using System.Reflection;
                  using System.Runtime.CompilerServices;
                  [assembly: AssemblyVersion(""{0}"")]
                  [assembly: AssemblyCompany(""{1}"")]
                  [assembly: AssemblyProduct(""{2}"")]
                  [assembly: AssemblyCopyright(""{3}"")]";

        #endregion

        #region File Extension constants
        internal static string XmlExtension = ".xml";
        internal static string MpbExtension = ".mpb";
        internal static string AssemblyExtension = ".mp";
        internal static bool isMpb = false;
        #endregion

    }

    /// <summary>
    /// This class is used as input to ManagementPackAssemblyWriter - to seal ManagementPacks
    /// Settings that can be specified for the ManagementPackAssemblyWriter
    /// </summary>
    public class FastAssemblyWriterSettings
    {
        /// <summary>
        /// Constructor to create a ManagementPackAssemblyWriterSettings 
        /// </summary>
        /// <param name="companyName">Name of the company</param>
        /// <param name="keyFilePath">The Keypair file (.snk) that has a key pair for signing the ManagementPack assembly. Usually created with sn.exe utility from .NET Framework</param>
        /// <param name="productName">Name of the Product that this ManagementPack Monitors (including version of the product) Example: Microsoft SQL Server 2005 ManagementPack</param>
        /// <param name="delaySign">Should the assemlby be delay signed?</param>
        public FastAssemblyWriterSettings(string companyName, string keyFilePath, bool delaySign)
        {
            //validate company name
            if (string.IsNullOrEmpty(companyName))
            {
                throw new ArgumentNullException("companyName");
            }
            this.companyName = companyName;

            //validate keyfile path
            if (string.IsNullOrEmpty(keyFilePath))
            {
                throw new ArgumentNullException("keyFilePath");
            }
            this.keyFilePath = keyFilePath;

            //initalize the output directory to be the default (current) directory
            this.OutputDirectory = ".";

            this.delaySign = delaySign;
        }


        #region Properties

        /// <summary>
        /// A value of the CompanyName Assembly attribute - to set on the sealed ManagementPack Assembly
        /// </summary>
        public string CompanyName
        {
            get { return this.companyName; }
        }

        /// <summary>
        /// A value of the ProductName Assembly attribute - to set on the sealed ManagementPack Assembly
        /// </summary>
        public string ProductName
        {
            get { return this.productName; }
            internal set { this.productName = value; }
        }
        /// <summary>
        /// A value of the Copyright Assembly attribute - to set on the sealed ManagementPack Assembly
        /// </summary>
        public string Copyright
        {
            get { return copyright; }
            set { this.copyright = value; }
        }

        /// <summary>
        /// Path to the Key pair to sign the ManagementPack with
        /// </summary>
        public string KeyFilePath
        {
            get { return this.keyFilePath; }
        }

        /// <summary>
        /// DelaySign flag
        /// </summary>
        public bool DelaySign
        {
            get { return (this.delaySign); }
            set { this.delaySign = value; }
        }

        /// <summary>
        /// The directory to write the assembly out to
        /// </summary>
        public string OutputDirectory
        {
            get { return (this.outputDirectory); }
            set
            {
                this.outputDirectory = value;

                //validate that the specified output directory exists
                DirectoryInfo info = null;
                try
                {
                    info = new DirectoryInfo(this.outputDirectory);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Output directory" + this.OutputDirectory + " is not accessible", e);
                }

                if (info == null || (info.Exists == false))
                {
                    throw new DirectoryNotFoundException("Output directory " + this.outputDirectory + " does not exist");
                }
            }
        }

        /// <summary>
        /// The version of the Assembly being written
        /// </summary>
        internal Version AssemblyVersion
        {
            get { return (this.version); }
            set { this.version = value; }
        }


        #endregion

        #region Private Fields
        private string outputDirectory;
        private string companyName;
        private string productName;
        private string copyright;
        private string keyFilePath;
        private bool delaySign;
        private Version version;
        #endregion

    }
    #endregion

    [Cmdlet("New", "SCSMManagementPackReference")]
    public class NewSCSMManagementPackReference : SMCmdletBase
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private string _alias;
        private ManagementPack _managementpack;

        [Parameter(Mandatory = true, ValueFromPipeline = false)]
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ManagementPack ManagementPack
        {
            get { return _managementpack; }
            set { _managementpack = value; }
        }
        
        protected override void ProcessRecord()
        {
            try
            {
                ManagementPackReference mpref = new ManagementPackReference(_managementpack);
                KeyValuePair<string, ManagementPackReference> kvp = new KeyValuePair<string, ManagementPackReference>(_alias, mpref);
                WriteObject(kvp);
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "Error", ErrorCategory.InvalidOperation, Alias));
            }
        }
    }

    [Cmdlet("Get", "SCSMManagementPackReference", DefaultParameterSetName="NAME")]
    public class GetSCSMManagementPackReference : SMCmdletBase
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
      
        private ManagementPack _managementpack;

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public ManagementPack ManagementPack
        {
            get { return _managementpack; }
            set { _managementpack = value; }
        }
        private string [] _alias = { "*" };
        [Parameter(ParameterSetName="ALIAS")]
        [ValidateNotNullOrEmpty]
        public string [] Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }
        private string [] _name = { "*" };
        [Parameter(ParameterSetName="NAME")]
        public string [] Name
        {
            get { return _name; }
            set { _name = value; }
        }

        protected override void ProcessRecord()
        {
            try
            {
                foreach (KeyValuePair<string,ManagementPackReference> mpref in _managementpack.References)
                {
                    if (ParameterSetName == "NAME")
                    {
                        foreach(string s in Name)
                        {
                            WildcardPattern wp = new WildcardPattern(s, WildcardOptions.CultureInvariant|WildcardOptions.IgnoreCase);
                            if ( wp.IsMatch(mpref.Value.Name) )
                            {
                                PSObject o = new PSObject(mpref.Value);
                                o.Members.Add(new PSNoteProperty("Alias", mpref.Key));
                                WriteObject(o);
                            }
                        }
                    }
                    else
                    {
                        foreach(string s in Alias)
                        {
                            WildcardPattern wp = new WildcardPattern(s, WildcardOptions.CultureInvariant|WildcardOptions.IgnoreCase);
                            if ( wp.IsMatch(mpref.Key) )
                            {
                                PSObject o = new PSObject(mpref.Value);
                                o.Members.Add(new PSNoteProperty("Alias", mpref.Key));
                                WriteObject(o);
                            }
                        }
                    }
                    // WriteObject(mpref);
                }
            }
            catch (Exception e)
            {
                ThrowTerminatingError(new ErrorRecord(e, "Error", ErrorCategory.InvalidOperation, _managementpack.Name));
            }
        }
    }

    #region New-SCManagementPackBundle
    // Implementation of New-SCSMManagementPackBundle
    // Creates a new management pack bundle (MPB)
    // New-SCManagementPackBundle already exist in Microsoft.EnterpriseManagement.Core.Cmdlets, so name changed
    [Cmdlet(VerbsCommon.New, "SCSMManagementPackBundle")]
    public class NewManagementPackBundleCommand : SMCmdletBase
    {
        private string _name = "";
        [Parameter(Position=0,Mandatory = true, HelpMessage = "Name of MPB file to create")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string[] _mpFileNames = { };
        [Parameter(Mandatory=true, HelpMessage="Full path to management pack(s), XML or MP allowed")]
        public string[] ManagementPackFiles
        {
            get { return _mpFileNames; }
            set { _mpFileNames = value; }
        }

        private string _outDir = "";
        [Parameter(Mandatory = false, HelpMessage = "Folder where new MPB will be created. Current directory by default")]
        public string OutputDir
        {
            get { return _outDir; }
            set { _outDir = value; }
        }

        protected override void ProcessRecord()
        {
            List<FileInformation> _mpsList = new List<FileInformation>();

            if (System.IO.File.Exists(this.Name))
            {
                new ArgumentException("The Name parameter must be a bandle Name, not a file name");
            }

            foreach (string file in this.ManagementPackFiles)
            {
                if (System.IO.File.Exists(file) && (new string[] { ".xml", ".mp" }).Contains((new System.IO.FileInfo(file)).Extension))
                    _mpsList.Add(new FileInformation(file));
                else
                {
                    this.WriteWarning(string.Format("Management pack {0} not found or not .mp either .xml", file));
                }
            }

            if (_mpsList.Count > 0)
            {
                this.WriteVerbose("\tStart BuildBundel process...");
                

                ManagementPackBundle newBandle = ManagementPackBundleFactory.CreateBundle();
                if (string.IsNullOrEmpty(this.OutputDir))
                    this.WriteVerbose("\tOutput folder not specified");

                foreach (FileInformation mp in _mpsList)
                {
                    this.WriteVerbose("\tProcess management pack " + mp.FileName + " ...");
                    //string[] includePaths = new string[] { @"c:\Program Files (x86)\Microsoft System Center\Service Manager 2010 Authoring\Library\" };
                    ManagementPack mpObj = new ManagementPack(mp.FullPath);
                    newBandle.AddManagementPack(mpObj);
                    ManagementPackElementCollection<ManagementPackResource> allResources = mpObj.GetResources<ManagementPackResource>();
                    foreach (ManagementPackResource resource in allResources)
                    {
                        this.WriteVerbose("\tProcess resource file " + resource.FileName + " ...");
                        FileInfo[] foundedFiles = mp.Info.Directory.GetFiles(resource.FileName);
                        FileInfo fileToAdd = foundedFiles.Length > 0 ? foundedFiles[0] : null;
                        if (fileToAdd != null)
                        {
                            Stream curStr = null;
                            try
                            {
                                curStr = fileToAdd.Open(FileMode.Open, FileAccess.Read);
                                newBandle.AddResourceStream(mpObj, resource.Name, curStr, ManagementPackBundleStreamSignature.Empty);
                            }
                            catch (Exception er)
                            {
                                if (curStr != null)
                                {
                                    curStr.Close();
                                    curStr.Dispose();
                                }
                                Console.WriteLine("\tCouldn't process file: " + resource.FileName);
                                Console.WriteLine("\tError: " + er.Message);
                                return;
                            }
                        }
                        else
                        {
                            new Exception("Resource file not found: " + resource.FileName);
                        }
                    }

                }
                try
                {
                    ManagementPackBundleWriter bundleWriter = ManagementPackBundleFactory.CreateBundleWriter(string.IsNullOrEmpty(this.OutputDir) ? _mpsList[0].Info.DirectoryName : this.OutputDir);
                    string ret = bundleWriter.Write(newBandle, this.Name);

                    Console.WriteLine("");
                    Console.WriteLine("\tBundle successfully saved!");
                    Console.WriteLine("");
                    WriteObject(new FileInfo(ret));
                }
                catch (Exception er)
                {
                    throw er;
                }
                finally
                {
                    foreach (ManagementPack mp in newBandle.ManagementPacks)
                    {
                        foreach (Stream str in newBandle.GetStreams(mp).Values)
                        {
                            str.Close();
                            str.Dispose();
                        }
                    }
                }
            }
            else
            {
                new Exception("Error: Management packs not found.");
            }
        }
    }
    #endregion
}
