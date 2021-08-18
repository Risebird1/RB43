using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class CalendlyEventsListResponse
    {
        public List<CalendlyEventDetails> collection { get; set; }
        public CalendlyEvenListPagination pagination { get; set; }
    }
}
