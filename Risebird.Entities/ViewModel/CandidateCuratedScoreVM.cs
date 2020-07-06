using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateCuratedScoreVM
    {
        public IList<CandidateCuratedScore> CandidateCuratedScores { get; set; }
        public string Message { get; set; }
        public int IRFId { get; set; }
    }
}
