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
        public async Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionId = null, string invoicePDF = null)
        {
            try
            {
                return await _walletRepository.AddClientWalletBalance(billingType, clientId, CreditAmount, loginedUser, CreatedOn, billingOwnerId, transactionId, invoicePDF);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}

