using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class TranscriptAnalysisSchema
    {
        public int score { get; set; }
        public string completeAnalysis { get; set; }
        public string verdict { get; set; }
    }
}
