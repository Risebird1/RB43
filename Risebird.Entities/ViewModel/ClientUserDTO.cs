namespace Risebird.Entities.ViewModel
{
    public class ClientUserDTO
    {
        public int id { get; set; }
        public int ClientId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
    public class ClientVM
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class ClientRoleVM
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public class TransClientUser
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int ClientUserId { get; set; }
        public int ClientRoleId { get; set; }
        public string EmailId { get; set; }

        public string Role { get; set; }
        public string uniqueid { get; set; }
        public string IRFName { get; set; }
        public string FirstName { get; set; }
    }
}
