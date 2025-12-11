using Risebird.Business.Interface;
using Risebird.CommonHelper;
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
    public class WalletService : IWalletService
    {
        private readonly IWalletRepository _walletRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public WalletService(IWalletRepository walletRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _walletRepository = walletRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<List<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null)
        {
            try
            {
                var resultdata = await _walletRepository.GetWallet(billingType, clientId, billingOwnerId, action);
                return resultdata?.ToList();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> AddWalletCredit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, string transactionType = "Manual")
        {
            try
            {
                return await _walletRepository.AddWalletCredit(billingType, clientId, billingOwnerId, emailid, points, comment, transactionType);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<string> AddWalletDebit(int billingType, int? clientId, int? billingOwnerId, string emailid, int points, string comment, int? interviewId = null, int? irfId = null, string transactionType = "Manual")
        {
            try
            {
                return await _walletRepository.AddWalletDebit(billingType, clientId, billingOwnerId, emailid, points, comment, interviewId, irfId, transactionType);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<string> DebitWalletByInterview(int interviewId, int? billingType = null, string transactionType = "InterviewBilling")
        {
            try
            {
                return await _walletRepository.DebitWalletByInterview(interviewId, billingType, transactionType);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed: " + ex.Message;
            }
        }

        public async Task<int> GetWalletBalance(int billingType, int? clientId = null, int? billingOwnerId = null)
        {
            try
            {
                return await _walletRepository.GetWalletBalance(billingType, clientId, billingOwnerId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
    }
}

