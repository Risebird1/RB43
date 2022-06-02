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
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class MCQRepository : IMCQRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public MCQRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<string> AddorUpdateMCQSet(MCQVMset _lstset)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@PartitionKey", _lstset.PartitionKey);
                    parameters.Add("@Title", _lstset.Title);
                    parameters.Add("@CompentencyLevel", _lstset.CompentencyLevel);
                    parameters.Add("@PrimarySkills", _lstset.PrimarySkills);
                    parameters.Add("@ProfileType", _lstset.ProfileType);
                    parameters.Add("@SecondarySkill", _lstset.SecondarySkill);
                    parameters.Add("@Timer", _lstset.Timer);
                    parameters.Add("@QuestionCount", _lstset.QuestionCount);
                    parameters.Add("@id", _lstset.id);
                    parameters.Add("@mcqid", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    await SqlMapper.QueryAsync(con, "usp_AddorUpdateMCQSet", parameters,
                         commandType: CommandType.StoredProcedure);
                    var MCQId = parameters.Get<int>("@mcqid");
                    return MCQId.ToString();
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
        public async Task<string> AddMCQQuestions(List<MCQVM> _lstMCQ, string MCQId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable mcqQuestions = new DataTable();
                    mcqQuestions.Columns.Add("PartitionKey", typeof(string));
                    mcqQuestions.Columns.Add("QuestionType", typeof(string));
                    mcqQuestions.Columns.Add("Question", typeof(string));
                    mcqQuestions.Columns.Add("Option1", typeof(string));
                    mcqQuestions.Columns.Add("Option2", typeof(string));
                    mcqQuestions.Columns.Add("Option3", typeof(string));
                    mcqQuestions.Columns.Add("Option4", typeof(string));
                    mcqQuestions.Columns.Add("Answer", typeof(string));
                    mcqQuestions.Columns.Add("Explanation", typeof(string));
                    mcqQuestions.Columns.Add("MCQId", typeof(string));
                    mcqQuestions.Columns.Add("QuestionNumber", typeof(int));
                    foreach (var item in _lstMCQ)
                    {
                        mcqQuestions.Rows.Add(item.PartitionKey, item.QuestionType, item.Question, item.Option1, item.Option2, item.Option3, item.Option4, item.Answer, item.Explanation, item.MCQId, item.QuestionNumber);
                    }
                    SqlCommand cmd = new SqlCommand("usp_MCQQuestionsBulkInsert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@MCQQuestionsBulkData", SqlDbType.Structured));
                    cmd.Parameters["@MCQQuestionsBulkData"].Value = mcqQuestions;
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    var parameters = new DynamicParameters();
                    return "Success";
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
            return "Fail";
        }
        public async Task<IEnumerable<MCQVMset>> FetchAllMCQSets()
        {
            try
            {
                var query = "Select * From MCQSets";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MCQVMset>(query, null);
                    return results;
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
        public async Task<IEnumerable<MCQVMset>> FetchMCQSetById(string MCQPId)
        {
            try
            {
                var query = "Select * From MCQSets where PartitionKey=@PartitionKey";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@PartitionKey", MCQPId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MCQVMset>(query, dynamicParameters);
                    return results;
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
        public async Task<string> AddMCQSetShareList(MCQVMShare _list)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@PartitionKey", _list.PartitionKey);
                    parameters.Add("@ApplyingCompany", _list.ApplyingCompany);
                    parameters.Add("@ClientHR", _list.ClientHR);
                    parameters.Add("@EmailAddress", _list.EmailAddress);
                    parameters.Add("@MCQId", _list.MCQId);
                    parameters.Add("@MCQSet", _list.MCQSet);
                    parameters.Add("@Videorecording", _list.Videorecording);
                    parameters.Add("@status", _list.status);
                    parameters.Add("@roomId", _list.roomId);

                    await SqlMapper.QueryAsync(con, "usp_AddMCQSetShareCandidate", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Success";
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
        public async Task<IEnumerable<MCQVMShare>> GetShareMCQEmailListByMCQId(string MCQPId)
        {
            try
            {
                var query = "Select * From MCQShareList where MCQId=@MCQPId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@MCQPId", MCQPId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MCQVMShare>(query, dynamicParameters);
                    return results;
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
        public async Task<string> UpdateShareMCQEmailStatusByid(int id, int status)
        {
            try
            {
                string query = "Update MCQShareList set status=@status where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@status", status);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var resultset = con.Query(query, dynamicParameters);
                    return "Success";
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
            return "Fail";
        }
        public async Task<IEnumerable<CandidateDetailsForTest>> GetMCQCandidateListByMCQId(string MCQPId)
        {
            try
            {
                var query = "Select * From MCQCandidates where MCQId=@MCQPId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@MCQPId", MCQPId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<CandidateDetailsForTest>(query, dynamicParameters);
                    return results;
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
        public async Task<IEnumerable<MCQVM>> GetMCQquestionsByMCQId(string MCQPId)
        {
            try
            {
                var query = "Select * From MCQquestions where MCQId=@MCQPId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@MCQPId", MCQPId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MCQVM>(query, dynamicParameters);
                    return results;
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
        public async Task<string> AddMCQCandidates(CandidateDetailsForTest _list)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@PartitionKey", _list.PartitionKey);
                    parameters.Add("@MCQId", _list.MCQId);
                    parameters.Add("@Skills", _list.Skills);
                    parameters.Add("@TotalExprience", _list.TotalExprience);
                    parameters.Add("@CandidateEmail", _list.CandidateEmail);
                    parameters.Add("@ApplyingCompany", _list.ApplyingCompany);
                    parameters.Add("@ClientHR", _list.ClientHR);
                    parameters.Add("@CandidateScore", _list.CandidateScore);
                    parameters.Add("@TotalQuestions", _list.TotalQuestions);
                    parameters.Add("@VideoLink", _list.VideoLink);
                    parameters.Add("@FeedbackComment", _list.FeedbackComment);
                    parameters.Add("@Feedbackreating", _list.Feedbackreating);

                    await SqlMapper.QueryAsync(con, "usp_AddMCQCandidates", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Success";
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
        public async Task<string> AddMCQCandidateQuestions(List<MCQVM> _question)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable mcqQuestions = new DataTable();
                    mcqQuestions.Columns.Add("PartitionKey", typeof(string));
                    mcqQuestions.Columns.Add("QuestionType", typeof(string));
                    mcqQuestions.Columns.Add("Question", typeof(string));
                    mcqQuestions.Columns.Add("Option1", typeof(string));
                    mcqQuestions.Columns.Add("Option2", typeof(string));
                    mcqQuestions.Columns.Add("Option3", typeof(string));
                    mcqQuestions.Columns.Add("Option4", typeof(string));
                    mcqQuestions.Columns.Add("Answer", typeof(string));
                    mcqQuestions.Columns.Add("Explanation", typeof(string));
                    mcqQuestions.Columns.Add("MCQId", typeof(string));
                    mcqQuestions.Columns.Add("QuestionNumber", typeof(int));
                    mcqQuestions.Columns.Add("CandidateID", typeof(string));
                    mcqQuestions.Columns.Add("MultiAnswerOption1", typeof(bool));
                    mcqQuestions.Columns.Add("MultiAnswerOption2", typeof(bool));
                    mcqQuestions.Columns.Add("MultiAnswerOption3", typeof(bool));
                    mcqQuestions.Columns.Add("MultiAnswerOption4", typeof(bool));
                    mcqQuestions.Columns.Add("QuestionId", typeof(string));
                    mcqQuestions.Columns.Add("Rating", typeof(int));
                    mcqQuestions.Columns.Add("QuestionAnswer", typeof(string));
                    mcqQuestions.Columns.Add("SingleAnswer", typeof(string));
                    foreach (var item in _question)
                    {
                        mcqQuestions.Rows.Add(item.PartitionKey, item.QuestionType, item.Question, item.Option1, item.Option2, item.Option3, item.Option4, item.Answer, item.Explanation, item.MCQId, item.QuestionNumber,
                            item.CandidateID, item.MultiAnswerOption1, item.MultiAnswerOption2, item.MultiAnswerOption3, item.MultiAnswerOption4, item.QuestionId, item.Rating, item.QuestionAnswer, item.SingleAnswer);
                    }
                    SqlCommand cmd = new SqlCommand("usp_MCQCandidateQuestionsBulkInsert", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@MCQCandidateQuestionsBulkData", SqlDbType.Structured));
                    cmd.Parameters["@MCQCandidateQuestionsBulkData"].Value = mcqQuestions;
                    con.Open();
                    if (cmd.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return "Success";
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
        public async Task<string> DeleteMCQCandidate(string CSPId, string CPId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SharePartitionKey", CSPId);
                    parameters.Add("@CandidatePartitionKey", CPId);
                    await SqlMapper.QueryAsync(con, "usp_DeleteMCQCandidate", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Success";
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
        public async Task<string> DeleteMCQSet(string MCQPId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@MCQPId", MCQPId);
                    await SqlMapper.QueryAsync(con, "usp_DeleteMCQSetDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return "Success";
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
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQId(string MCQPId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@MCQPId", MCQPId);
                    var result = await SqlMapper.QueryAsync<CandidateDetailsForTestDuplicate>(con, "usp_MCQCandidateDetailsByMCQId", parameters,
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
            return null;
        }
        public async Task<IEnumerable<CandidateDetailsForTestDuplicate>> GetMCQCandidateDetailsByMCQTitle(string MCQtitle)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@MCQtitle", MCQtitle);
                    var result = await SqlMapper.QueryAsync<CandidateDetailsForTestDuplicate>(con, "usp_MCQCandidateDetailsByMCQtitle", parameters,
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
            return null;
        }
        public async Task<IEnumerable<MCQVM>> GetMCQCandidateQuestions(string CandidateId)
        {
            try
            {
                var query = "Select * From MCQCandidateQuestionAnswer where CandidateId=@CandidateId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@CandidateId", CandidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<MCQVM>(query, dynamicParameters);
                    return results;
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
        public async Task<string> UpdateVideoURLForCandidate(CandidateDetailsForTestDuplicate model)
        {
            try
            {
                var query = "Update MCQCandidates set VideoLink=@VideoLink where PartitionKey=@PartitionKey";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@PartitionKey", model.Id);
                dynamicParameters.Add("@VideoLink", model.VideoLink);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync(query, dynamicParameters);
                    return "Success";
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
    }
}
