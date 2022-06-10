using System;

namespace Risebird.Entities.ViewModel
{
    public class IRFFilterViewModel
    {
        public string IRFId { get; set; }
        public string ClientCompanyName { get; set; }
        public string PrimarySkill { get; set; }
        public string EndClientName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
