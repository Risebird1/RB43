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

        [Display(Name = "Fraud Check Status")]
        public SelectList FraudCheckStatusList { get; set; }

        [Display(Name = "Training Status")]
        public SelectList TrainingStatusList { get; set; }

        [Display(Name = "Quality Control Status")]
        public SelectList QCStatusList { get; set; }

        [Display(Name = "Upload Audio")]
        public IFormFile AudioFile { get; set; }

        [Display(Name = "Upload Video")]
        public IFormFile VideoFile { get; set; }



    }
}
