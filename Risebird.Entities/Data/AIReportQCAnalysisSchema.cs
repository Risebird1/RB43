using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class AIReportQCAnalysisSchema
    {
        public int final_score { get; set; }
        public string completeAnalysis { get; set; }
        public string final_verdict { get; set; }
    }
}
