using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CuratedIndexVM
    {
        public string Message { get; set; }
        public IEnumerable<CuratedUserGroupData> CuratedData { get; set; }
    }
}
