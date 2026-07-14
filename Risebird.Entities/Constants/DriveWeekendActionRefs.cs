using System;

namespace Risebird.Entities.Constants
{
    public static class DriveWeekendActionRefs
    {
        public const string RefType = "DriveWeekend";

        public static string BuildRefId(string irfId, DateTime weekendStart)
        {
            return $"{irfId}|{weekendStart:yyyy-MM-dd}";
        }
    }
}
