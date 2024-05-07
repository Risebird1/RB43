using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum CandidateStatusOptionsDrpdwn
    {
        [Description("New")]
        New = 1,

        [Display(Name = "In Progress")]
        [Description("In Progress")]
        InProgress = 2,

        [Description("Waiting For Response")]
        WFR = 3,

        //[Description("Scheduled")]
        //Scheduled = 4,

        [Description("Irrelevant")]
        Irrelevant = 5,

        [Description("Duplicate")]
        Duplicate = 6,

        [Display(Name = "Not Interested")]
        [Description("Not Interested")]
        NotInterested = 7,

        [Display(Name = "Got Another Offer")]
        [Description("Got Another Offer")]
        GotAnotherOffer = 8,

        [Display(Name = "Cold")]
        [Description("Cold")]
        Cold = 9,

        [Display(Name = "Hold")]
        [Description("Hold")]
        Hold = 10,

        //[Display(Name = "Invitation Sent")]
        //[Description("Invitation Sent")]
        //InvitationSent = 11,

        //[Display(Name = "Added")]
        //[Description("Added")]
        //Added = 12,

        [Display(Name = "Reminder Call")]
        [Description("Reminder Call")]
        ReminderCall = 13,

        //[Display(Name = "Meeting Status")]
        //[Description("Meeting Status")]
        //MeetingStatus=14,

        //[Display(Name = "Interview Status")]
        //[Description("Interview Status")]
        //InterviewStatus=15,

        [Display(Name = "Called")]
        [Description("Called")]
        Called = 16,

        [Display(Name = "Not Responding")]
        [Description("Not Responding")]
        NotResponding = 17,

        //[Display(Name = "Cancelled")]
        //[Description("Cancelled")]
        //Cancelled=18,

        //[Display(Name = "QC Pending")]
        //[Description("QC Pending")]
        //QCPending=19,

        [Display(Name = "Not Reachable")]
        [Description("Not Reachable")]
        NotReachable = 20,

        [Display(Name = "Wrong Number/Out of service")]
        [Description("Wrong Number/Out of service")]
        WrongNumber = 21,

        [Display(Name = "Will schedule by themselves")]
        [Description("Will schedule by themselves")]
        WillSchedule = 22,
    }
}
