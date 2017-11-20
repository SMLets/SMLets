using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SMLets
{
    public static class Enumerations
    {
        public static string SystemWorkItemActionLogEnumFileAttached = "System.WorkItem.ActionLogEnum.FileAttached";
        public static string System_Announcement_PriorityEnum_Low = "System.Announcement.PriorityEnum.Low";
        public static string System_Announcement_PriorityEnum_Medium = "System.Announcement.PriorityEnum.Medium";
        public static string System_Announcement_PriorityEnum_Critical = "System.Announcement.PriorityEnum.Critical";
    }

    public static class ManagementPacks
    {
        public static string System_WorkItem_Library = "System.WorkItem.Library";
        public static string Microsoft_SystemCenter_InstanceGroup_Library = "Microsoft.SystemCenter.InstanceGroup.Library";
        public static string Microsoft_Windows_Library = "Microsoft.Windows.Library";
        public static string System_SupportingItem_Library = "System.SupportingItem.Library";
        public static string System_WorkItem_Incident_Library = "System.WorkItem.Incident.Library";
        public static string ServiceManager_IncidentManagement_Library = "ServiceManager.IncidentManagement.Library";
        public static string Microsoft_EnterpriseManagement_ServiceManager_UI_Console = "Microsoft.EnterpriseManagement.ServiceManager.UI.Console";
        public static string System_AdminItem_Library = "System.AdminItem.Library";
    }

    public static class RelationshipTypes
    {
        public static string System_WorkItemHasFileAttachment = "System.WorkItemHasFileAttachment";
        public static string System_WorkItem_TroubleTicketHasActionLog = "System.WorkItem.TroubleTicketHasActionLog";
        public static string System_WorkItem_TroubleTicketHasUserComment = "System.WorkItem.TroubleTicketHasUserComment";
        public static string System_WorkItem_TroubleTicketHasAnalystComment = "System.WorkItem.TroubleTicketHasAnalystComment";
        public static string System_WorkItemAboutConfigItem = "System.WorkItemAboutConfigItem";
        public static string System_WorkItemAffectedUser = "System.WorkItemAffectedUser";
        public static string System_WorkItemGroupContainsWorkItems = "System.WorkItemGroupContainsWorkItems";
        public static string Microsoft_SystemCenter_InstanceGroupContainsEntities = "Microsoft.SystemCenter.InstanceGroupContainsEntities";
    }

    public static class ClassTypes
    {
        public static string System_WorkItem_Incident = "System.WorkItem.Incident";
        public static string System_FileAttachment = "System.FileAttachment";
        public static string System_WorkItem_TroubleTicket_AnalystCommentLog = "System.WorkItem.TroubleTicket.AnalystCommentLog";
        public static string System_WorkItem_TroubleTicket_UserCommentLog = "System.WorkItem.TroubleTicket.UserCommentLog";
        public static string System_WorkItem_TroubleTicket_ActionLog = "System.WorkItem.TroubleTicket.ActionLog";
        public static string Microsoft_AD_User = "Microsoft.AD.User";
        public static string System_WorkItem_Incident_GeneralSetting = "System.WorkItem.Incident.GeneralSetting";
        public static string System_Announcement_Item = "System.Announcement.Item";
    }

    public static class TypeProjections
    {
        public static string System_WorkItem_Incident_ProjectionType = "System.WorkItem.Incident.ProjectionType";
    }

    public static class ClassProperties
    {
        public static string System_Domain_User__UserName = "UserName";
        public static string System_Domain_User__Domain = "Domain";
    }

    public static class Images
    {
        public static string Microsoft_EnterpriseManagement_ServiceManager_UI_Console_Image_Folder = "Microsoft.EnterpriseManagement.ServiceManager.UI.Console.Image.Folder";
    }
}
