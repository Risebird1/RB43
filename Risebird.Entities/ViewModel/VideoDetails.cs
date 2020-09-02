using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VideoDetails
    {
        public int interviewid { get; set; }
        public string Candidate { get; set; }
        public int Count { get; set; }
        public int Videoid { get; set; }
        public string VideoUrl { get; set; }
        public string TimeSpend { get; set; }
        public string CandidateEmail { get; set; }
        public string CurrentTime { get; set; }
        public string UrlType { get; set; }
        public string IsIntrested { get; set; }
        public string ViewName { get; set; }
        public DateTime OnDate { get; set; }

    }
}
