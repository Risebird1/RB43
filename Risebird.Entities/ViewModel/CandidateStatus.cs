using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel;

namespace Risebird.Entities.ViewModel
{
    public class CandidateStatus
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }

        public string AudioFileLink { get; set; }
        public string Comments { get; set; }
        public int Status { get; set; }
        public string StatusValue { get; set; }
        public DateTime Date { get; set; }

        [DisplayName("Skill Channel")]

        public string IRFName { get; set; }

        [DisplayName("Candidate Name")]

        public string CandidateName { get; set; }

        [DisplayName("Upload Audio")]
        public IFormFile AudioFile { get; set; }
        public string PerformedBy { get; set; }
        public bool IsManuallyDeletable { get; set; }

        public int? InterviewId { get; set; }
        public int? MeetingStatus { get; set; }
        public int? confirmationStatus { get; set; }

    }
}
