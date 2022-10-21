using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClonableTemplateListVM
    {
        public ClonableTemplateListVM()
        {
            ClonableTemplates = new List<ClonableSkillChannelVM>();
        }
        public IEnumerable<ClonableSkillChannelVM> ClonableTemplates { get; set; }
        public string Message { get; set; }
        public string Profile { get; set; }
        public string LoggedInUser { get; set; }
        public int? ClientId { get; set; }
        public string ClientCompanyName { get; set; }
        public string Message2 { get; set; }
        public string AsmtGrpsPrSkillsMappedToCurrentTemplates { get; set; }
        public string IRFId { get; set; }
    }
}
