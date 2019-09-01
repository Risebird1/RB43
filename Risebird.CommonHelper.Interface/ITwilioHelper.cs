using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ITwilioHelper
    {
        Task SendSMSAsync(string msg, string userMobileNumber);
        Task MakePhoneCall(string userMobileNumber);
        Task SendWhatappMessageAsync(string msg, string userMobileNumber);

    }
}
