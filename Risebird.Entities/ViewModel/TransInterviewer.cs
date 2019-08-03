using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewer
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int UserId { get; set; }
        public int InterviewerId { get; set; }
        public string PrimarySkill { get; set; }
    }
}
