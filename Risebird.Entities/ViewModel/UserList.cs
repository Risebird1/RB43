namespace Risebird.Entities.ViewModel
{
    public class Users
    {
        public int id { get; set; }
        public int idProfile { get; set; }
        public string FirstName { get; set; }
        public string EmailId { get; set; }
        //public int RoleId { get; set; } // applicable only in case of the user being of a 'Client' profile
    }
}
