using System;

namespace Risebird.Web.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime RoundToNearSlot(this DateTime value, int intervalMinutes = 30)
        {
            if (intervalMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMinutes));

            // Remove seconds and milliseconds
            value = value.AddSeconds(-value.Second).AddMilliseconds(-value.Millisecond);

            int remainder = value.Minute % intervalMinutes;

            if (remainder >= intervalMinutes / 2)
                value = value.AddMinutes(intervalMinutes - remainder);
            else
                value = value.AddMinutes(-remainder);

            return value;
        }
    }
}
