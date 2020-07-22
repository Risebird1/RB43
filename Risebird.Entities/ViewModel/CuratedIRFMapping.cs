namespace Risebird.Entities.ViewModel
{
    public class CuratedIRFMapping
    {
        public int id { get; set; }
        public int CuratedGroupId { get; set; }
        public int IRFId { get; set; }
        public bool IsAdded { get; set; }
        public string IRFName { get; set; }
        public string PrimarySkills { get; set; }
        public string CuratedGroupName { get; set; }
    }
}
