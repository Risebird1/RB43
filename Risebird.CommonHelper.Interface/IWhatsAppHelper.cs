using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IWhatsAppHelper
    {
        Task<string> WhatsAppTrackUser(WhatsAppVM _whatsAppVM);
        Task<string> WhatsAppSendEventNotification(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendOnAssignation(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendScheduling(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendEInterviewer(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendApology(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendInterviewWaiting(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendReScheduling(WhatsAppVM whatsAppVM);
        //string WhatsAppSendCandidateReminder(WhatsAppVM whatsAppVM);
        //string WhatsAppSendClientCandidateConfirmation(WhatsAppVM _whatsAppVM);
        Task<string> WhatsAppSendCancleReScheduling(WhatsAppVM _whatsAppVM);
        Task<string> WhatsAppSendRatingNotification(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendFeedbackRemainder(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendInterviewerFeedbackRemainder(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendInterviewerCommonInterviewDetails(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendInterviewReminder(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendClientCompleteInterview(WhatsAppVM whatsAppVM);
        Task<string> WhatsAppSendCandidateDashboardReminder(WhatsAppVM whatsAppVM);
    }
}
