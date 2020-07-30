using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class DashboardVM
    {
        public IEnumerable<Meeting> Meetings { get; set; }
        public IEnumerable<DailyInterviewSlot> DailyInterviewSlots { get; set; }
        public string MeetingsData { get; set; }
        public string MeetingsDataLabels { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public DashboardInterviewerCount InterviewerCountObj { get; set; }
        public MeetingResultCount MeetingResultCountObj { get; set; }
        public CandidatesCount CandidatesCountObj { get; set; }

        [BindProperty]
        //public List<IRF> IrfProgressList { get; set; }
        //public IEnumerable<IRF> IrfProgressList { get; set; }
        public IList<IRF> IrfProgressList { get; set; }
        public SelectList ProgressOptions { get; set; }

    }
}
