using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace Risebird.Entities.Data
{
    public class InterviewComments
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public int? InterviewStatus { get; set; }
        public int? MeetingStatus { get; set; }
        public string AudioLink { get; set; }
        public string VideoLink { get; set; }
        [Display(Name ="Interviewer Comment")]
        public string InterviewerComment { get; set; }

        public int? FraudCheckStatus { get; set; }
        [Display(Name ="HR Comment")]
        public string HRComment { get; set; }

        public int? QCStatus { get; set; }
        [Display(Name ="Quality Control Comment")]
        public string QCComment { get; set; }

        public int? TrainingStatus { get; set; }
        [Display(Name ="Trainer Comment")]
        public string TrainerComment { get; set; }

        [Display(Name ="Interview Status")]
        public SelectList InterviewStatusList { get; set; }

        [Display(Name = "Meeting Status")]
        public SelectList MeetingStatusList { get; set; }

        [Display(Name = "Fraud Check Status")]
        public SelectList FraudCheckStatusList { get; set; }

        [Display(Name = "Training Status")]
        public SelectList TrainingStatusList { get; set; }

        [Display(Name = "Quality Control Status")]
        public SelectList QCStatusList { get; set; }

        [Display(Name = "L2 Interview Status")]
        public SelectList L2InterviewStatusList { get; set; }

        [Display(Name = "Final Interview Meeting Status")]
        public SelectList FinalInterviewMeetingStatusList { get; set; }
        [Display(Name = "Client Interview Status")]
        public SelectList ClientInterviewStatusList { get; set; }

        [Display(Name = "Joining Status")]
        public SelectList JoiningStatusList { get; set; }
        [Display(Name = "Preparation Status")]
        public SelectList PreparationStatusList { get; set; }

        [Display(Name = "Offer Status")]
        public SelectList OfferStatusList { get; set; }

        [Display(Name = "Upload Audio")]
        public IFormFile AudioFile { get; set; }

        [Display(Name = "Upload Video")]
        public IFormFile VideoFile { get; set; }
        public int? FinalInterviewMeetingStatus { get; set; }
        public int? OfferStatus { get; set; }
        public int? PreparationStatus { get; set; }
        public int? JoiningStatus { get; set; }
        public int? L2InterviewStatus { get; set; }
        public int? ClientInterviewStatus { get; set; }
        public int? IsSelected { get; set; }
        public int? IsApproved { get; set; }
        public string ThumbnailImage { get; set; }
        public int? TotalView { get; set; }
        public string IsShared { get; set; }

    }
}
