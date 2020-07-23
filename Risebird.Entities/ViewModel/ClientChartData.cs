using System;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class ClientChartData
    {
        public string Name { get; set; }
        public int NoShowCount { get; set; }
        public int ScheduledCount { get; set; }
        public int DroppedCount { get; set; }
        public int CompletedCount { get; set; }

    }
}
