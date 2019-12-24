using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class CandidateResumeEvaluation
    {
        public int id { get; set; }
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public int ResumeEvaluatorId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmailId { get; set; }
        public string ResumeLink { get; set; }
        public string IRFName { get; set; }
        public string JDLink { get; set; }
        public string RisebirdHR { get; set; }
        public string Phone { get; set; }

        public double JoinabilityScore { get; set; }
        public double SelectabilityScore { get; set; }
    }
}
