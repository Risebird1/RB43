using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class SupportService : ISupportService
    {
        private readonly ISupportRepository _supportRespository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IRaisedIssueListingSearchRepository _raisedIssueListingSearchRepository;

        public SupportService(
            ISupportRepository supportRespository,
            IApplicationErrorLogger applicationErrorLogger,
            IRaisedIssueListingSearchRepository raisedIssueListingSearchRepository)
        {
            _supportRespository = supportRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _raisedIssueListingSearchRepository = raisedIssueListingSearchRepository;
        }

        public async Task<List<Support>> GetAllSupportHR(string emailId = null)
        {
            try
            {
                return await _supportRespository.GetAllSupportHR(emailId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> AddSupportHR(Support hrDetails)
        {
            try
            {
                return await _supportRespository.AddSupportHR(hrDetails);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<Support> GetSupportHR(int id)
        {
            try
            {
                return await _supportRespository.GetSupportHR(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateSupportHR(Support hrDetails)
        {
            try
            {
                return await _supportRespository.UpdateSupportHR(hrDetails);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteSupportHR(int id)
        {
            try
            {
                return await _supportRespository.DeleteSupportHR(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> SaveRaisedIssueDetails(RaiseBugsVM model)
        {
            try
            {
                var result = await _supportRespository.SaveRaisedIssueDetails(model);
                if (result)
                    await TrySyncRaisedIssueListingSearchAsync(model.id);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IEnumerable<RaiseBugsVM>> GetRaisedIssueDetails(string profileName, string type = null)
        {
            try
            {
                if (_raisedIssueListingSearchRepository.IsEnabled && !string.IsNullOrWhiteSpace(type))
                {
                    var search = await _raisedIssueListingSearchRepository.SearchAsync(profileName, type);
                    if (search != null)
                        return search;
                }

                var all = await _supportRespository.GetRaisedIssueDetails(profileName);
                return ApplyTypeFilter(all, type);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        /// <summary>SQL-fallback equivalent of the OData type filter applied in RaisedIssueListingSearchRepository.</summary>
        private static IEnumerable<RaiseBugsVM> ApplyTypeFilter(IEnumerable<RaiseBugsVM> source, string type)
        {
            if (source == null)
                return null;

            if (string.IsNullOrWhiteSpace(type) || string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
                return source;

            if (string.Equals(type, "Open", StringComparison.OrdinalIgnoreCase))
                return source.Where(x => x.Status == "Open" && x.RequestType != "Payment");

            if (string.Equals(type, "OpenPayment", StringComparison.OrdinalIgnoreCase))
                return source.Where(x => x.Status == "Open" && x.RequestType == "Payment" && !x.IsReopened);

            if (string.Equals(type, "ReOpenPayment", StringComparison.OrdinalIgnoreCase))
                return source.Where(x => x.Status == "Open" && x.RequestType == "Payment" && x.IsReopened);

            return source.Where(x => x.Status != "Open");
        }

        public async Task<bool> UpdateRaisedRequestStatus(RaiseBugsVM model)
        {
            try
            {
                var result = await _supportRespository.UpdateRaisedRequestStatus(model);
                if (result)
                    await TrySyncRaisedIssueListingSearchAsync(model.id);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<RaiseBugsVM>> GetMyRaisedIssues(string requestedBy)
        {
            try
            {
                return await _supportRespository.GetMyRaisedIssues(requestedBy);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<RaiseBugsVM> GetRaisedIssueById(int id)
        {
            try
            {
                return await _supportRespository.GetRaisedIssueById(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> ReopenRaisedIssue(int id, string comment, string actionBy, string actionByRole)
        {
            try
            {
                var result = await _supportRespository.ReopenRaisedIssue(id, comment, actionBy, actionByRole);
                if (result)
                    await TrySyncRaisedIssueListingSearchAsync(id);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<RaisedIssueHistoryVM>> GetRaisedIssueHistory(int raisedIssueId)
        {
            try
            {
                return await _supportRespository.GetRaisedIssueHistory(raisedIssueId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<PaymentTicketReviewVM>> GetOpenPaymentTicketsForReview()
        {
            try
            {
                return await _supportRespository.GetOpenPaymentTicketsForReview();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> SaveRaisedIssueAIResponse(int ticketId, string aiResponseJson,
            string statusComment, string closedBy, DateTime closedOn, bool closeTicket)
        {
            try
            {
                var result = await _supportRespository.SaveRaisedIssueAIResponse(
                    ticketId, aiResponseJson, statusComment, closedBy, closedOn, closeTicket);
                if (result)
                    await TrySyncRaisedIssueListingSearchAsync(ticketId);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        private async Task TrySyncRaisedIssueListingSearchAsync(int raisedIssueId)
        {
            if (!_raisedIssueListingSearchRepository.IsEnabled || raisedIssueId <= 0)
                return;

            try
            {
                var synced = await _raisedIssueListingSearchRepository.UpsertRaisedIssueAsync(raisedIssueId);
                if (!synced)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new InvalidOperationException(
                            $"RaisedIssue Azure Search upsert returned false for raisedIssueId={raisedIssueId}."));
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> UpdateQCPenaltyFromAIReview(int interviewId, int revisedPenaltyPct,
            string updatedMergedDetails, string reasoningComment)
        {
            try
            {
                return await _supportRespository.UpdateQCPenaltyFromAIReview(
                    interviewId, revisedPenaltyPct, updatedMergedDetails, reasoningComment);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
