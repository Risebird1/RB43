using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class InterviewerResumeFitmentAnalysisSchema
    {
        public int fitmentScore { get; set; }
        public string completeFitmentAnalysis { get; set; }
        public string verdict { get; set; }
    }
}
