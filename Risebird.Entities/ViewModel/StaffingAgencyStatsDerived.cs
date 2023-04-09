namespace Risebird.Entities.ViewModel
{
    public class StaffingAgencyStatsDerived : StatsVM
    {
        public string IRFnSkill { get; set; }
        public int TotalNoShow { get; set; }//all inclusive, both candidate and interviewer

    }
}
