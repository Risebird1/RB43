using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;

namespace Risebird.Business.CallPreference
{
    /// <summary>
    /// Mails for the interviews a candidate call opt-out cancelled (usp_RecordCandidateCallOptOut): the same as a
    /// manual cold mark (DashboardController.MarkedColdCandidate) — the interviewer, with the IRF owners. No mail to
    /// the candidate: they just asked not to be contacted, and the manual cold mark never reached the candidate either.
    /// Used by the Services opt-out webhook and the AI call outcome (PlivoCXCallOutcomeProcessor).
    /// A failed mail is logged and never thrown: the opt-out is already applied.
    /// </summary>
    public static class CallOptOutCancellationMails
    {
        public static async Task SendAsync(IEnumerable<CancelledInterview> interviews, INotificationHelper notificationHelper,
            ICandidateService candidateService, IApplicationErrorLogger logger)
        {
            if (interviews == null) return;
            var nowIst = DateTime.UtcNow.AddMinutes(330);
            foreach (var item in interviews.Where(i => i.InterviewId > 0 && i.InterviewDate > nowIst))
            {
                try
                {
                    var owners = await candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(item.InterviewId);
                    var interviewers = string.IsNullOrEmpty(item.Interviewer) ? new List<string>() : new List<string> { item.Interviewer };
                    await notificationHelper.SendInterviewCancelledMail(interviewers, item.InterviewDate.ToString(), owners?.ToList(), item.IRFId, item.InterviewId);
                }
                catch (Exception ex)
                {
                    await logger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
