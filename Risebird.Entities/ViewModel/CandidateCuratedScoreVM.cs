using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class CandidateCuratedScoreVM
    {
        public IList<CandidateCuratedScore> CandidateCuratedScores { get; set; }
        public string Message { get; set; }
        public int IRFId { get; set; }
        public string IRFDetailsID { get; set; }
        public string candidateProfile { get; set; }
    }
}
