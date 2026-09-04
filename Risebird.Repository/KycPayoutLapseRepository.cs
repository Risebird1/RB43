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
    public class KycPayoutLapseRepository : IKycPayoutLapseRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public KycPayoutLapseRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IList<KycLapseCandidate>> FetchKycIncompletePayoutCandidatesAsync(DateTime asOfDate, int minAgeDays)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<KycLapseCandidate>(con,
                        "usp_FetchKycIncompletePayoutCandidates",
                        new { AsOfDate = asOfDate.Date, MinAgeDays = minAgeDays },
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

        public async Task<bool> AddLapseTrackingAsync(int userid, string month, string remark, decimal? pendingAmount, string createdBy)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var inserted = await SqlMapper.QuerySingleOrDefaultAsync<int>(con,
                        "usp_AddKycPayoutLapseTracking",
                        new
                        {
                            Userid = userid,
                            Month = month,
                            Remark = remark,
                            PendingAmount = pendingAmount,
                            CreatedBy = createdBy
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

        public async Task<IList<KycLapseReminderTarget>> FetchReminderTargetsAsync(DateTime asOfDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var rows = await SqlMapper.QueryAsync<KycLapseReminderTarget>(con,
                        "usp_FetchKycPayoutLapseReminderTargets",
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

        public async Task UpdateTrackingStatusAsync(int id, string status)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.ExecuteAsync("usp_UpdateKycPayoutLapseStatus",
                        new { Id = id, Status = status },
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

        public async Task MarkReminderSentAsync(int id, int reminderCount)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.ExecuteAsync("usp_MarkKycPayoutLapseReminderSent",
                        new { Id = id, ReminderCount = reminderCount },
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

        public async Task LapseInterviewPointsAsync(int userid, string month)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    await con.ExecuteAsync("usp_LapseInterviewPointsForKyc",
                        new { Userid = userid, Month = month },
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
