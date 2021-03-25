using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
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

        public async Task<string> GenerateUserPassword(UserDetails user)
        {
            Random rand = new Random(); // <-- Make this static somewhere

            const int maxValue = 999;
            string number = rand.Next(maxValue + 1).ToString("D3");
            var userobj = new UserList
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailId = user.EmailId,
                UserPassword = user.FirstName.ToUpper() + "@" + number
            };
            if (await _b2CClientService.CreateUser(userobj))
            {
                return userobj.UserPassword;
            }
            return string.Empty;
        }
    }
}
