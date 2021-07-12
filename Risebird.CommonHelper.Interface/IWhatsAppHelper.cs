using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Interface
{
    public interface IWhatsAppHelper
    {
        string WhatsAppTrackUser(WhatsAppVM _whatsAppVM);
        string WhatsAppSendEventNotification(WhatsAppVM whatsAppVM);
        string WhatsAppSendOnAssignation(WhatsAppVM whatsAppVM);
        string WhatsAppSendScheduling(WhatsAppVM whatsAppVM);
        string WhatsAppSendEInterviewer(WhatsAppVM whatsAppVM);
        string WhatsAppSendApology(WhatsAppVM whatsAppVM);
        string WhatsAppSendInterviewWaiting(WhatsAppVM whatsAppVM);
        string WhatsAppSendReScheduling(WhatsAppVM whatsAppVM);
        string WhatsAppSendCandidateReminder(WhatsAppVM whatsAppVM);
        string WhatsAppSendClientCandidateConfirmation(WhatsAppVM _whatsAppVM);
        string WhatsAppSendCancleReScheduling(WhatsAppVM _whatsAppVM);
        string WhatsAppSendRatingNotification(WhatsAppVM whatsAppVM);
        string WhatsAppSendFeedbackRemainder(WhatsAppVM whatsAppVM);
        string WhatsAppSendInterviewerFeedbackRemainder(WhatsAppVM whatsAppVM);

    }
}
