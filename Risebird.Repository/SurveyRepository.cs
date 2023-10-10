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
using static Dapper.SqlMapper;

namespace Risebird.Repository
{
    public class SurveyRepository : ISurveyRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SurveyRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<List<ClientAudioFeedbackDetails>> FetchClientAudioFeedbackDetails()
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientAudioFeedbackDetails>(con, "usp_GetClientAudioFeedback",
                        commandType: CommandType.StoredProcedure);
                    return result?.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<List<ClientAudioFeedbackDetails>> FetchBroadcastMessages()
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<ClientAudioFeedbackDetails>(con, "usp_GetClientBroadcastMessages",
                        commandType: CommandType.StoredProcedure);
                    return result?.ToList();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<SurveyCandidateInterviewer> AddOrEditCandidateRating(SurveyVM model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailAddress", model.EmailAddress);
                dynamicParameters.Add("@Question1", model.Question1);
                dynamicParameters.Add("@Question2", model.Question2);
                dynamicParameters.Add("@Question3", model.Question3);
                dynamicParameters.Add("@Question4", model.Question4);
                dynamicParameters.Add("@Question5", model.Question5);
                dynamicParameters.Add("@Question6", model.Question6);
                dynamicParameters.Add("@Type", model.Type);
                dynamicParameters.Add("@interviewid", model.interviewid);
                dynamicParameters.Add("@Comment", model.CommentforInterviewer);
                dynamicParameters.Add("@Name", model.Name);
                dynamicParameters.Add("@Phone", model.Phone);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SurveyCandidateInterviewer>(con, "Usp_AddOrEditCandidateRating", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<SurveyVM>> GetCandidateRating(string emailid,string profileName)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                dynamicParameters.Add("@Profile", profileName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SurveyVM>(con, "usp_FetchCandidateFeedback", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<SurveyVM>> GetCandidateRatingByIRFId(string IRFId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SurveyVM>(con, "usp_FetchCandidateFeedbackByIRFId", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> CandidateFedbackForm(PaymentSLAVM model)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@CreatedOn",DateTime.Now);
                parameters.Add("@Rating",model.Rating);
                parameters.Add("@CandidateInteractionQuality", model.CandidateInteractionQuality);
                parameters.Add("@LimitedHandoncoding", model.LimitedHandoncoding);
                parameters.Add("@UnethicalPractices", model.UnethicalPractices);
                parameters.Add("@TimeManagement", model.TimeManagement);
                parameters.Add("@PoorCommunicationSkills", model.PoorCommunicationSkills);
                parameters.Add("@WeakInfrastructure", model.WeakInfrastructure);
                parameters.Add("@InterviewId", model.Interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_CandidateFeedbackDetails", parameters,
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

