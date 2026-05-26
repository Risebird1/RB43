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

        public async Task<List<WalletVM>> GetWallet(int billingType, int? clientId = null, int? billingOwnerId = null, string action = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var resultdata = await _walletRepository.GetWallet(billingType, clientId, billingOwnerId, action, startDate, endDate);
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
        public async Task<bool> AddClientWalletBalance(int billingType, int clientId, int CreditAmount, string loginedUser, DateTime? CreatedOn, int? billingOwnerId = null, string transactionRefNumber = null, string invoicePDF = null, DateTime? transactionDate = null, string transactionType = "Manual", int? clientPOId = null)
        {
            try
            {
                return await _walletRepository.AddClientWalletBalance(billingType, clientId, CreditAmount, loginedUser, CreatedOn, billingOwnerId, transactionRefNumber, invoicePDF, transactionDate, transactionType, clientPOId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int> SaveClientPO(int clientId, int billingType, int billingModel, decimal poCreditAmount, int poMonth, int poYear, string createdBy, int? billingOwnerId = null, string poNumber = null, string poDocumentUrl = null, string remarks = null)
        {
            try
            {
                return await _walletRepository.SaveClientPO(clientId, billingType, billingModel, poCreditAmount, poMonth, poYear, createdBy, billingOwnerId, poNumber, poDocumentUrl, remarks);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<ClientPOVM>> GetClientPOList(int clientId, int? month = null, int? year = null, int? billingOwnerId = null, bool openOnly = false)
        {
            try
            {
                var list = await _walletRepository.GetClientPOList(clientId, month, year, billingOwnerId, openOnly);
                foreach (var po in list)
                {
                    MapPOStatus(po);
                }
                return list;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<ClientPOVM>();
            }
        }

        public async Task<List<ClientPOVM>> GetPOHistory(int clientId)
        {
            var list = await GetClientPOList(clientId);
            foreach (var po in list)
            {
                MapPOStatus(po);
            }
            return list;
        }

        public async Task<List<WalletVM>> GetCashCreditHistory(int clientId)
        {
            try
            {
                return await _walletRepository.GetCashCreditHistory(clientId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<WalletVM>();
            }
        }

        private static void MapPOStatus(ClientPOVM po)
        {
            var mfi = new System.Globalization.DateTimeFormatInfo();
            po.PeriodLabel = mfi.GetMonthName(po.POMonth) + " " + po.POYear;
            switch (po.Status)
            {
                case 0:
                    po.StatusDesc = "Awaiting Cash";
                    po.StatusCss = "badge-warning";
                    break;
                case 1:
                    po.StatusDesc = "Partial Cash";
                    po.StatusCss = "badge-info";
                    break;
                case 2:
                    po.StatusDesc = "Cash Credited";
                    po.StatusCss = "badge-success";
                    break;
                case 3:
                    po.StatusDesc = "Document Only";
                    po.StatusCss = "badge-secondary";
                    break;
                default:
                    po.StatusDesc = "Unknown";
                    po.StatusCss = "badge-secondary";
                    break;
            }
        }

        public async Task<ClientWalletDashboardVM> GetClientWalletDashboard(int clientId, int month, int year, bool allDetails = false)
        {
            try
            {
                var dashboard = await _walletRepository.GetClientWalletDashboard(clientId, month, year, allDetails);
                if (dashboard == null)
                {
                    dashboard = new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
                }

                dashboard.IsAllDetailsView = allDetails;
                dashboard.SupportsPO = dashboard.BillingModel == (int)BillingModelEnum.M2
                    || dashboard.BillingModel == (int)BillingModelEnum.M3;

                if (Enum.IsDefined(typeof(BillingModelEnum), dashboard.BillingModel))
                {
                    dashboard.BillingModelDesc = ((BillingModelEnum)dashboard.BillingModel).GetDescription();
                }

                var mfi = new System.Globalization.DateTimeFormatInfo();
                dashboard.PeriodLabel = mfi.GetMonthName(month) + " " + year;
                dashboard.DetailsPeriodLabel = allDetails ? "All Time" : dashboard.PeriodLabel;

                if (dashboard.POCreditLimit > 0)
                {
                    dashboard.POCreditUtilizationPercent = (int)Math.Min(100, Math.Round(dashboard.POConsumed / dashboard.POCreditLimit * 100));
                }

                foreach (var owner in dashboard.OwnerWallets)
                {
                    if (owner.POCredit <= 0)
                    {
                        owner.State = dashboard.BillingModel == (int)BillingModelEnum.M2 ? "Awaiting PO" : "No PO";
                        owner.StateCss = "badge-warning";
                    }
                    else if (owner.Remaining <= 0)
                    {
                        owner.State = "PO exhausted";
                        owner.StateCss = "badge-danger";
                    }
                    else
                    {
                        owner.State = "Active";
                        owner.StateCss = "badge-success";
                    }
                }

                foreach (var po in dashboard.PORecords)
                {
                    MapPOStatus(po);
                }

                return dashboard;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new ClientWalletDashboardVM { ClientId = clientId, SelectedMonth = month, SelectedYear = year };
            }
        }
    }
}

