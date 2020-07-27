using System;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class MeetingBudgetChart
    {
        public DateTime Date { get; set; }
        public double Cost { get; set; }
    }
}
