using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class InterviewerRedMarkers
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IInterviewerRepository _interviewerRepository;
        private readonly INotificationHelper _notificationHelper;

        public InterviewerRedMarkers(IApplicationErrorLogger applicationErrorLogger, IInterviewerRepository interviewerRepository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _interviewerRepository = interviewerRepository;
            _notificationHelper = notificationHelper;
        }

        [FunctionName("InterviewerRedMarkers")]
        public async Task Run([TimerTrigger("0 0 9 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _list = await _interviewerRepository.FecthUpcomingInterviewsRedMarkers();
            foreach (var item in _list)
            {
                try
                {
                    var _redmarkers = await _interviewerRepository.GetInterviewerInsights(item.EmailId);
                    var _mailtemplate = await TemplateMarkers(_redmarkers);
                    var acklink = "https://portal.risebird.io/Interviewer/AcknowledgeRedmarkers?uniqueid=" + item.uniqueid;
                    //send mail tpo interviewer
                    Mail _mail = new Mail
                    {
                        TO = item.EmailId,
                        Subject = "Urgent action required to resolve errors in your Risebird Interviews"
                    };
                    var emailsend = await _notificationHelper.SendInterviewerRedMarkersNotification(_mail, _mailtemplate, acklink);
                }
                catch (Exception ex)
                {
                    log.LogInformation($"error message : {ex.Message}");
                }
            }
        }
        public async Task<string> TemplateMarkers(InterviewerInsightsVM model)
        {
            var templatehtml = "<table width='100%' border='0' cellspacing='10' cellpadding='10'>";
            if (model.RedFlags > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Red Flag Interview:</td>";
                templatehtml += "<td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{0}'>" + model.RedFlags + "</a></td></tr>";
            }
            if (model.SuspiciousCount > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important;'>Suspicious Interviewer:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; color: #0061B7;font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{1}'>" + model.SuspiciousCount + "</a></td></tr>";
            }
            if (model.PaymentSLA > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important;'>Penalty:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{2}'>" + model.PaymentSLA + "</a></td></tr>";
            }
            if (model.Penalty > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Late Feedback Penalty:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{3}'>" + model.Penalty + "</a></td></tr>";
            }
            if (model.CandidateRating < 4)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important;'>Candidate Rating:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{4}'>" + model.CandidateRating + "</a></td></tr>";
            }
            if (model.CustomerRating < 4)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important;'>Client Rating</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{5}'>" + model.CustomerRating + "</a></td></tr>";
            }
            if (model.Redorequests > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-weight: bold; text-transform: uppercase; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Redo Request</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'><a style='text-decoration: none; color: #0061B7;' href='{6}'>" + model.Redorequests + "</a></td></tr>";
            }
            var calculatedValue = (model.SelectedInterviews * 100 / model.CompletedInterviews);
            if (calculatedValue >= 50)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Selected Interviews:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + calculatedValue + " %</td></tr>";
            }
            calculatedValue = (model.RejectedInterviews * 100 / model.CompletedInterviews);
            if (calculatedValue >= 80)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Rejected Interviews:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + calculatedValue + "</td></tr>";
            }
            calculatedValue = (model.UnassignInterviews * 100 / model.TotalInterviews);
            if (calculatedValue > 5)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Unassigned Interviews :</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>{9}</td></tr>";
            }
            calculatedValue = (model.NoShowInterviews * 100 / model.TotalInterviews);
            if (calculatedValue > 5)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important;'>NoShow Interviewer :</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + calculatedValue + "</td></tr>";
            }
            calculatedValue = (model.JoinLateMeeting * 100 / model.TotalInterviews);
            if (calculatedValue > 2)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Late Join:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + calculatedValue + "</td></tr>";
            }
            if (model.RejectedComment > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Less than 250 characters for Rejected:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + model.RejectedComment + "</td></tr>";
            }
            if (model.SelectedComment > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Less than 500 characters for Selected:</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + model.SelectedComment + "</td></tr> ";
            }
            if (model.LessQuestions > 0)
            {
                templatehtml += "<tr><td align='left' valign='top' width='40%' style='border: 1px solid #D9D9D9; font-size: 13px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; text-transform: uppercase; font-weight: bold; color: #003851; font-family: 'AvenirNextLTPro-Regular', 'Avenir Next', 'HelveticaNeue', 'Helvetica Neue', 'Avenir', Helvetica, Arial, Lucida Grande, sans-serif !important; '>Less than 5 Questions added</td><td align='left' valign='top' width='60%' style='border: 1px solid #D9D9D9; font-size: 14px; padding-top: 4px; padding-bottom: 4px; line-height: 28px; font-family: Arial;'>" + model.LessQuestions + "</td></tr></table>";
            }
            return templatehtml;
        }
    }
}
