using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class CuratedGroupUsersVM
    {
        public string CuratedGroupName { get; set; }
        public int CuratedGroupId { get; set; }
        public IList<CuratedGroupUsers> CuratedGroupUserList { get; set; }
    }
}
