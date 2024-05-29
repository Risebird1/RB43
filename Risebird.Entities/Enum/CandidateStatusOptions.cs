using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum CandidateStatusOptions
    {
        [Description("New")]
        New = 1,

        [Display(Name = "In Progress")]
        [Description("In Progress")]
        InProgress,

        [Description("Waiting For Response")]
        WFR,

        [Description("Scheduled")]
        Scheduled,

        [Description("Irrelevant")]
        Irrelevant,

        [Description("Duplicate")]
        Duplicate,

        [Display(Name = "Not Interested")]
        [Description("Not Interested")]
        NotInterested,

        [Display(Name = "Got Another Offer")]
        [Description("Got Another Offer")]
        GotAnotherOffer,
        [Display(Name = "Cold")]
        [Description("Cold")]
        Cold,
        [Display(Name = "Hold")]
        [Description("Hold")]
        Hold,
        [Display(Name = "Invitation Sent")]
        [Description("Invitation Sent")]
        InvitationSent,

        [Display(Name = "Added")]
        [Description("Added")]
        Added,

        [Display(Name = "Reminder Call")]
        [Description("Reminder Call")]
        ReminderCall,

        [Display(Name = "Meeting Status")]
        [Description("Meeting Status")]
        MeetingStatus,

        [Display(Name = "Interview Status")]
        [Description("Interview Status")]
        InterviewStatus,

        [Display(Name = "Called")]
        [Description("Called")]
        Called,
        [Display(Name = "Not Responding")]
        [Description("Not Responding")]
        NotResponding,

        [Display(Name = "Cancelled")]
        [Description("Cancelled")]
        Cancelled,

        [Display(Name = "QC Pending")]
        [Description("QC Pending")]
        QCPending,
        [Display(Name = "Not Reachable")]
        [Description("Not Reachable")]
        NotReachable,

        [Display(Name = "Wrong Number/Out of service")]
        [Description("Wrong Number/Out of service")]
        WrongNumber,

        [Display(Name = "Will schedule by themselves")]
        [Description("Will schedule by themselves")]
        WillSchedule,
    }
}
