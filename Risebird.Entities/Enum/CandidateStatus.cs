using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum CandidateStatus
    {
        [Description("New")]
        New =1,

        [Display(Name = "In Progress")]
        [Description("In Progress")]
        InProgress,

        [Description("WFR")]
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
        GotAnotherOffer


    }
}
