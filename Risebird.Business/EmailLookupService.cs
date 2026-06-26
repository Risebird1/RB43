using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class EmailLookupService : IEmailLookupService
    {
        private readonly IEmailLookupRepository _emailLookupRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public EmailLookupService(IEmailLookupRepository emailLookupRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _emailLookupRepository = emailLookupRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<EmailLookupResultVM> GetEmailLookupData(string emailId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(emailId))
                {
                    return new EmailLookupResultVM { IsFound = false };
                }

                return await _emailLookupRepository.GetEmailLookupData(emailId.Trim().ToLower());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new EmailLookupResultVM { SearchEmail = emailId, IsFound = false };
            }
        }
    }
}
