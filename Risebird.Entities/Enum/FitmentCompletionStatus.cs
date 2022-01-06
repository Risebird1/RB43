using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum FitmentCompletionStatus
    {
        [Display(Name = "New")]
        [Description("New")]
        New = 1,

        [Display(Name = "In Progress")]
        [Description("In Progress")]
        InProgress = 2,

        [Display(Name = "Waiting For Response")]
        [Description("Waiting For Response")]
        WFR = 3,

        [Display(Name = "Irrelevant")]
        [Description("Irrelevant")]
        Irrelevant = 4,

        [Display(Name = "Duplicate")]
        [Description("Duplicate")]
        Duplicate = 5,

        [Display(Name = "Not Interested")]
        [Description("Not Interested")]
        NotInterested = 6,

        [Display(Name = "Got Another Offer")]
        [Description("Got Another Offer")]
        GotAnotherOffer = 7,

        [Display(Name = "Cold")]
        [Description("Cold")]
        Cold = 8,

        [Display(Name = "Hold")]
        [Description("Hold")]
        Hold = 9,

        [Display(Name = "Reminder Call")]
        [Description("Reminder Call")]
        ReminderCall = 10,

        [Display(Name = "Called")]
        [Description("Called")]
        Called = 11,

        [Display(Name = "Not Responding")]
        [Description("Not Responding")]
        NotResponding = 12,
    }
}
