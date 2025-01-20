using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class InterviewHelper: IInterviewHelper
    {
        private readonly IApplicationErrorLogger _errorLogger;
        private readonly IConfiguration _configuration;
        public InterviewHelper(IConfiguration configuration, IApplicationErrorLogger errorLogger)
        {
            _configuration = configuration;
            _errorLogger = errorLogger;

        }
        public async Task<string> GetInterviewCancellationSource(int interviewId)
        {
            return "";
        }
        }
}
