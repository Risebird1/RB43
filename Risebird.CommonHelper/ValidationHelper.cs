using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Risebird.CommonHelper
{
    public class ValidationHelper : IValidationHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public ValidationHelper(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public bool ValidateEmailAddress(string email)
        {
            try
            {
                Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                Match match = regex.Match(email);
                return match.Success;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
