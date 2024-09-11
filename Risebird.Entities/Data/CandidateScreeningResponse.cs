using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class CandidateScreeningResponse
    {
        public string Verdict { get; set; }
        public List<string> Messages { get; set; }
        public string EvaluationResult { get; set; }
        public bool? Status { get; set; }
    }
}
