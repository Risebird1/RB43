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
    public class SupportRepository : ISupportRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public SupportRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<List<Support>> GetAllSupportHR(string emailId = null)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Support>(con, "usp_GetSupportBasedOnProfile", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.ToList();

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddSupportHR(Support hrDetails)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Name", hrDetails.Name);
                parameters.Add("@EmailId", hrDetails.EmailId);
                parameters.Add("@MobileNumber", hrDetails.MobileNumber);
                parameters.Add("@WhatsappNumber", hrDetails.WhatsappNumber);
                parameters.Add("@AvailabilityHours", hrDetails.AvailabilityHours);
                parameters.Add("@OnVoiceCall", hrDetails.OnVoiceCall);
                parameters.Add("@OnWhatsapp", hrDetails.OnWhatsapp);
                parameters.Add("@OnEmail", hrDetails.OnEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_AddSupportHR", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
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
        public async Task<Support> GetSupportHR(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Support>(con, "usp_GetSupportHRById", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> UpdateSupportHR(Support hrDetails)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", hrDetails.id);
                parameters.Add("@Name", hrDetails.Name);
                parameters.Add("@EmailId", hrDetails.EmailId);
                parameters.Add("@MobileNumber", hrDetails.MobileNumber);
                parameters.Add("@WhatsappNumber", hrDetails.WhatsappNumber);
                parameters.Add("@AvailabilityHours", hrDetails.AvailabilityHours);
                parameters.Add("@OnVoiceCall", hrDetails.OnVoiceCall);
                parameters.Add("@OnWhatsapp", hrDetails.OnWhatsapp);
                parameters.Add("@OnEmail", hrDetails.OnEmail);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateSupportHR", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            return false;
        }

        public async Task<bool> DeleteSupportHR(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_DeleteSupportHR", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<List<string>> GetAllSupportHREmails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<string>(con, "usp_GetSupportHREmails",
                        commandType: CommandType.StoredProcedure);
                    return results.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> SaveRaisedIssueDetails(RaiseBugsVM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RequestType", model.RequestType);
                parameters.Add("@Comment", model.Comment);
                parameters.Add("@AttachmentURL", model.AttachmentURL);
                parameters.Add("@RequestedBy", model.RequestedBy);
                parameters.Add("@RequestedOn", DateTime.Now);
                parameters.Add("@Requester", model.Requester);
                parameters.Add("@interviewId", model.interviewId);
                parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_SaveRaisedIssueDetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    model.id = parameters.Get<int>("@NewId");
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<RaiseBugsVM>> GetRaisedIssueDetails(string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@profileName", profileName);
                    return await SqlMapper.QueryAsync<RaiseBugsVM>(con, "usp_GetRaisedIssues", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<bool> UpdateRaisedRequestStatus(RaiseBugsVM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", model.id);
                parameters.Add("@Comment", model.Comment);
                parameters.Add("@RequestedBy", model.RequestedBy);
                parameters.Add("@RequestedOn", DateTime.Now);
                parameters.Add("@ActionByRole", model.Requester);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateRaisedIssueDetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<IEnumerable<RaiseBugsVM>> GetMyRaisedIssues(string requestedBy)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RequestedBy", requestedBy);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<RaiseBugsVM>(con, "usp_GetMyRaisedIssues", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<RaiseBugsVM> GetRaisedIssueById(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<RaiseBugsVM>(con, "usp_GetRaisedIssueById", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> ReopenRaisedIssue(int id, string comment, string actionBy, string actionByRole)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@Comment", comment);
                parameters.Add("@ActionBy", actionBy);
                parameters.Add("@ActionByRole", actionByRole);
                parameters.Add("@ActionOn", DateTime.Now);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_ReopenRaisedIssue", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<IEnumerable<RaisedIssueHistoryVM>> GetRaisedIssueHistory(int raisedIssueId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@RaisedIssueId", raisedIssueId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<RaisedIssueHistoryVM>(con, "usp_GetRaisedIssueHistory", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<PaymentTicketReviewVM>> GetOpenPaymentTicketsForReview()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<PaymentTicketReviewVM>(con,
                        "usp_GetOpenPaymentRaisedIssues",
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveRaisedIssueAIResponse(int ticketId, string aiResponseJson,
            string statusComment, string closedBy, DateTime closedOn, bool closeTicket)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@TicketId", ticketId);
                parameters.Add("@AIReviewResponse", aiResponseJson);
                parameters.Add("@StatusComment", statusComment);
                parameters.Add("@ClosedBy", closedBy);
                parameters.Add("@ClosedOn", closedOn);
                parameters.Add("@CloseTicket", closeTicket ? 1 : 0);
                parameters.Add("@ActionByRole", "System");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_SaveRaisedIssueAIResponse", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> UpdateQCPenaltyFromAIReview(int interviewId, int revisedPenaltyPct,
            string updatedMergedDetails, string reasoningComment)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);
                parameters.Add("@RevisedPenaltyPct", revisedPenaltyPct);
                parameters.Add("@UpdatedMergedDetails", updatedMergedDetails);
                parameters.Add("@ReasoningComment", reasoningComment);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateQCPenaltyFromAIReview", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
    }
}
