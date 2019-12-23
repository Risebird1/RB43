using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class SelectabilityEvaluatorReport
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public string RequiredLevel { get; set; }
        public int RequiredLevelId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string ActualLevel { get; set; }
        public int ActualLevelId { get; set; }
        public int WeightageRequired { get; set; }
        public int WeightageActual { get; set; }
        public string Name { get; set; }
    }
}
