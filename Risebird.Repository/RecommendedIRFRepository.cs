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
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class RecommendedIRFRepository : IRecommendedIRFRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RecommendedIRFRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public async Task<IEnumerable<SkillCahnnelDetailsVM>> GetFetchRecommendedIRF()
        {
            try
            {
                var query = "Select ir.id,IRF.IRFId,ag.AssessmentGroup,ir.AssessmentGroupId,ir.CompetencyLevelId,ir.CountryType,ir.SeniorityLevelId from IRFRecommendations ir join ClonableSkillChannel cs on cs.id=ir.CloneIRFId join IRF on cs.CreatedFromIRFId=IRF.id left join AssessmentGroup ag on ag.id=ir.AssessmentGroupId order by ir.id desc";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SkillCahnnelDetailsVM>(query, null);
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
        public IRF FetchClonableIRF(int irfId)
        {
            try
            {
                var query = "Select ir.CompetencyLevelId as Heuristicsscore,ir.CountryType as JobCountry,AssessmentGroupId from IRFRecommendations ir where ir.CloneIRFId =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TransSkill> GetClonableIRFSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select tts.*, Skill.[Name] as SkillName from TransClonableIRFTechSkill tts join Skill on tts.SkillId=Skill.id  where ClonableIRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkill>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TransInterviewer> GetClonableIRFTransInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransInterviewer>(con, "usp_GetClonableIRFTransInterviewer", param,
                                            commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<TransHR> GetClonableIRFTransHR(int id)
        {
            try
            {
                var query = "select thr.*, HR.UserId from TransClonableIRFHR thr inner join HR  on thr.HRId = HR.id where ClonableIRFId=@irfId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransHR>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public IEnumerable<SkillComment> GetClonableIRFQuestiondiscussed(int id)
        {
            try
            {
                var query = "Select sk.Name as PrimarySkillInvite,ts.Question Comment From ClonableTemplateQuestions ts join Skill sk on sk.id=ts.Skill where ts.ClonableIRFId=@irfId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<SkillComment>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool AddRecommendationIRFDetails(IRFVm irfDetails, string loggedInUser, out int irfID)
        {
            try
            {
                DynamicParameters parameters = SetStoredProcedureParameterForIRF(irfDetails);
                parameters.Add("@CreatedOn", irfDetails.CreatedOn);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_AddIRFDetails", parameters,
                       commandType: CommandType.StoredProcedure);
                    irfID = parameters.Get<int>("@idIrf");

                    if (irfID > 0)
                    {
                        DynamicParameters Recparameters = new DynamicParameters();
                        Recparameters.Add("@IRFId", irfID);
                        Recparameters.Add("@CreatedBy", loggedInUser);
                        SqlMapper.Query(con, "usp_AddRecommendedIRFDetails", Recparameters,
                       commandType: CommandType.StoredProcedure);


                        DataTable techSkillDt = new DataTable();
                        techSkillDt.Columns.Add("IRFId", typeof(int));
                        techSkillDt.Columns.Add("LevelId", typeof(int));
                        techSkillDt.Columns.Add("SkillId", typeof(int));
                        techSkillDt.Columns.Add("Weightage", typeof(int));
                        techSkillDt.Columns.Add("Concepts", typeof(string));
                        techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                        techSkillDt.Columns.Add("Timelimit", typeof(int));

                        foreach (var item in irfDetails.SkillCollection)
                        {
                            techSkillDt.Rows.Add(irfID, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                        }

                        SqlCommand cmdTechSkill = new SqlCommand("usp_TechSkillBulkInsert", con);
                        cmdTechSkill.CommandType = CommandType.StoredProcedure;
                        cmdTechSkill.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdTechSkill.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                        con.Open();
                        if (cmdTechSkill.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() > 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                        {
                            DataTable interviewerDt = new DataTable();
                            interviewerDt.Columns.Add("IRFId", typeof(int));
                            interviewerDt.Columns.Add("UserId", typeof(int));
                            interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                            interviewerDt.Columns.Add("IsActive", typeof(bool));
                            interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                            foreach (var item in irfDetails.InterviewerCollection)
                            {
                                interviewerDt.Rows.Add(irfID, item.UserId, item.PerSuccessRate, irfDetails.IsClone ? item.IsActive : true, irfDetails.IsClone ? item.IsAvailable : true);
                            }

                            SqlCommand cmdInterviewer = new SqlCommand("usp_InterviewerBulkInsert", con);
                            cmdInterviewer.CommandType = CommandType.StoredProcedure;
                            cmdInterviewer.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                            cmdInterviewer.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                            con.Open();
                            if (cmdInterviewer.ExecuteNonQuery() != 0)
                            {
                                con.Close();
                            }
                        }

                        //update trans Client table
                        DataTable clientUserDt = new DataTable();
                        clientUserDt.Columns.Add("IRFId", typeof(int));
                        clientUserDt.Columns.Add("ClientUserId", typeof(int));

                        foreach (var item in irfDetails.ClientCollection)
                        {
                            clientUserDt.Rows.Add(irfID, item.ClientUserId);
                        }

                        SqlCommand cmdClientUser = new SqlCommand("usp_ClientUserBulkInsert", con);
                        cmdClientUser.CommandType = CommandType.StoredProcedure;
                        cmdClientUser.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdClientUser.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                        con.Open();
                        if (cmdClientUser.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        //DataTable hRDt = new DataTable();
                        //hRDt.Columns.Add("IRFId", typeof(int));
                        //hRDt.Columns.Add("UserId", typeof(int));

                        //foreach (var item in irfDetails.HRCollection)
                        //{
                        //    hRDt.Rows.Add(irfID, item.UserId);
                        //}

                        //SqlCommand cmdHR = new SqlCommand("usp_HRBulkInsert", con);
                        //cmdHR.CommandType = CommandType.StoredProcedure;
                        //cmdHR.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                        //cmdHR.Parameters["@HRBulkData"].Value = hRDt;
                        //con.Open();
                        //if (cmdHR.ExecuteNonQuery() != 0)
                        //{
                        //    con.Close();
                        //}

                        //update question table
                        DataTable techSkillCommentsDt = new DataTable();
                        techSkillCommentsDt.Columns.Add("IRFId", typeof(int));
                        techSkillCommentsDt.Columns.Add("Skill", typeof(int));
                        techSkillCommentsDt.Columns.Add("Comment", typeof(string));
                        techSkillCommentsDt.Columns.Add("UserId", typeof(int));

                        foreach (var item in irfDetails.QuesionList)
                        {
                            techSkillCommentsDt.Rows.Add(irfID, Convert.ToInt16(item.Skill), item.Comment, item.UserId);
                        }

                        SqlCommand cmdAddSkillComments = new SqlCommand("usp_AddTechSkillComments", con);
                        cmdAddSkillComments.CommandType = CommandType.StoredProcedure;
                        cmdAddSkillComments.Parameters.Add(new SqlParameter("@TechSkillCommentsBulkData", SqlDbType.Structured));
                        cmdAddSkillComments.Parameters["@TechSkillCommentsBulkData"].Value = techSkillCommentsDt;
                        cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int));
                        cmdAddSkillComments.Parameters["@IRFId"].Value = irfID;
                        cmdAddSkillComments.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int));
                        cmdAddSkillComments.Parameters["@userId"].Value = 5;
                        con.Open();
                        if (cmdAddSkillComments.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                irfID = 0;
                return false;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                irfID = 0;
                return false;
            }
        }
        public async Task<bool> UpdateRecommendationIRFDetails(IRFVm irfDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    int irfID = irfDetails.id;
                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));

                    foreach (var item in irfDetails.SkillCollection)
                    {
                        techSkillDt.Rows.Add(irfID, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                    }

                    SqlCommand cmdTechSkill = new SqlCommand("usp_TechSkillBulkInsert", con);
                    cmdTechSkill.CommandType = CommandType.StoredProcedure;
                    cmdTechSkill.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                    cmdTechSkill.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    con.Open();
                    if (cmdTechSkill.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() > 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        DataTable interviewerDt = new DataTable();
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in irfDetails.InterviewerCollection)
                        {
                            interviewerDt.Rows.Add(irfID, item.UserId, item.PerSuccessRate, irfDetails.IsClone ? item.IsActive : true, irfDetails.IsClone ? item.IsAvailable : true);
                        }

                        SqlCommand cmdInterviewer = new SqlCommand("usp_InterviewerBulkInsert", con);
                        cmdInterviewer.CommandType = CommandType.StoredProcedure;
                        cmdInterviewer.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdInterviewer.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                        con.Open();
                        if (cmdInterviewer.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
                    }

                    //update trans Client table
                    DataTable clientUserDt = new DataTable();
                    clientUserDt.Columns.Add("IRFId", typeof(int));
                    clientUserDt.Columns.Add("ClientUserId", typeof(int));

                    foreach (var item in irfDetails.ClientCollection)
                    {
                        clientUserDt.Rows.Add(irfID, item.ClientUserId);
                    }

                    SqlCommand cmdClientUser = new SqlCommand("usp_ClientUserBulkInsert", con);
                    cmdClientUser.CommandType = CommandType.StoredProcedure;
                    cmdClientUser.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                    cmdClientUser.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    con.Open();
                    if (cmdClientUser.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    //DataTable hRDt = new DataTable();
                    //hRDt.Columns.Add("IRFId", typeof(int));
                    //hRDt.Columns.Add("UserId", typeof(int));

                    //foreach (var item in irfDetails.HRCollection)
                    //{
                    //    hRDt.Rows.Add(irfID, item.UserId);
                    //}

                    //SqlCommand cmdHR = new SqlCommand("usp_HRBulkInsert", con);
                    //cmdHR.CommandType = CommandType.StoredProcedure;
                    //cmdHR.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                    //cmdHR.Parameters["@HRBulkData"].Value = hRDt;
                    //con.Open();
                    //if (cmdHR.ExecuteNonQuery() != 0)
                    //{
                    //    con.Close();
                    //}

                    //update question table
                    DataTable techSkillCommentsDt = new DataTable();
                    techSkillCommentsDt.Columns.Add("IRFId", typeof(int));
                    techSkillCommentsDt.Columns.Add("Skill", typeof(int));
                    techSkillCommentsDt.Columns.Add("Comment", typeof(string));
                    techSkillCommentsDt.Columns.Add("UserId", typeof(int));

                    foreach (var item in irfDetails.QuesionList)
                    {
                        techSkillCommentsDt.Rows.Add(irfID, Convert.ToInt16(item.Skill), item.Comment, item.UserId);
                    }

                    SqlCommand cmdAddSkillComments = new SqlCommand("usp_AddTechSkillComments", con);
                    cmdAddSkillComments.CommandType = CommandType.StoredProcedure;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@TechSkillCommentsBulkData", SqlDbType.Structured));
                    cmdAddSkillComments.Parameters["@TechSkillCommentsBulkData"].Value = techSkillCommentsDt;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int));
                    cmdAddSkillComments.Parameters["@IRFId"].Value = irfID;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int));
                    cmdAddSkillComments.Parameters["@userId"].Value = 5;
                    con.Open();
                    if (cmdAddSkillComments.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }
                    return true;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        private DynamicParameters SetStoredProcedureParameterForIRF(IRFVm entity)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@IRFId", entity.IRFId);
            parameters.Add("@ClientName", entity.ClientName);
            parameters.Add("@JDTiltle", entity.JDTiltle);
            parameters.Add("@CompanyType", entity.CompanyType);
            parameters.Add("@ClientId", entity.ClientId);
            parameters.Add("@JobCountry", entity.JobCountry);
            parameters.Add("@StartDate", entity.StartDate);
            parameters.Add("@EndDate", entity.EndDate);
            parameters.Add("@JDLink", entity.JDLink);
            parameters.Add("@NoOfPosition", entity.NoOfPosition);
            parameters.Add("@NoOfIntterview", entity.NoOfIntterview);
            parameters.Add("@RisebirdHr", entity.RisebirdHr);
            parameters.Add("@PerMintuteRate", entity.PerMintuteRate);
            parameters.Add("@JobLevel", entity.JobLevel);
            parameters.Add("@CalendlyLink", entity.CalendlyLink);
            parameters.Add("@TotalBudget", entity.TotalBudget);
            parameters.Add("@IsBiometricEnabled", entity.IsBiometricEnabled);
            parameters.Add("@PriorityLevel", entity.PriorityLevel);
            parameters.Add("@CompanyDetails", entity.CompanyDetails);
            parameters.Add("@Type", entity.Type);
            parameters.Add("@Heuristicsscore", entity.Heuristicsscore);
            parameters.Add("@PrimarySkillInvite", entity.PrimarySkillInvite);
            parameters.Add("@BillingOwner", entity.BillingOwner);
            parameters.Add("@AssessmentGroupId", entity.AssessmentGroupId);
            parameters.Add("@IsQCRequired", entity.IsQCRequired);
            parameters.Add("@IsJoinabilitySelectabilityRequired", entity.IsJoinabilitySelectabilityRequired);
            parameters.Add("@IsGeneralSkillRequired", entity.IsGeneralSkillRequired);
            parameters.Add("@idIrf", dbType: DbType.Int32, direction: ParameterDirection.Output);

            return parameters;
        }
        public async Task<IEnumerable<IRF>> GetAllRecommendedIRFs(string emailID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetAllRecommendedIRF", parameters, commandTimeout: 600,
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
    }
}
