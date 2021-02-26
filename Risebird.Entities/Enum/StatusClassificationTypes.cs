using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum StatusClassificationTypes
    {
        [Description("Meeting Status")]
        MeetingStatusType=1,

        [Description("Interview Status")]
        InterviewStatusType = 2,

        [Description("QC Status")]
        QCStatusType = 3,
    }
}
