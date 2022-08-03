namespace Risebird.Entities.ViewModel
{
    public class TransVMUser
    {
        public int id { get; set; }
        public int VmId { get; set; }
        public int UserId { get; set; }
        public bool IsAdded { get; set; }
        public string EmailId { get; set; }
        public string profileName { get; set; }
        public int ProfileId { get; set; }
    }
}
