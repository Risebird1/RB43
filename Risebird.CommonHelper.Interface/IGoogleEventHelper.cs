using Google.Apis.Calendar.v3.Data;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IGoogleEventHelper
    {
        Task<Event> CreateCalendarEvent(CalendarEventDM model);
        Task DeleteCalendarEvent(string eventid);
        Task<DataTable> CalendarEventVideo(string eventid);
        Task DeleteCalendarEventVideo(string fileid);
    }
}
