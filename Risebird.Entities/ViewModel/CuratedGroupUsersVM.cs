using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CuratedGroupUsersVM
    {
        public string CuratedGroupName { get; set; }
        public int CuratedGroupId { get; set; }
        public IList<CuratedGroupUsers> CuratedGroupUserList { get; set; }
    }
}
