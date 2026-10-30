using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class OpsToolkitVM
    {
        public const string ToolEmergencyUnassign = "emergency-unassign";
        public const string ToolIRFScheduleLink = "irf-schedule-link";

        public static string NormalizeTool(string tool) =>
            string.Equals(tool, ToolIRFScheduleLink, StringComparison.OrdinalIgnoreCase) ? ToolIRFScheduleLink : ToolEmergencyUnassign;

        public string Tool { get; set; } = ToolEmergencyUnassign;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<OpsToolkitEmergencyUnassignVM> EmergencyUnassignments { get; set; } = new List<OpsToolkitEmergencyUnassignVM>();
        public List<OpsToolkitIRFScheduleLinkVM> MissingScheduleLinks { get; set; } = new List<OpsToolkitIRFScheduleLinkVM>();

        public List<OpsToolkitEmergencyUnassignVM> PenaltyApplied =>
            EmergencyUnassignments.Where(x => x.Category == "PenaltyApplied").ToList();

        public List<OpsToolkitEmergencyUnassignVM> EINoPenalty =>
            EmergencyUnassignments.Where(x => x.Category == "EINoPenalty").ToList();
    }

    public class OpsToolkitEmergencyUnassignVM
    {
        public string Category { get; set; }
        public int InterviewId { get; set; }
        public string IRFId { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public DateTime? InterviewDate { get; set; }
        public int? ConfirmationStatus { get; set; }
        public bool EIAssigned { get; set; }
        public string CurrentInterviewer { get; set; }
        public string UnassignedBy { get; set; }
        public int? UnassignedByUserId { get; set; }
        public string UnassignedByName { get; set; }
        public DateTime? UnassignedAt { get; set; }
        public int? MinutesBeforeInterview { get; set; }
        public bool IsEmergencyInterviewer { get; set; }
        public int? InterviewPointsId { get; set; }
        public int? PenaltyPoints { get; set; }
        public string PenaltyStatus { get; set; }
        public DateTime? PenaltyDate { get; set; }
        public bool CanRemovePenalty { get; set; }
        public int? RemovedPoints { get; set; }
        public string RemovedBy { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string RemovedReason { get; set; }
    }

    public class OpsToolkitIRFScheduleLinkVM
    {
        public int IRFIntId { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public string SkillCluster { get; set; }
        public string PrimarySkillInvite { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPaused { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string EventName { get; set; }
        public string EventUniqueId { get; set; }
        public bool IsEventActive { get; set; }
        public bool HasScheduleEvent { get; set; }
        public string SuggestedLink { get; set; }
    }

    public class OpsToolkitInterviewHistoryVM
    {
        public int InterviewId { get; set; }
        public List<OpsToolkitPointsEntryVM> Points { get; set; } = new List<OpsToolkitPointsEntryVM>();
        public List<OpsToolkitAssignmentChangeVM> Assignments { get; set; } = new List<OpsToolkitAssignmentChangeVM>();
        public List<OpsToolkitActionLogVM> Actions { get; set; } = new List<OpsToolkitActionLogVM>();
    }

    public class OpsToolkitPointsEntryVM
    {
        public int InterviewPointsId { get; set; }
        public int? UserId { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public int? Points { get; set; }
        public string Type { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public DateTime? Date { get; set; }
        public int? PenaltyAmount { get; set; }
        public int? QCPoints { get; set; }
        public int? PaymentHistoryId { get; set; }
        public bool CanRemovePenalty { get; set; }
    }

    public class OpsToolkitAssignmentChangeVM
    {
        public string Change { get; set; }
        public string FromInterviewer { get; set; }
        public string ToInterviewer { get; set; }
        public DateTime? PerformedAt { get; set; }
    }

    public class OpsToolkitActionLogVM
    {
        public string ActionType { get; set; }
        public int? InterviewPointsId { get; set; }
        public string EmailId { get; set; }
        public int? OldPoints { get; set; }
        public int? NewPoints { get; set; }
        public string Reason { get; set; }
        public string PerformedBy { get; set; }
        public DateTime? PerformedAt { get; set; }
    }

    public class OpsToolkitActionResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
