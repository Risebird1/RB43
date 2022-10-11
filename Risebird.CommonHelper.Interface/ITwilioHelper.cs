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

    }
}
