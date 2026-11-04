using Risebird.Entities.ViewModel;
using System;
using System.Linq;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// The links and time zone of one C3 Interview Reminder (email and WhatsApp), built the way
    /// CandidateMeetingReminder builds them, for the reminders sent by hand: the Not Confirmed bulk send and
    /// WhatsApp campaigns with a confirm purpose.
    /// </summary>
    public sealed class CandidateReminderLinks
    {
        public string Yes { get; private set; }
        public string No { get; private set; }
        public string Reschedule { get; private set; }
        public string NeedClarity { get; private set; }
        public string Dashboard { get; private set; }
        public string TimeZone { get; private set; }
        /// <summary>The interview time in the candidate's zone (stored in IST).</summary>
        public DateTime InterviewDate { get; private set; }

        public static CandidateReminderLinks For(UnassignedInterviewVM item, string portalBaseUrl = "https://portal.risebird.io")
        {
            var portal = (portalBaseUrl ?? "https://portal.risebird.io").TrimEnd('/');
            var query = "&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
            var links = new CandidateReminderLinks
            {
                Yes           = portal + "/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=1" + query,
                No            = portal + "/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=2" + query,
                Reschedule    = portal + "/Interview/CandidateRescheduleMeetingsStatus?meetingid=" + item.Interviewid + "&status=3" + query,
                NeedClarity   = portal + "/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=4" + query,
                Dashboard     = portal + "/Candidate/InterviewDetails?id=" + item.uniqueid + "&IRFId=" + item.Irfid,
                TimeZone      = "IST",
                InterviewDate = item.interviewDate
            };
            if (!string.IsNullOrWhiteSpace(item.TimeZonesValue)
                && item.TimeZoneoffset.HasValue
                && !item.TimeZonesValue.Contains("Chennai"))
            {
                links.TimeZone = item.TimeZonesValue.Split(")").ElementAtOrDefault(1);
                links.InterviewDate = item.interviewDate.AddMinutes(-(330 + item.TimeZoneoffset.Value));
            }
            return links;
        }

        /// <summary>The C3 WhatsApp message, as CandidateMeetingReminder.SendReminderWhatsApp builds it.</summary>
        public WhatsAppVM WhatsApp(UnassignedInterviewVM item, string sentBy)
        {
            return new WhatsAppVM
            {
                Emailid       = item.CandidateEmail,
                InterviewDate = InterviewDate.ToString(),
                buttonlink    = Yes,
                DashboardLink = "Candidate/VerifiedInterviewDetails/" + item.uniqueid + "?emailid=" + Uri.EscapeDataString(item.CandidateEmail ?? "") + "&IRFId=" + item.Irfid,
                Event         = "Interview Reminder",
                PrimarySkill  = item.PrimarySkillInvite,
                ClientName    = item.ClientName,
                timezone      = TimeZone,
                irfId         = item.Irfid,
                InterviewId   = item.Interviewid,
                SentBy        = sentBy
            };
        }
    }
}
