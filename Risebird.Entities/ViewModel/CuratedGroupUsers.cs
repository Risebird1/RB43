namespace Risebird.Entities.ViewModel
{
    public class CuratedGroupUsers
    {
        public int id { get; set; }
        public string CuratedGroupName { get; set; }
        public int UserId { get; set; }
        public string EmailId { get; set; }
        public int CuratedGroupId { get; set; }
        public bool IsAdded { get; set; }
    }
}
