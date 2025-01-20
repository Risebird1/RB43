using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class InterviewHelper : IInterviewHelper
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IMeetingService _meetingService;

        public InterviewHelper(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingService meetingService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingService = meetingService;
        }
        public async Task<string> GetInterviewCancellationSource(int interviewId)
        {
            try
            {
                var cancellationsource = await _meetingService.GetInterviewCancellationSource(interviewId);
                return cancellationsource;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return string.Empty;
        }
    }
}
