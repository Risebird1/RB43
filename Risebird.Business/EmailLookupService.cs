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

        public async Task<OpsToolkitVM> GetOpsToolkitData(DateTime fromDate, DateTime toDate)
        {
            var result = new OpsToolkitVM { FromDate = fromDate.Date, ToDate = toDate.Date };
            try
            {
                result.EmergencyUnassignments = await _emailLookupRepository.GetEmergencyUnassignments(result.FromDate, result.ToDate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return result;
        }

        public async Task<OpsToolkitInterviewHistoryVM> GetInterviewPointsHistory(int interviewId)
        {
            if (interviewId <= 0)
                return new OpsToolkitInterviewHistoryVM { InterviewId = interviewId };

            try
            {
                return await _emailLookupRepository.GetInterviewPointsHistory(interviewId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new OpsToolkitInterviewHistoryVM { InterviewId = interviewId };
            }
        }

        public async Task<OpsToolkitActionResultVM> RemoveUnassignPenalty(int interviewPointsId, string reason, string performedBy)
        {
            if (interviewPointsId <= 0)
                return new OpsToolkitActionResultVM { Success = false, Message = "Invalid penalty record." };
            if (string.IsNullOrWhiteSpace(reason))
                return new OpsToolkitActionResultVM { Success = false, Message = "Please enter a reason." };

            try
            {
                reason = reason.Trim();
                if (reason.Length > 500) reason = reason.Substring(0, 500);
                return await _emailLookupRepository.RemoveUnassignPenalty(interviewPointsId, reason, performedBy);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new OpsToolkitActionResultVM { Success = false, Message = "Could not remove the penalty." };
            }
        }
    }
}
