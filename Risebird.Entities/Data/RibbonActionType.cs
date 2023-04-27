using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public static class RibbonActionType
    {
        public static readonly string InterviewCount = "InterviewCount";
        public static readonly string MeetingStatus = "MeetingStatus";
        public static readonly string QcPending = "QcPending";
        public static readonly string UnAssignedCoordianter = "UnAssignedCoordianter";
        public static readonly string ZoomLink = "ZoomLink";
        public static readonly string UnAttendedZoomLink = "UnAttendedZoomLink";
        public static readonly string VideoLink = "VideoLink";
        public static readonly string QCPendingReport = "QCPendingReport";
        public static readonly string QCReport = "QCReport";
        public static readonly string NeedClarity = "NeedClarity";
        public static readonly string DeletedZoomMeetings = "DeletedZoomMeetings";
    }
}
