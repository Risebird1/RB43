using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Risebird.CommonHelper
{
    public class ZoomHelper : IZoomHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;


        public ZoomHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public string ZoomToken()
        {
            try
            {
                // Token will be good for 1 day
                DateTime Expiry = DateTime.UtcNow.AddDays(1);


                string ApiKey = _configuration["ZoomAppDetails:apiKey"];
                string ApiSecret = _configuration["ZoomAppDetails:secretKey"];

                int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;

                // Create Security key  using private key above:
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));

                // length should be >256b
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                //Finally create a Token
                var header = new JwtHeader(credentials);

                //Zoom Required Payload
                var payload = new JwtPayload
            {
                { "iss", ApiKey},
                { "exp", ts },
            };

                var secToken = new JwtSecurityToken(header, payload);
                var handler = new JwtSecurityTokenHandler();

                // Token to String so you can use it in your client
                var tokenString = handler.WriteToken(secToken);

                return tokenString;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
