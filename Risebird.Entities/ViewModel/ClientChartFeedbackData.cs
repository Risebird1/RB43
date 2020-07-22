using System;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class ClientChartFeedbackData
    {
        public string Name { get; set; }
        public int ProxyCount { get; set; }
        public int SelectedCount { get; set; }
        public int RejectedCount { get; set; }
        public int BorderLineCount { get; set; }
    }
}
