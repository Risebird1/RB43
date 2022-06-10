using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class CalendlyEventsListResponse
    {
        public List<CalendlyEventDetails> collection { get; set; }
        public CalendlyEvenListPagination pagination { get; set; }
    }
}
