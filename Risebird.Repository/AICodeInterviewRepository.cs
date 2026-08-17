using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
    public class AICodeInterviewRepository : IAICodeInterviewRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AICodeInterviewRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<List<AICodeInterviewQCItemVM>> GetOpenQCItems(string expertEmail)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailID", expertEmail);
                    var data = await SqlMapper.QueryAsync<AICodeInterviewQCItemVM>(con,
                        "usp_GetOpenAICodeInterviewQCItems", parameters,
                        commandType: CommandType.StoredProcedure);
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<AICodeInterviewQCItemVM>();
        }

        public async Task<List<AICodeInterviewQCItemVM>> GetMyQCItems(string expertEmail, bool? isCompleted = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailID", expertEmail);
                    parameters.Add("@isCompleted", isCompleted);
                    var data = await SqlMapper.QueryAsync<AICodeInterviewQCItemVM>(con,
                        "usp_GetMyAICodeInterviewQCItems", parameters,
                        commandType: CommandType.StoredProcedure);
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<AICodeInterviewQCItemVM>();
        }

        public async Task<(int AssignmentId, int Status)> AssignExpert(string sessionId, string expertEmail, string assignedBy)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@ExpertEmail", expertEmail);
                    parameters.Add("@AssignedBy", assignedBy);
                    var row = await SqlMapper.QueryFirstOrDefaultAsync<AssignResult>(con,
                        "usp_AssignAICodeInterviewExpert", parameters,
                        commandType: CommandType.StoredProcedure);
                    if (row != null)
                    {
                        return (row.AssignmentId, row.Status);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return (0, 0);
        }

        public async Task<int> SaveReport(AICodeInterviewReportVM report, string user)
        {
            if (report == null) return 0;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", report.SessionId);
                    parameters.Add("@InterviewId", report.InterviewId == 0 ? (int?)null : report.InterviewId);
                    parameters.Add("@ReportSource", report.ReportSource);
                    parameters.Add("@OverallVerdict", report.OverallVerdict);
                    parameters.Add("@OverallComment", report.OverallComment);
                    parameters.Add("@OverallCommunicationRating", report.OverallCommunicationRating);
                    parameters.Add("@OverallCommunicationComment", report.OverallCommunicationComment);
                    parameters.Add("@RubricRatingsJson", report.RubricRatingsJson);
                    parameters.Add("@QuestionRatingsJson", SerializeQuestionRatings(report.QuestionRatings));
                    parameters.Add("@User", user);

                    return await SqlMapper.ExecuteScalarAsync<int>(con,
                        "usp_SaveAICodeInterviewReport", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return 0;
        }

        public async Task<AICodeInterviewAuditReportVM> GetAuditReport(string sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);

                    using (var multi = await SqlMapper.QueryMultipleAsync(con,
                        "usp_GetAICodeInterviewAuditReport", parameters,
                        commandType: CommandType.StoredProcedure))
                    {
                        var model = (await multi.ReadFirstOrDefaultAsync<AICodeInterviewAuditReportVM>())
                                    ?? new AICodeInterviewAuditReportVM { SessionId = sessionId };

                        var reports = (await multi.ReadAsync<AICodeInterviewReportVM>()).ToList();
                        var ratings = (await multi.ReadAsync<AICodeInterviewReportQuestionRatingVM>()).ToList();
                        model.Assignment = await multi.ReadFirstOrDefaultAsync<AICodeInterviewExpertAssignmentVM>();
                        model.Discrepancies = (await multi.ReadAsync<AICodeInterviewReportDiscrepancyVM>()).ToList();

                        foreach (var report in reports)
                        {
                            report.QuestionRatings = ratings
                                .Where(r => r.ReportId == report.id)
                                .OrderBy(r => r.DisplayOrder)
                                .ToList();
                        }

                        model.AIReport = reports.FirstOrDefault(r => r.ReportSource == (int)AICodeInterviewReportSourceEnum.AI);
                        model.ExpertReport = reports.FirstOrDefault(r => r.ReportSource == (int)AICodeInterviewReportSourceEnum.Expert);
                        model.IsSubmitted = model.Assignment?.SubmittedAt != null;

                        return model;
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<(bool WasDiscrepancyFound, int MaxSeverity)> SaveDiscrepancies(
            string sessionId, int? assignmentId, int? expertReportId,
            List<AICodeInterviewReportDiscrepancyVM> discrepancies)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var payload = (discrepancies ?? new List<AICodeInterviewReportDiscrepancyVM>())
                        .Select(d => new
                        {
                            d.FieldType,
                            d.QuestionId,
                            d.DisplayOrder,
                            d.FieldKey,
                            d.AIValue,
                            d.ExpertValue,
                            d.IsDiscrepancy,
                            d.Severity
                        })
                        .ToList();

                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@AssignmentId", assignmentId);
                    parameters.Add("@ExpertReportId", expertReportId);
                    parameters.Add("@DiscrepanciesJson", JsonConvert.SerializeObject(payload));

                    var row = await SqlMapper.QueryFirstOrDefaultAsync<DiscrepancyOutcome>(con,
                        "usp_SaveAICodeInterviewDiscrepancies", parameters,
                        commandType: CommandType.StoredProcedure);
                    if (row != null)
                    {
                        return (row.WasDiscrepancyFound, row.MaxSeverity);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return (false, 0);
        }

        private static string SerializeQuestionRatings(List<AICodeInterviewReportQuestionRatingVM> ratings)
        {
            if (ratings == null || ratings.Count == 0) return null;

            var payload = ratings.Select(r => new
            {
                r.QuestionId,
                r.BotInterviewQuestionVideoId,
                r.DisplayOrder,
                r.QuestionText,
                r.Rating,
                r.Verdict,
                r.Comment,
                r.CommunicationRating,
                r.CommunicationComment,
                r.RubricRatingsJson,
                r.TranscriptText
            }).ToList();

            return JsonConvert.SerializeObject(payload);
        }

        private class AssignResult
        {
            public int AssignmentId { get; set; }
            public int Status { get; set; }
        }

        private class DiscrepancyOutcome
        {
            public bool WasDiscrepancyFound { get; set; }
            public int MaxSeverity { get; set; }
        }
    }
}
