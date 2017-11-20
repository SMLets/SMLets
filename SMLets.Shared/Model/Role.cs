using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using Microsoft.EnterpriseManagement.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;

namespace SMLets.Model.Security
{
    public enum UserRoleTypeEnum
    {
        ActivityImplementer,
        AdvancedOperator,
        Author,
        ChangeInitiator,
        ChangeManager,
        EndUser,
        IncidentResolver,
        ProblemAnalyst,
        ReadOnlyOperator,
        ReleaseManager,
        ServiceRequestAnalyst
    }


    public class Role
    {
        public static readonly Guid System_CatalogItem = new Guid("E5151BC2-14B3-A138-DC31-9EF1FBC34728");
        public static readonly Guid System_ConfigItem = new Guid("62F0BE9F-ECEA-E73C-F00D-3DD78A7422FC");
        public static readonly Guid System_WorkItem = new Guid("F59821E2-0364-ED2C-19E3-752EFBB1ECE9");

        private EnterpriseManagementObject[] catalogGroup;
        private ManagementPackClass[] classes;
        private string description;
        private bool existing;
        private ManagementPackObjectTemplate[] formtemplate;
        private EnterpriseManagementObject[] group;
        private EnterpriseManagementGroup managementGroup;
        private string name;
        private Profile profile;
        private EnterpriseManagementObject[] queue;
        private UserRoleScope scope;
        private ManagementPackConsoleTask[] task;
        private string[] user;
        private Microsoft.EnterpriseManagement.Security.UserRole userRole;
        private ManagementPackView[] view;

        public Role(Microsoft.EnterpriseManagement.Security.UserRole existingRole)
        {
            if (existingRole == null)
            {
                throw new ArgumentNullException("existingRole");
            }
            this.existing = true;
            this.managementGroup = existingRole.ManagementGroup;
            this.userRole = existingRole;
            this.profile = existingRole.Profile;
            this.scope = existingRole.Scope;
            this.RetrieveValues();
        }

        public Role(EnterpriseManagementGroup mg, UserRoleTypeEnum roleType)
        {
            if (mg == null)
            {
                throw new ArgumentNullException("mg");
            }
            this.existing = false;
            this.managementGroup = mg;
            this.userRole = new Microsoft.EnterpriseManagement.Security.UserRole();
            this.userRole.Name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", new object[] { "UserRole", Guid.NewGuid() });
            this.profile = this.GetProfile(mg, roleType.ToString());
            this.scope = new UserRoleScope();
        }

        protected void AssignValues()
        {
            if ((this.scope != null) && !this.scope.IsScopeFixed)
            {
                this.userRole.DisplayName = this.DisplayName;
                this.userRole.Description = this.Description;
                if (this.IsAuthorProfile())
                {
                    this.scope.Classes.Clear();
                    if (this.AllClasses)
                    {
                        this.scope.Classes.Add(UserRoleScope.RootClassId);
                    }
                    else if (this.Classes != null)
                    {
                        foreach (ManagementPackClass class2 in this.Classes)
                        {
                            this.scope.Classes.Add(class2.Id);
                        }
                    }
                }
                this.scope.Objects.Clear();
                if ((this.AllQueues && this.AllGroups) && this.AllCatalogGroups)
                {
                    this.scope.Objects.Add(UserRoleScope.RootObjectId);
                }
                else
                {
                    if (this.AllQueues)
                    {
                        this.scope.Objects.Add(Role.System_WorkItem);
                    }
                    else if (this.Queue != null)
                    {
                        foreach (EnterpriseManagementObject obj2 in this.Queue)
                        {
                            if (!this.scope.Objects.Contains(obj2.Id))
                            {
                                this.scope.Objects.Add(obj2.Id);
                            }
                        }
                    }
                    if (this.AllGroups)
                    {
                        this.scope.Objects.Add(Role.System_ConfigItem);
                    }
                    else if (this.Group != null)
                    {
                        foreach (EnterpriseManagementObject obj3 in this.Group)
                        {
                            if (!this.scope.Objects.Contains(obj3.Id))
                            {
                                this.scope.Objects.Add(obj3.Id);
                            }
                        }
                    }
                    if (this.AllCatalogGroups)
                    {
                        this.scope.Objects.Add(Role.System_CatalogItem);
                    }
                    else if (this.CatalogGroup != null)
                    {
                        foreach (EnterpriseManagementObject obj4 in this.CatalogGroup)
                        {
                            if (!this.scope.Objects.Contains(obj4.Id))
                            {
                                this.scope.Objects.Add(obj4.Id);
                            }
                        }
                    }
                    Guid guid = new Guid("F4ED5777-BF37-BF1F-E947-0470A69FFA39");
                    Guid guid2 = new Guid("D37F9D4F-D641-A28C-FBAE-0AFDA6DC64F0");
                    Guid guid3 = new Guid("59C5493A-C00C-90FD-1F8C-73DBB7578E6C");
                    Guid[] guidArray = new Guid[] { guid, guid2, guid3 };
                    foreach (Guid guid4 in guidArray)
                    {
                        if (!this.scope.Objects.Contains(guid4))
                        {
                            this.scope.Objects.Add(guid4);
                        }
                    }
                }
                if (!this.IsEndUserProfile())
                {
                    this.scope.ConsoleTasks.Clear();
                    if (this.AllTasks)
                    {
                        this.scope.ConsoleTasks.Add(UserRoleScope.RootConsoleTaskId);
                    }
                    else if (this.Task != null)
                    {
                        foreach (ManagementPackConsoleTask task in this.Task)
                        {
                            this.scope.ConsoleTasks.Add(task.Id);
                        }
                        Guid item = new Guid("C8F03F1E-A453-8559-0EB0-AB2E457F8785");
                        if (!this.scope.ConsoleTasks.Contains(item))
                        {
                            this.scope.ConsoleTasks.Add(item);
                        }
                    }
                    this.scope.Views.Clear();
                    if (this.AllViews)
                    {
                        this.scope.Views.Add(new Pair<Guid, bool>(UserRoleScope.RootViewId, false));
                    }
                    else if (this.View != null)
                    {
                        foreach (ManagementPackView view in this.View)
                        {
                            this.scope.Views.Add(new Pair<Guid, bool>(view.Id, false));
                        }
                        List<Guid> list = new List<Guid> {
                            new Guid("34837DA4-A2A7-91B1-8120-214A8C563D76"),
                            new Guid("32DEB530-FE11-A6F2-37AC-0820A62074D6"),
                            new Guid("CDF4CD8A-1B37-F475-AEC4-BF01A77B8155"),
                            new Guid("2F8A2ECD-EAEF-5484-1D3D-1E9E7596C253"),
                            new Guid("3E68ABA7-38EA-B9D3-96EE-15B6DEE092AB"),
                            new Guid("46885C44-510F-2CE9-6EAA-F6BFE0D213A1"),
                            new Guid("C66D70D2-40E0-728A-E169-E84E90FD515A")
                        };
                        foreach (Guid guid6 in list)
                        {
                            Pair<Guid, bool> pair = new Pair<Guid, bool>(guid6, false);
                            if (!this.scope.Views.Contains(pair))
                            {
                                this.scope.Views.Add(pair);
                            }
                        }
                    }
                }
                this.scope.Templates.Clear();
                if (this.AllFormTemplates)
                {
                    this.scope.Templates.Add(UserRoleScope.RootTemplateId);
                }
                else if (this.FormTemplate != null)
                {
                    foreach (ManagementPackObjectTemplate template in this.FormTemplate)
                    {
                        this.scope.Templates.Add(template.Id);
                    }
                }
            }
            this.userRole.Users.Clear();
            if (this.User != null)
            {
                foreach (string str in this.User)
                {
                    this.userRole.Users.Add(str);
                }
            }
        }

        public void Commit()
        {
            this.AssignValues();
            if (this.existing)
            {
                this.managementGroup.Security.UpdateUserRoles(new Microsoft.EnterpriseManagement.Security.UserRole[] { this.userRole });
            }
            else
            {
                this.userRole.Scope = this.scope;
                this.userRole.Profile = this.profile;
                this.managementGroup.Security.InsertUserRole(this.userRole);
                this.existing = true;
            }
        }

        private EnterpriseManagementObject[] GetCatalogGroups() =>
            this.GetScopeItems(new Guid("0F5DFFF7-D5D0-F79D-8716-9FA23589C900"));

        private ManagementPackConsoleTask[] GetConsoleTasks() =>
            (from t in this.managementGroup.TaskConfiguration.GetConsoleTasks()
             where this.scope.ConsoleTasks.Contains(t.Id)
             select t).ToArray<ManagementPackConsoleTask>();

        private EnterpriseManagementObject[] GetGroups() =>
            this.GetScopeItems(new Guid("E652CFD7-91AD-784A-552F-60458A0B3053"));

        private Profile GetProfile(EnterpriseManagementGroup mg, string name) =>
            (from p in mg.Security.GetProfiles()
             where string.Compare(p.Name, name, StringComparison.OrdinalIgnoreCase) == 0
             select p).FirstOrDefault<Profile>();

        private EnterpriseManagementObject[] GetQueues() =>
            this.GetScopeItems(new Guid("31D729D9-83C5-5B36-703B-C51D54395687"));

        private EnterpriseManagementObject[] GetScopeItems(Guid type)
        {
            ManagementPackClass managementPackClass = this.managementGroup.EntityTypes.GetClass(type);
            return (from q in this.managementGroup.EntityObjects.GetObjectReader<EnterpriseManagementObject>(managementPackClass, ObjectQueryOptions.Default)
                    where this.scope.Objects.Contains(q.Id)
                    select q).ToArray<EnterpriseManagementObject>();
        }

        private ManagementPackObjectTemplate[] GetTemplates() =>
            (from t in this.managementGroup.Templates.GetObjectTemplates()
             where this.scope.Templates.Contains(t.Id)
             select t).ToArray<ManagementPackObjectTemplate>();

        private ManagementPackView[] GetViews() =>
            (from t in this.managementGroup.Presentation.GetViews()
             where this.scope.Views.Any<Pair<Guid, bool>>(delegate (Pair<Guid, bool> p) {
                 if (p.First == t.Id)
                 {
                     return !p.Second;
                 }
                 return false;
             })
             select t).ToArray<ManagementPackView>();

        private bool IsAuthorProfile() =>
            ((this.profile != null) && (string.Compare(this.profile.Name, UserRoleTypeEnum.Author.ToString(), StringComparison.OrdinalIgnoreCase) == 0));

        private bool IsEndUserProfile() =>
            ((this.profile != null) && (string.Compare(this.profile.Name, UserRoleTypeEnum.EndUser.ToString(), StringComparison.OrdinalIgnoreCase) == 0));

        public void Remove()
        {
            this.managementGroup.Security.DeleteUserRole(this.userRole);
            this.existing = false;
        }

        protected void RetrieveValues()
        {
            this.name = this.userRole.DisplayName;
            this.description = this.userRole.Description;
            if ((this.scope != null) && ((this.scope.Classes != null) & this.scope.Classes.Any<Guid>()))
            {
                if (this.scope.Classes.Contains(UserRoleScope.RootClassId))
                {
                    this.AllClasses = true;
                }
                else
                {
                    this.classes = this.managementGroup.EntityTypes.GetClasses(this.scope.Classes).ToArray<ManagementPackClass>();
                }
            }
            else
            {
                this.classes = new ManagementPackClass[0];
            }
            if (((this.scope != null) && (this.scope.Objects != null)) && this.scope.Objects.Any<Guid>())
            {
                if (this.scope.Objects.Contains(UserRoleScope.RootObjectId))
                {
                    this.AllQueues = true;
                    this.AllGroups = true;
                    this.AllCatalogGroups = true;
                }
                else
                {
                    if (this.scope.Objects.Contains(Role.System_WorkItem))
                    {
                        this.AllQueues = true;
                    }
                    else
                    {
                        this.queue = this.GetQueues();
                    }
                    if (this.scope.Objects.Contains(Role.System_ConfigItem))
                    {
                        this.AllGroups = true;
                    }
                    else
                    {
                        this.group = this.GetGroups();
                    }
                    if (this.scope.Objects.Contains(Role.System_CatalogItem))
                    {
                        this.AllCatalogGroups = true;
                    }
                    else
                    {
                        this.catalogGroup = this.GetCatalogGroups();
                    }
                }
            }
            else
            {
                this.group = new EnterpriseManagementObject[0];
                this.queue = new EnterpriseManagementObject[0];
                this.catalogGroup = new EnterpriseManagementObject[0];
            }
            if (((this.scope != null) && (this.scope.ConsoleTasks != null)) && this.scope.ConsoleTasks.Any<Guid>())
            {
                if (this.scope.ConsoleTasks.Contains(UserRoleScope.RootConsoleTaskId))
                {
                    this.AllTasks = true;
                }
                else
                {
                    this.task = this.GetConsoleTasks();
                }
            }
            else
            {
                this.task = new ManagementPackConsoleTask[0];
            }
            if (((this.scope != null) && (this.scope.Views != null)) && this.scope.Views.Any<Pair<Guid, bool>>())
            {
                if (this.scope.Views.Contains(new Pair<Guid, bool>(UserRoleScope.RootViewId, false)))
                {
                    this.AllViews = true;
                }
                else
                {
                    this.view = this.GetViews();
                }
            }
            else
            {
                this.view = new ManagementPackView[0];
            }
            if (((this.scope != null) && (this.scope.Templates != null)) && this.scope.Templates.Any<Guid>())
            {
                if (this.scope.Templates.Contains(UserRoleScope.RootTemplateId))
                {
                    this.AllFormTemplates = true;
                }
                else
                {
                    this.formtemplate = this.GetTemplates();
                }
            }
            else
            {
                this.formtemplate = new ManagementPackObjectTemplate[0];
            }
            if ((this.userRole.Users != null) && this.userRole.Users.Any<string>())
            {
                this.user = new string[this.userRole.Users.Count];
                this.userRole.Users.CopyTo(this.user, 0);
            }
            else
            {
                this.user = new string[0];
            }
        }

        private void VerifyUpdateAction()
        {
            if ((this.scope != null) && this.scope.IsScopeFixed)
            {
                throw new InvalidOperationException("You can't update build-in roles");
            }
        }

        public bool AllCatalogGroups { get; set; }

        public bool AllClasses { get; set; }

        public bool AllFormTemplates { get; set; }

        public bool AllGroups { get; set; }

        public bool AllQueues { get; set; }

        public bool AllTasks { get; set; }

        public bool AllViews { get; set; }

        public EnterpriseManagementObject[] CatalogGroup
        {
            get =>
                this.catalogGroup;
            set
            {
                this.VerifyUpdateAction();
                this.AllCatalogGroups = false;
                this.catalogGroup = value;
            }
        }

        public ManagementPackClass[] Classes
        {
            get =>
                this.classes;
            set
            {
                this.VerifyUpdateAction();
                this.AllClasses = false;
                this.classes = value;
            }
        }

        public string Description
        {
            get =>
                this.description;
            set
            {
                this.VerifyUpdateAction();
                this.description = value;
            }
        }

        public string DisplayName
        {
            get =>
                this.name;
            set
            {
                this.VerifyUpdateAction();
                this.name = value;
            }
        }

        public ManagementPackObjectTemplate[] FormTemplate
        {
            get =>
                this.formtemplate;
            set
            {
                this.VerifyUpdateAction();
                this.AllFormTemplates = false;
                this.formtemplate = value;
            }
        }

        public EnterpriseManagementObject[] Group
        {
            get =>
                this.group;
            set
            {
                this.VerifyUpdateAction();
                this.AllGroups = false;
                this.group = value;
            }
        }

        public EnterpriseManagementObject[] Queue
        {
            get =>
                this.queue;
            set
            {
                this.VerifyUpdateAction();
                this.AllQueues = false;
                this.queue = value;
            }
        }

        public ManagementPackConsoleTask[] Task
        {
            get =>
                this.task;
            set
            {
                this.VerifyUpdateAction();
                this.AllTasks = false;
                this.task = value;
            }
        }

        public string[] User
        {
            get =>
                this.user;
            set
            {
                this.user = value;
            }
        }

        public Microsoft.EnterpriseManagement.Security.UserRole UserRole =>
            this.userRole;

        public ManagementPackView[] View
        {
            get =>
                this.view;
            set
            {
                this.VerifyUpdateAction();
                this.AllViews = false;
                this.view = value;
            }
        }
    }
}
