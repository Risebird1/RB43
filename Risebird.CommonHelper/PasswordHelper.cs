using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class PasswordHelper : IPasswordHelper
    {
        private readonly IB2CClientService _b2CClientService;


        public PasswordHelper(IB2CClientService b2CClientService)
        {
            _b2CClientService = b2CClientService;

        }

        public async Task<B2CProvisioningResult> GenerateUserPassword(UserDetails user, bool forcePasswordChangeNextLogin = false)
        {
            Random rand = new Random(); // <-- Make this static somewhere

            const int maxValue = 999;
            string number = rand.Next(maxValue + 1).ToString("D3");
            string password = string.Empty;
            if (user.FirstName.Length >= 4)
            {
                password = user.FirstName.Substring(0, 4).ToUpper() + "@" + number;
            }
            else
            {
                //string name = string.Empty;
                // Padded to 4 so the password is always 8 characters: B2C rejects anything shorter,
                // which failed provisioning for short names ("Al", "Om").
                string name = (user.FirstName + user.LastName).PadRight(4, 'X');
                password = name.Substring(0, 4).ToUpper() + "@" + number;
            }
            var userobj = new UserList
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailId = user.EmailId,
                UserPassword = password
            };
            var outcome = await _b2CClientService.CreateUser(userobj, forcePasswordChangeNextLogin);

            return new B2CProvisioningResult
            {
                Outcome = outcome,
                // Only a freshly created account actually has this password. For one that already
                // existed the generated password was never applied, so handing it back would store
                // and email a credential that does not work.
                Password = outcome == B2CProvisioningOutcome.Created ? userobj.UserPassword : string.Empty
            };
        }
    }
}
