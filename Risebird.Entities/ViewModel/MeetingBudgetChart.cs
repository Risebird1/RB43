using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class MeetingBudgetChart
    {
        public DateTime Date { get; set; }
        public double Cost { get; set; }
    }
}
