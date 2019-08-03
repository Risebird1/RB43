using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum InterviewStatus
    {
        [Description("Proxy")]
        Proxy = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Border Line")]
        BorderLine = 4

    }

    public enum MeetingStatus
    {
        [Description("No Show")]
        NoShow = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Border Line")]
        BorderLine = 4

    }

    public enum OfferStatus
    {
        [Description("Offer Released")]
        OfferReleased = 1,
        [Description("Offer Accepted")]
        OfferAccepted = 2,
        [Description("Offer Denied")]
        OfferDenied = 3,
        [Description("Dropped")]
        Dropped = 4

    }

    public enum JoiningStatus
    {
        [Description("Joined")]
        Joining = 1,
        [Description("Not Joined")]
        NotJoining = 2
        
    }
    public enum FinalInterviewMeetingStatus
    {
        [Description("No Show")]
        NoShow = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Border Line")]
        BorderLine = 4

    }

    public enum PreparationStatus
    {
        [Description("Completed")]
        Completed = 1,
        [Description("Not Completed")]
        NotCompleted = 2
        
    }
    public enum L2InterviewStatus
    {
        [Description("Selected")]
        Selected = 1,
        [Description("Rejected")]
        Rejected = 2
    }

    public enum ClientInterviewStatus
    {

        [Description("Offered")]
        Offered = 1,
        [Description("Not Offered")]
        NotOffered = 2
    }

    public enum JobLevels
    {

        [Description("SE")]
        SE = 1,
        [Description("SSE")]
        SSE = 2,
        [Description("Lead")]
        Lead = 3,
        [Description("Manager")]
        Manager = 4,
        [Description("Architect")]
        Architect = 5
    }

    public enum JoinabilityType
    {

        [Description("Job Location")]
        JobLocation = 1,
        [Description("Approved Per Hour Rate")]
        ApprovedPerHourRate = 2,
        [Description("Approved Visa Type")]
        ApprovedVisaType = 3,
        [Description("Max Notice period (Days)")]
        MaxNoticeperiodDays = 4,
        
    }

    public enum SelectabilityType
    {

        [Description("Technology Stack")]
        TechnologyStack = 1,
        [Description("Career Progression")]
        CareerProgression = 2,
        [Description("Industry")]
        Industry = 3,
        [Description("Domain")]
        Domain = 4,
        [Description("Project")]
        Project = 5,
        [Description("Previous Employers")]
        PreviousEmployers = 6,
        [Description("Academics")]
        Academics = 7,
    }

    public enum SkillGeneralType
    {

        [Description("Logical")]
        Logical = 1,
        [Description("Analytical")]
        Analytical = 2,
        [Description("Communication")]
        Communication = 3
    }
}
