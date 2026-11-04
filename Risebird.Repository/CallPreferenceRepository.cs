using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class CallPreferenceRepository : ICallPreferenceRepository
    {
        private const int MaxRawPayloadLength = 100000;

        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public CallPreferenceRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        // CallPreferences.CreatedOn and the cold / MAG timestamps are stored in IST.
        private static DateTime NowIst => DateTime.UtcNow.AddMinutes(330);

        private static string Cap(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value.Substring(0, max);

        public async Task<CallOptOutResult> RecordInterviewerCallOptOut(InterviewerCallOptOutRequest request)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryFirstOrDefaultAsync<CallOptOutResult>("usp_RecordInterviewerCallOptOut",
                    new
                    {
                        UserId = request.UserId,
                        Phone = Cap(request.Phone, 200),
                        EmailId = Cap(request.EmailId, 200),
                        Source = Cap(request.Source, 50),
                        OptOutSource = request.OptOutSource,
                        ProviderCallId = Cap(request.ProviderCallId, 100),
                        Reason = Cap(request.Reason, 500),
                        RawPayload = Cap(request.RawPayload, MaxRawPayloadLength),
                        CreatedBy = Cap(request.CreatedBy, 200),
                        Now = NowIst
                    },
                    commandType: CommandType.StoredProcedure);
                return result ?? new CallOptOutResult();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<CallOptOutResult> RecordCandidateCallOptOut(CandidateCallOptOutRequest request)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                // Usp_ColdCandidateMarked selects the interviews it cancelled, one result set per marked candidate,
                // before the final OptedOut / IRFId row. Read them all: the cancelled interviews feed the mails.
                using var multi = await con.QueryMultipleAsync("usp_RecordCandidateCallOptOut",
                    new
                    {
                        CandidateId = request.CandidateId,
                        IRFId = request.IRFId,
                        Phone = Cap(request.Phone, 200),
                        EmailId = Cap(request.EmailId, 200),
                        Source = Cap(request.Source, 50),
                        OptOutSource = request.OptOutSource,
                        ProviderCallId = Cap(request.ProviderCallId, 100),
                        Reason = Cap(request.Reason, 500),
                        RawPayload = Cap(request.RawPayload, MaxRawPayloadLength),
                        CreatedBy = Cap(request.CreatedBy, 200),
                        Now = NowIst
                    },
                    commandType: CommandType.StoredProcedure);

                var result = new CallOptOutResult();
                while (!multi.IsConsumed)
                {
                    foreach (IDictionary<string, object> row in await multi.ReadAsync())
                    {
                        if (row.TryGetValue("OptedOut", out var optedOut))
                        {
                            result.OptedOut = optedOut == null ? 0 : Convert.ToInt32(optedOut);
                            result.IRFId = row.TryGetValue("IRFId", out var irf) && irf != null ? Convert.ToInt32(irf) : (int?)null;
                        }
                        else if (row.TryGetValue("InterviewId", out var id))
                        {
                            result.CancelledInterviews.Add(new CancelledInterview
                            {
                                InterviewId = id == null ? 0 : Convert.ToInt32(id),
                                Interviewer = row.TryGetValue("interviewer", out var interviewer) ? interviewer as string : null,
                                InterviewDate = row.TryGetValue("interviewDate", out var date) && date is DateTime d ? d : default,
                                CandidateName = row.TryGetValue("candidateName", out var name) ? name as string : null,
                            });
                        }
                    }
                }
                // The IRF is only known from the last result set.
                result.CancelledInterviews.ForEach(i => i.IRFId = result.IRFId ?? 0);
                return result;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<CanCallResult> CanCallContact(string personType, string phone, int? irfId = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryFirstOrDefaultAsync<CanCallResult>("usp_CanCallContact",
                    new { PersonType = personType, Phone = Cap(phone, 200), IRFId = irfId },
                    commandType: CommandType.StoredProcedure);
                return result ?? new CanCallResult { CanCall = true };
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<CallConsentResult> RecordInterviewerCallConsent(int userId, string ipAddress, string userAgent)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var result = await con.QueryFirstOrDefaultAsync<CallConsentResult>("usp_RecordInterviewerCallConsent",
                    new { UserId = userId, IPAddress = Cap(ipAddress, 64), UserAgent = Cap(userAgent, 500), Now = NowIst },
                    commandType: CommandType.StoredProcedure);
                return result ?? new CallConsentResult();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new CallConsentResult();
            }
        }

        public async Task<bool> UndoInterviewerCallOptOut(int userId, string performedBy)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<bool>("usp_UndoInterviewerCallOptOut",
                    new { UserId = userId, PerformedBy = Cap(performedBy, 200), Now = NowIst },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
