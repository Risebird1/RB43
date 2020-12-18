using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Data
{
    public class SpeechToTextDTO
    {
        public int InterviewId { get; set; }

        [Display(Name = "Upload Audio File")]

        public IFormFile AudioFile { get; set; }
        public string AudioFileUrl { get; set; }
        [Display(Name = "Skill Channel")]

        public string IRFName { get; set; }

        [Display(Name = "Interview Date")]
        public DateTime InterviewDate { get; set; }

        [Display(Name = "Candidate EmailId")]
        public string CandidateEmailId { get; set; }
        [Display(Name = "Candidate's Name")]

        public string CandidateName { get; set; }
        [Display(Name = "Keywords(comma separated)")]

        public string KeyWords { get; set; }
        public string ConvertedText { get; set; }
        public string UploadedAudioFileUrl { get; set; }
    }
}
