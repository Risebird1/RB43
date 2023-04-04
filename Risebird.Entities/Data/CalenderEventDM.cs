using Google.Apis.Calendar.v3.Data;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class CalendarEventDM
    {
        //public CalenderEventDM()
        //{
        //    Start = new EventDateTime();
        //    End = new EventDateTime();
        //}
        public string Summary { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimeZone { get; set; }
        public IList<EventAttendee> Attendees { get; set; }
    }
}