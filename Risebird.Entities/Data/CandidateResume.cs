using System;

namespace Risebird.Entities.Data
{
    public class CandidateResume
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public string ResumeLink { get; set; }
        public int AddedBy { get; set; }//Added By UserId
        public string AddedByEmailId { get; set; }
        public DateTime AddedOn { get; set; }
        public bool IsProcessed { get; set; }
        public bool IsResumeParsed { get; set; }
        public bool IsResumeParsingFailed { get; set; }
    }
}
