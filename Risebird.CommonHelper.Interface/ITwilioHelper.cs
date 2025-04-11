using System;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace Risebird.CommonHelper.Interface
{
    public interface ITwilioHelper
    {
        Task SendSMSAsync(string msg, string userMobileNumber);
        Task MakePhoneCall(string userMobileNumber);
        Task SendWhatappMessageAsync(string msg, string userMobileNumber);
        Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail);
        Task<string> callrecording(string callSid, string callbackUrl);
        Task<bool> CandidateReminderCall(string phoneNumber, string FirstName, string interviewdate);
        Task<RecordingResource> FetchCallRecordingDetails(string recordingSid);
        Task<bool> MakeIVRCall(string userPhoneNumber, string recipientName, string interviewDate, string ivrType = null);
        Task<bool> MakeIVRCallToInterviewerForAvailability(string userPhoneNumber, string uniqueId, int irfId, string jdTitle, int assessmentGroupId);
        Task<bool> MakeIVRCallForCandidateMeetingReminder(string userPhoneNumber, string candidateFirstName, string uniqueId, int irfId, string jdTitle, int interviewId, int candidateId, DateTime interviewDate, string timeZoneValue);

    }
}
