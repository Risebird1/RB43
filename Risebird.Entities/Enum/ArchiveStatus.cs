using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum ArchiveStatus
    {
        [Description("UnArchived")]
        UnArchived = 0,
        [Description("Archived")]
        Archived = 1,
        
    }
}
