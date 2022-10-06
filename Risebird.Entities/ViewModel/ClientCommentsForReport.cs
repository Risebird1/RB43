using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ClientCommentsForReport
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string Profile { get; set; }

        [MaxLength(250)]
        public string Comment { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public int Rating { get; set; }
        public IEnumerable<ClientCommentsForReport> ClientCommentsList { get; set; }
        public string ClientActionOnInterviewer { get; set; }
        public bool allowClientAction { get; set; }
        public string Interviewer { get; set; }
        public DateTime interviewDate { get; set; }

    }
}
