using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class MailService : IMailService
    {
        private readonly IMailRepository _mailRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public MailService(IMailRepository mailRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _mailRepository = mailRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IEnumerable<SendgridEventData>> GetRecipients(string recipientType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _mailRepository.GetRecipients(recipientType, startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientActivities(string recipientType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _mailRepository.GetEmailRecipientActivities(recipientType, startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetEmailRecipientsLatestActivity(string recipientType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _mailRepository.GetEmailRecipientsLatestActivity(recipientType, startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetRecipientMailActivitiesByMessageId(string recipientType, string messageId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                return await _mailRepository.GetRecipientMailActivitiesByMessageId(recipientType, messageId, startDate, endDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<MailRecipientActivityDetailsVM>> GetInterviewerMailActivityForAvailabilityByIRFId(int irfId)
        {
            try
            {
                return await _mailRepository.GetInterviewerMailActivityForAvailabilityByIRFId(irfId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
    }
}
