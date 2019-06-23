namespace Risebird.Entities.Data
{
    public class Menu
    {
        public int id { get; set; }
        public string functionName { get; set; }
        public string Description { get; set; }
        public int iDisplayOrder { get; set; }
        public string sIcon { get; set; }
        public string sUrl { get; set; }
        public int idParentFunction { get; set; }
        
    }
}
