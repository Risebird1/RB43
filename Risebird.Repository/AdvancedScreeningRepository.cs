using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel.AdvancedScreening;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <inheritdoc cref="IAdvancedScreeningRepository"/>
    public class AdvancedScreeningRepository : IAdvancedScreeningRepository
    {
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AdvancedScreeningRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<IRFScreeningRubricVM> GetIRFScreeningRubric(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return (await con.QueryAsync<IRFScreeningRubricVM>("usp_GetIRFScreeningRubric",
                        new { IRFId = irfId }, commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<RubricSaveResult> SaveIRFScreeningRubric(RubricSaveRequest r)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@IRFId", r.IRFId);
                p.Add("@User", r.User);
                p.Add("@SaveInstructions", r.SaveInstructions);
                p.Add("@Instructions", r.Instructions, DbType.String, size: 2000);
                p.Add("@ApplyInstructionsToRubric", r.ApplyInstructionsToRubric);
                p.Add("@ApplyInstructionsToScoring", r.ApplyInstructionsToScoring);
                p.Add("@JDText", r.JDText, DbType.String, size: -1);
                p.Add("@RubricJson", r.RubricJson, DbType.String, size: -1);
                p.Add("@TotalWeight", r.TotalWeight);
                p.Add("@Status", r.Status);
                p.Add("@LastError", Truncate(r.LastError, 1000), DbType.String, size: 1000);
                p.Add("@Approve", r.Approve);
                using (var con = new SqlConnection(connectionString))
                {
                    return (await con.QueryAsync<RubricSaveResult>("usp_SaveIRFScreeningRubric", p,
                        commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<AdvancedScreeningWorkItem>> ClaimCandidatesForAdvancedScreening(int batchSize)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return await con.QueryAsync<AdvancedScreeningWorkItem>("usp_GetCandidatesForAdvancedScreening",
                        new { BatchSize = batchSize }, commandType: CommandType.StoredProcedure, commandTimeout: 120);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Enumerable.Empty<AdvancedScreeningWorkItem>();
            }
        }

        public async Task<bool> SaveCandidateAdvancedScreening(CandidateAdvancedScreeningSave m)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@CandidateId", m.CandidateId);
                p.Add("@IRFId", m.IRFId);
                p.Add("@Status", m.Status);
                p.Add("@RubricVersion", m.RubricVersion);
                p.Add("@InstructionsVersion", m.InstructionsVersion);
                p.Add("@TotalScore", m.TotalScore);
                p.Add("@OverallPercentage", m.OverallPercentage);
                p.Add("@Verdict", m.Verdict);
                p.Add("@SkillsScore", m.SkillsScore);
                p.Add("@WorkScore", m.WorkScore);
                p.Add("@EducationScore", m.EducationScore);
                p.Add("@AuthenticityRating", m.AuthenticityRating);
                p.Add("@AuthenticityFlagCount", m.AuthenticityFlagCount);
                p.Add("@ResponseJson", m.ResponseJson, DbType.String, size: -1);
                p.Add("@FactsJson", m.FactsJson, DbType.String, size: -1);
                p.Add("@ErrorMessage", Truncate(m.ErrorMessage, 1000), DbType.String, size: 1000);
                p.Add("@TriggeredBy", Truncate(m.TriggeredBy, 200));
                using (var con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_SaveCandidateAdvancedScreening", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<int> QueueAdvancedScreening(int irfId, string scope, int? candidateId, string triggeredBy)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return await con.ExecuteScalarAsync<int>("usp_QueueAdvancedScreening",
                        new { IRFId = irfId, Scope = scope, CandidateId = candidateId, TriggeredBy = Truncate(triggeredBy, 200) },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return -1;
            }
        }

        public async Task<CandidateAdvancedScreeningVM> GetCandidateAdvancedScreening(int candidateId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return (await con.QueryAsync<CandidateAdvancedScreeningVM>("usp_GetCandidateAdvancedScreening",
                        new { CandidateId = candidateId }, commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<AdvancedScreeningCounts> GetAdvancedScreeningCounts(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return (await con.QueryAsync<AdvancedScreeningCounts>("usp_GetAdvancedScreeningCounts",
                        new { IRFId = irfId }, commandType: CommandType.StoredProcedure)).FirstOrDefault()
                        ?? new AdvancedScreeningCounts();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CompanyVerificationRecord> GetCompanyVerification(string normalizedName)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return (await con.QueryAsync<CompanyVerificationRecord>("usp_GetCompanyVerification",
                        new { NormalizedName = normalizedName }, commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> SaveCompanyVerification(CompanyVerificationRecord r, bool setBlocklist)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@NormalizedName", r.NormalizedName);
                p.Add("@DisplayName", Truncate(r.DisplayName, 300));
                p.Add("@Found", r.Found);
                p.Add("@CIN", r.CIN);
                p.Add("@Status", r.Status);
                p.Add("@IncorporationDate", r.IncorporationDate, DbType.Date);
                p.Add("@StruckOffDate", r.StruckOffDate, DbType.Date);
                p.Add("@EmployeeBand", r.EmployeeBand);
                p.Add("@Website", r.Website);
                p.Add("@DomainAgeMonths", r.DomainAgeMonths);
                p.Add("@Source", r.Source);
                p.Add("@SetBlocklist", setBlocklist);
                p.Add("@IsBlocklisted", r.IsBlocklisted);
                p.Add("@BlocklistReason", r.BlocklistReason);
                p.Add("@BlocklistedBy", r.BlocklistedBy);
                using (var con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_SaveCompanyVerification", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SaveCandidateWorkHistory(int candidateId, int candidateMasterId, int irfId, string historyJson)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@CandidateId", candidateId);
                p.Add("@CandidateMasterId", candidateMasterId);
                p.Add("@IRFId", irfId);
                p.Add("@HistoryJson", historyJson, DbType.String, size: -1);
                using (var con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_SaveCandidateWorkHistory", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<CandidateWorkHistoryRecord>> GetCandidateWorkHistories(int candidateMasterId, int excludeCandidateId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    return await con.QueryAsync<CandidateWorkHistoryRecord>("usp_GetCandidateWorkHistories",
                        new { CandidateMasterId = candidateMasterId, ExcludeCandidateId = excludeCandidateId },
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Enumerable.Empty<CandidateWorkHistoryRecord>();
            }
        }

        public async Task<bool> DeleteResumeFingerprints(int candidateId)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_DeleteResumeFingerprints", new { CandidateId = candidateId },
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SaveResumeFingerprint(int candidateId, int? candidateMasterId, string sectionType, string sectionName,
            byte[] minHash, IEnumerable<(byte BandNo, long BandHash)> bands)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@CandidateId", candidateId);
                p.Add("@CandidateMasterId", candidateMasterId);
                p.Add("@SectionType", sectionType);
                p.Add("@SectionName", Truncate(sectionName, 300));
                p.Add("@MinHash", minHash, DbType.Binary, size: 512);
                p.Add("@Bands", BandTable(bands).AsTableValuedParameter("dbo.ResumeFingerprintBand_TableType"));
                using (var con = new SqlConnection(connectionString))
                {
                    await con.ExecuteAsync("usp_SaveResumeFingerprint", p, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<ResumeFingerprintMatch>> FindSimilarResumeFingerprints(int candidateId, int? candidateMasterId,
            IEnumerable<(byte BandNo, long BandHash)> bands)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("@CandidateId", candidateId);
                p.Add("@CandidateMasterId", candidateMasterId);
                p.Add("@Bands", BandTable(bands).AsTableValuedParameter("dbo.ResumeFingerprintBand_TableType"));
                using (var con = new SqlConnection(connectionString))
                {
                    return await con.QueryAsync<ResumeFingerprintMatch>("usp_FindSimilarResumeFingerprints", p,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Enumerable.Empty<ResumeFingerprintMatch>();
            }
        }

        private static DataTable BandTable(IEnumerable<(byte BandNo, long BandHash)> bands)
        {
            var table = new DataTable();
            table.Columns.Add("BandNo", typeof(byte));
            table.Columns.Add("BandHash", typeof(long));
            foreach (var (bandNo, bandHash) in bands ?? Enumerable.Empty<(byte, long)>())
            {
                table.Rows.Add(bandNo, bandHash);
            }
            return table;
        }

        private static string Truncate(string value, int max) =>
            value == null || value.Length <= max ? value : value.Substring(0, max);
    }
}
