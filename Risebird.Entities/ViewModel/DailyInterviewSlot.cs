namespace Risebird.Entities.ViewModel
{
    public class DailyInterviewSlot
    {
        public string IRFId { get; set; }
        public string IRFnSkill { get; set; }
        public int Morning { get; set; }
        public int MorningUnassinged { get; set; }
        public int Evening { get; set; }
        public int EveningUnassinged { get; set; }

        public int Proxy { get; set; }
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public int BorderLine { get; set; }
        public int Pending { get; set; }
        public int total { get; set; }
        public int NoShowCount { get; set; }
        public int ScheduledCount { get; set; }
        public int DroppedCount { get; set; }
        //public int QCPending { get; set; }
    }
}
