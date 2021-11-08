using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
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

        public async Task<string> GenerateUserPassword(UserDetails user, bool forcePasswordChangeNextLogin = false)
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
                string name = user.FirstName + user.LastName;
                password = name.Length >= 4 ? (name.Substring(0, 4).ToUpper() + "@" + number) : (password = name.ToUpper() + "@" + number);
            }
            var userobj = new UserList
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailId = user.EmailId,
                UserPassword = password
            };
            if (await _b2CClientService.CreateUser(userobj, forcePasswordChangeNextLogin))
            {
                return userobj.UserPassword;
            }
            return string.Empty;
        }
    }
}
