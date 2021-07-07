using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ITwilioHelper
    {
        Task SendSMSAsync(string msg, string userMobileNumber);
        Task MakePhoneCall(string userMobileNumber);
        Task SendWhatappMessageAsync(string msg, string userMobileNumber);
        Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail);

    }
}
