using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TdsRepository : ITdsRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public TdsRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IList<InterviewerQuarterlyTdsRow>> FetchInterviewerQuarterlyTdsAsync(
            DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<InterviewerQuarterlyTdsRow>(con,
                        "usp_FetchInterviewerQuarterlyTds",
                        new { StartDate = startDate.Date, EndDate = endDate.Date },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IList<WeeklyPayoutVM>> FetchWeeklyPayoutsAsync(
            string financialYear, string quarter)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<WeeklyPayoutVM>(con,
                        "usp_FetchWeeklyPayouts",
                        new { FinancialYear = financialYear, Quarter = quarter },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IList<TdsChallanVM>> FetchTdsChallansAsync(
            string financialYear, string quarter)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<TdsChallanVM>(con,
                        "usp_FetchTdsChallans",
                        new { FinancialYear = financialYear, Quarter = quarter },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<int> AddOrUpdateTdsChallanAsync(TdsChallanVM vm, string operatorEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var result = await SqlMapper.QuerySingleOrDefaultAsync<int>(con,
                        "usp_AddOrUpdateTdsChallan",
                        new
                        {
                            id                     = vm.id,
                            FinancialYear          = vm.FinancialYear,
                            Quarter                = vm.Quarter,
                            WeekNumber             = vm.WeekNumber,
                            SrNo                   = vm.SrNo,
                            SectionCode            = vm.SectionCode,
                            TANNumber              = vm.TANNumber,
                            Deductor               = vm.Deductor,
                            TdsAmount              = vm.TdsAmount,
                            BSRCode                = vm.BSRCode,
                            DepositDate            = vm.DepositDate,
                            ChallanSerialNumber    = vm.ChallanSerialNumber,
                            MinorHead              = vm.MinorHead,
                            ChallanReferenceNumber = vm.ChallanReferenceNumber,
                            ChallanPdfPath         = vm.ChallanPdfPath,
                            PayoutReference        = vm.PayoutReference,
                            PaymentDate            = vm.PaymentDate,
                            IsPaid                 = vm.IsPaid,
                            Operator               = operatorEmail
                        },
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }

        public async Task<IList<ChallanInterviewerVM>> FetchChallanInterviewersAsync(int challanId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<ChallanInterviewerVM>(con,
                        "usp_FetchChallanInterviewers",
                        new { ChallanId = challanId },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task DeleteTdsChallanAsync(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await SqlMapper.ExecuteAsync(con,
                        "usp_DeleteTdsChallan",
                        new { id },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<TdsReturnFilingVM> FetchTdsReturnFilingAsync(
            string financialYear, string quarter)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var row = await SqlMapper.QueryFirstOrDefaultAsync<TdsReturnFilingVM>(con,
                        "usp_FetchTdsReturnFiling",
                        new { FinancialYear = financialYear, Quarter = quarter },
                        commandType: CommandType.StoredProcedure);
                    return row;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task AddOrUpdateTdsReturnFilingAsync(TdsReturnFilingVM vm, string operatorEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await SqlMapper.ExecuteAsync(con,
                        "usp_AddOrUpdateTdsReturnFiling",
                        new
                        {
                            FinancialYear          = vm.FinancialYear,
                            Quarter                = vm.Quarter,
                            ReturnType             = vm.ReturnType,
                            FilingDate             = vm.FilingDate,
                            AcknowledgementNumber  = vm.AcknowledgementNumber,
                            TokenNumber            = vm.TokenNumber,
                            Status                 = vm.Status,
                            Remarks                = vm.Remarks,
                            Operator               = operatorEmail
                        },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<InterviewerPanInfo> FetchInterviewerByPanAsync(string pan)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    return await SqlMapper.QueryFirstOrDefaultAsync<InterviewerPanInfo>(con,
                        "usp_FetchInterviewerByPan",
                        new { PAN = pan },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IList<InterviewerChallanLink>> FetchInterviewerChallanNumbersAsync(
            string financialYear, string quarter)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<InterviewerChallanLink>(con,
                        "usp_FetchInterviewerChallanNumbers",
                        new { FinancialYear = financialYear, Quarter = quarter },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IList<PendingTdsWeekVM>> FetchPendingTdsByWeekAsync(
            string financialYear, string quarter)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<PendingTdsWeekVM>(con,
                        "usp_FetchPendingTdsByWeek",
                        new { FinancialYear = financialYear, Quarter = quarter },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IList<UnverifiedWalletInterviewer>> FetchUnverifiedWalletInterviewersAsync(
            DateTime asOfDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<UnverifiedWalletInterviewer>(con,
                        "usp_FetchUnverifiedWalletInterviewers",
                        new { AsOfDate = asOfDate.Date },
                        commandType: CommandType.StoredProcedure);
                    return rows.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> LogLapseReminderAsync(
            int userid, string paymentReference, int reminderIndex, string emailTo)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var inserted = await SqlMapper.QuerySingleOrDefaultAsync<int>(con,
                        "usp_LogTdsLapseReminder",
                        new
                        {
                            Userid           = userid,
                            PaymentReference = paymentReference,
                            ReminderIndex    = reminderIndex,
                            EmailTo          = emailTo
                        },
                        commandType: CommandType.StoredProcedure);
                    return inserted == 1;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task AddForm16CertificateAsync(
            string emailId, string financialYear, string quarter, string blobUrl)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await SqlMapper.ExecuteAsync(con,
                        "usp_AddCertificates",
                        new
                        {
                            Emailid = emailId,
                            year    = financialYear,
                            comment = "Form 16A – Auto-uploaded",
                            clink   = blobUrl,
                            month   = quarter,
                            id      = 0
                        },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
