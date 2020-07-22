using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class CuratedIndexVM
    {
        public string Message { get; set; }
        public IEnumerable<CuratedUserGroupData> CuratedData { get; set; }
    }
}
