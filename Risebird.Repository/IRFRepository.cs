using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class IRFRepository : IIRFRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public IRFRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public IEnumerable<Users> GetCientUsers()
        {
            IEnumerable<ClientUserDTO> clientUsers = new List<ClientUserDTO>();
            IEnumerable<Users> clients = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    clientUsers = SqlMapper.Query<ClientUserDTO>(
                        con, "select * from ClientUser", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in clientUsers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    clients = SqlMapper.Query<Users>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return clients;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Risebird.Entities.Data.ClientRole> GetClientRoles()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<ClientRole>(
                        con, "select * from ClientRole", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Client> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client = SqlMapper.Query<Client>(
                        con, "select * from Client", null, commandType: Text);
                }
                return client;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<InterviewerVM> GetInterviewers()
        {
            IEnumerable<InterviewerVM> interviewersVm = new List<InterviewerVM>();
            IEnumerable<Interviewer> interviewers = new List<Interviewer>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    interviewers = SqlMapper.Query<Interviewer>(
                        con, "select * from Interviewer where IsApproved=1", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in interviewers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    interviewersVm = SqlMapper.Query<InterviewerVM>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return interviewersVm;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<InterviewerVM> GetInterviewersforIRF()
        {
            IEnumerable<InterviewerVM> interviewersVm = new List<InterviewerVM>();
            IEnumerable<Interviewer> interviewers = new List<Interviewer>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    interviewers = SqlMapper.Query<Interviewer>(
                        con, "select * from Interviewer", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in interviewers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    interviewersVm = SqlMapper.Query<InterviewerVM>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return interviewersVm;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Level> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<LevelTech> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<LevelTech>(
                        con, "select * from LevelTech", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Interviewer> GetInterviewerSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(
                        con, "select intp.UserId,PrimarySkill = STUFF((SELECT distinct  ', ' + Name FROM (select tis.InterviewerProfileId, ski.Name from TransInterviewerSkill tis join Skill ski on tis.SkillId = ski.id ) b WHERE b.InterviewerProfileId = intp.Id FOR XML PATH('')), 1, 1, '') FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId  where intp.IsApproved=1  ", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<int> GetInterviewerRate(int userId, string companyType = null, int? countryType = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@userId", userId);
                    parameters.Add("@companyType", companyType);
                    parameters.Add("@countryType", countryType);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_GetInterviewerRate", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;

            }
        }
        public async Task<int> GetInterviewerQCRate(int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@userId", userId);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_GetInterviewerQCRate", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;

            }
        }

        public IEnumerable<Interviewer> GetIRFInterviewerSkills(string assessmentgroupid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@assessmentgroupid", assessmentgroupid);
                    return con.Query<Interviewer>(";With CTE AS(Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,',')) Select Distinct CTE.UserId,CTE.AssessmentGroupId, ul.EmailId  From CTE join Userlist ul on CTE.Userid=ul.id where CTE.value=@assessmentgroupid", dp);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<TemplateInterviewerData>> GetInterviewersByAssessmentGroup(string assessmentGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("assessmentGroupId", assessmentGroupId);
                    return await SqlMapper.QueryAsync<TemplateInterviewerData>(con, "usp_GetInterviewersByAssessmentGroup", parameters,
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

        public IEnumerable<Users> GetQualityController()
        {
            IEnumerable<QualityController> qualityControllers = new List<Entities.Data.QualityController>();
            IEnumerable<Users> users = new List<Users>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    qualityControllers = SqlMapper.Query<QualityController>(
                        con, "select * from QualityController", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in qualityControllers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    users = SqlMapper.Query<Users>(
                        con, "select * from UserList where  id in (" + ids + ")", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return users;
        }

        public IEnumerable<Skill> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Skill>(
                        con, "select * from Skill", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public Skill GetSkillById(int id)
        {
            try
            {
                var query = "select id, [Name] from Skill where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Skill>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public IEnumerable<IRF> GetAllIRF(string emailid, string profileName)
        {
            try
            {
                if (emailid != string.Empty && profileName == "Client")
                {
                    //"Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id inner join[dbo].[UserList] ul on ul.id = tc.ClientUserId where IsDeleted = @IsDeleted and ul.EmailId = @emailid Order by StartDate desc"
                    var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id inner join[dbo].[UserList] ul on ul.id = tc.ClientUserId where ul.EmailId = @emailid Order by EndDate desc";
                    var dynamicParameters = new DynamicParameters();
                    //dynamicParameters.Add("@IsDeleted", 0);
                    dynamicParameters.Add("@emailid", emailid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<IRF>(query, dynamicParameters);
                        return results;
                    }
                }
                else if (emailid != string.Empty && profileName == "Recruiter")
                {
                    var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id inner join [dbo].[TransRecruiter] tr on tr.IRFId = IRF.id inner join[dbo].[UserList] ul on ul.id = tr.UserId where ul.EmailId = @emailid Order by EndDate desc";
                    var dynamicParameters = new DynamicParameters();
                    //dynamicParameters.Add("@IsDeleted", 0);
                    dynamicParameters.Add("@emailid", emailid);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<IRF>(query, dynamicParameters);
                        return results;
                    }
                }
                else
                {
                    //"Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id where IsDeleted = @IsDeleted Order by StartDate desc"
                    var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id Order by EndDate desc";
                    var dynamicParameters = new DynamicParameters();
                    //dynamicParameters.Add("@IsDeleted", 0);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<IRF>(query, dynamicParameters);
                        return results;
                    }
                }


            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<IEnumerable<IRF>> GetAllIRFs(string emailID, string profileName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetAllIRF", parameters, commandTimeout: 600,
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

        public IRF EditIRF(int irfId)
        {
            try
            {
                var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id where IRF.id =@id";
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

        public IEnumerable<ResumeEvaluator> GetResumeEvaluators()
        {
            try
            {
                var query = "select re.UserId, ul.EmailId from ResumeEvaluator re join UserList ul on re.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ResumeEvaluator>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<string> GetResumeEvaluatorsMailIds(int irfId)
        {
            try
            {
                var query = "select ul.EmailId from TransResumeEvaluator tre join ResumeEvaluator re on tre.ResumeEvaluatorId= re.id join UserList ul on ul.id= re.UserId where IRFId =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<string> GetHRsMailIds(int irfId)
        {
            try
            {
                var query = "select ul.EmailId from TransHR thr join HR  on thr.HRId= HR.id join UserList ul on ul.id= HR.UserId where IRFId =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public IEnumerable<HR> GetHRs()
        {
            try
            {
                var query = "select HR.UserId, ul.EmailId from HR join UserList ul on HR.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<HR>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public string GetQCSkills(int userId)
        {
            try
            {
                var query = "Select PrimarySkill from QualityController where UserId = @UserId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", userId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.ToList().FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string GetClientUserRole(int userId)
        {
            try
            {
                var query = "select [Name] from ClientRole where id =(select RoleId from ClientUser where UserId = @UserId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", userId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.ToList().FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> GetTechSkillofSkillChannelByQuestion(string question, int irfId)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@Question", question);
                parameters.Add("@IRFId", irfId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetTechSkillofSkillChannelByQuestion", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetQuestionSkills(string id)
        {
            try
            {
                var query = "select [Name] from Skill where id=(select Skill from [TechSkillComments] where Comment = @id)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.ToList().FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> CheckUniqueness(string value, bool isCloneTemplate = false)
        {
            try
            {
                //var query = "Select * from IRF where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@value", value);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if(!isCloneTemplate)
                    {
                        var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFISCUniqueness", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                        if (result.Count() > 0)
                            return false;
                        else
                            return true;
                    }
                    else
                    {
                        var result = await SqlMapper.QueryAsync<string>(con, "usp_GetClonableSkillChannelNameUniqueness", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                        if (result.Count() > 0)
                            return false;
                        else
                            return true;
                    }
                    
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }
        public IRF GetIRFbyId(int id)
        {
            try
            {
                var query = "select IRF.*, Client.[Name] as ClientCompName, Client.GST as ClientGST, Client.OfficeAddress as ClientAddress from IRF join Client on IRF.ClientId=client.id where IRF.id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }
        public bool UpdateIRFInvites(int id, int totalInvites)
        {
            try
            {
                var query = "Update IRF set TotalInvites=@TotalInvites where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@TotalInvites", totalInvites);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }
        public bool UpdateCandidateInvites(int id, int totalInvites)
        {
            try
            {
                var InviteDate = DateTime.Now;
                var query = "Update Candidate set TotalInvites=@TotalInvites, InviteDate=@InviteDate where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                dynamicParameters.Add("@TotalInvites", totalInvites);
                dynamicParameters.Add("@InviteDate", InviteDate);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Candidate>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }


        public IEnumerable<Users> GetUsers()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Users>(
                        con, "select * from UserList", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public bool AddIRFDetails(IRFVm irfDetails, out int irfID)
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
                        if (irfDetails.Joinability != null && irfDetails.Joinability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
                        {
                            DataTable joinabilityDt = new DataTable();
                            joinabilityDt.Columns.Add("IRFId", typeof(int));
                            joinabilityDt.Columns.Add("LevelId", typeof(int));
                            joinabilityDt.Columns.Add("Type", typeof(string));
                            joinabilityDt.Columns.Add("Value", typeof(string));
                            joinabilityDt.Columns.Add("Weightage", typeof(int));


                            foreach (var item in irfDetails.Joinability)
                            {
                                joinabilityDt.Rows.Add(irfID, item.LevelId, item.Type, item.Value, item.Weightage);
                            }

                            SqlCommand cmdJoinability = new SqlCommand("usp_JoinabilityBulkInsert", con);
                            cmdJoinability.CommandType = CommandType.StoredProcedure;
                            cmdJoinability.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
                            cmdJoinability.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
                            con.Open();
                            if (cmdJoinability.ExecuteNonQuery() != 0)
                            {
                                con.Close();
                            }
                        }


                        if (irfDetails.Selectability != null && irfDetails.Selectability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
                        {
                            DataTable selectabilityDt = new DataTable();
                            selectabilityDt.Columns.Add("IRFId", typeof(int));
                            selectabilityDt.Columns.Add("LevelId", typeof(int));
                            selectabilityDt.Columns.Add("Type", typeof(string));
                            selectabilityDt.Columns.Add("Value", typeof(string));
                            selectabilityDt.Columns.Add("Weightage", typeof(int));


                            foreach (var item in irfDetails.Selectability)
                            {
                                selectabilityDt.Rows.Add(irfID, item.LevelId, item.Type, item.Value, item.Weightage);
                            }

                            SqlCommand cmdSelectability = new SqlCommand("usp_SelectabilityBulkInsert", con);
                            cmdSelectability.CommandType = CommandType.StoredProcedure;
                            cmdSelectability.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
                            cmdSelectability.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
                            con.Open();
                            if (cmdSelectability.ExecuteNonQuery() != 0)
                            {
                                con.Close();
                            }
                        }



                        //update general skill table
                        if (irfDetails.SkillGeneral != null && irfDetails.SkillGeneral.Count() > 0 && irfDetails.IsGeneralSkillRequired)
                        {
                            DataTable skillGeneralDt = new DataTable();
                            skillGeneralDt.Columns.Add("IRFId", typeof(int));
                            skillGeneralDt.Columns.Add("LevelId", typeof(int));
                            skillGeneralDt.Columns.Add("Type", typeof(string));
                            skillGeneralDt.Columns.Add("Value", typeof(string));
                            skillGeneralDt.Columns.Add("Weightage", typeof(int));


                            foreach (var item in irfDetails.SkillGeneral)
                            {
                                skillGeneralDt.Rows.Add(irfID, item.LevelId, item.Type, item.Value, item.Weightage);
                            }

                            SqlCommand cmdSkillGeneral = new SqlCommand("usp_SkillGeneralBulkInsert", con);
                            cmdSkillGeneral.CommandType = CommandType.StoredProcedure;
                            cmdSkillGeneral.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
                            cmdSkillGeneral.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
                            con.Open();
                            if (cmdSkillGeneral.ExecuteNonQuery() != 0)
                            {
                                con.Close();
                            }
                        }


                        //update tech skill table

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


                        //update trans interviewer table
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



                        //update trans emergency interviewer table

                        if (irfDetails.EmergencyInterviewerCollection != null && irfDetails.EmergencyInterviewerCollection.Count() > 0 && !(irfDetails.EmergencyInterviewerCollection.Where(x => x.UserId == 0).Any()))
                        {
                            DataTable emergencyInterviewerDt = new DataTable();
                            emergencyInterviewerDt.Columns.Add("IRFId", typeof(int));
                            emergencyInterviewerDt.Columns.Add("UserId", typeof(int));
                            emergencyInterviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                            emergencyInterviewerDt.Columns.Add("IsActive", typeof(bool));

                            foreach (var item in irfDetails.EmergencyInterviewerCollection)
                            {
                                emergencyInterviewerDt.Rows.Add(irfID, item.UserId, item.PerSuccessRate, irfDetails.IsClone ? item.IsActive : true);
                            }

                            SqlCommand cmdEmergencyInterviewer = new SqlCommand("usp_EmergencyInterviewerBulkInsert", con);
                            cmdEmergencyInterviewer.CommandType = CommandType.StoredProcedure;
                            cmdEmergencyInterviewer.Parameters.Add(new SqlParameter("@EmergencyInterviewerBulkData", SqlDbType.Structured));
                            cmdEmergencyInterviewer.Parameters["@EmergencyInterviewerBulkData"].Value = emergencyInterviewerDt;
                            con.Open();
                            if (cmdEmergencyInterviewer.ExecuteNonQuery() != 0)
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

                        //update trans QC table
                        if (irfDetails.QualityController != null && irfDetails.QualityController.Count() > 0 && !(irfDetails.QualityController.Where(x => x.UserId == 0).Any()) && irfDetails.IsQCRequired)
                        {
                            DataTable qualityControllerDt = new DataTable();
                            qualityControllerDt.Columns.Add("IRFId", typeof(int));
                            qualityControllerDt.Columns.Add("UserId", typeof(int));
                            qualityControllerDt.Columns.Add("PerSuccessRate", typeof(int));
                            qualityControllerDt.Columns.Add("IsActive", typeof(bool));


                            foreach (var item in irfDetails.QualityController)
                            {
                                qualityControllerDt.Rows.Add(irfID, item.UserId, item.PerSuccessRate, irfDetails.IsClone ? item.IsActive : true);
                            }

                            SqlCommand cmdQualityController = new SqlCommand("usp_QCBulkInsert", con);
                            cmdQualityController.CommandType = CommandType.StoredProcedure;
                            cmdQualityController.Parameters.Add(new SqlParameter("@QCBulkData", SqlDbType.Structured));
                            cmdQualityController.Parameters["@QCBulkData"].Value = qualityControllerDt;
                            con.Open();
                            if (cmdQualityController.ExecuteNonQuery() != 0)
                            {
                                con.Close();
                            }

                        }


                        //DataTable resumeEvaluatorDt = new DataTable();
                        //resumeEvaluatorDt.Columns.Add("IRFId", typeof(int));
                        //resumeEvaluatorDt.Columns.Add("UserId", typeof(int));

                        //foreach (var item in irfDetails.ResumeEvaluatorCollection)
                        //{
                        //    resumeEvaluatorDt.Rows.Add(irfID, item.UserId);
                        //}

                        //SqlCommand cmdResumeEvaluator = new SqlCommand("usp_ResumeEvaluatorBulkInsert", con);
                        //cmdResumeEvaluator.CommandType = CommandType.StoredProcedure;
                        //cmdResumeEvaluator.Parameters.Add(new SqlParameter("@ResumeEvaluatorBulkData", SqlDbType.Structured));
                        //cmdResumeEvaluator.Parameters["@ResumeEvaluatorBulkData"].Value = resumeEvaluatorDt;
                        //con.Open();
                        //if (cmdResumeEvaluator.ExecuteNonQuery() != 0)
                        //{
                        //    con.Close();
                        //}


                        DataTable hRDt = new DataTable();
                        hRDt.Columns.Add("IRFId", typeof(int));
                        hRDt.Columns.Add("UserId", typeof(int));

                        foreach (var item in irfDetails.HRCollection)
                        {
                            hRDt.Rows.Add(irfID, item.UserId);
                        }

                        SqlCommand cmdHR = new SqlCommand("usp_HRBulkInsert", con);
                        cmdHR.CommandType = CommandType.StoredProcedure;
                        cmdHR.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                        cmdHR.Parameters["@HRBulkData"].Value = hRDt;
                        con.Open();
                        if (cmdHR.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                        //DataTable recruiterDt = new DataTable();
                        //recruiterDt.Columns.Add("IRFId", typeof(int));
                        //recruiterDt.Columns.Add("UserId", typeof(int));

                        //foreach (var item in irfDetails.RecruiterCollection)
                        //{
                        //    recruiterDt.Rows.Add(irfID, item.UserId);
                        //}

                        //SqlCommand cmdRecruiter = new SqlCommand("usp_RecruiterBulkInsert", con);
                        //cmdRecruiter.CommandType = CommandType.StoredProcedure;
                        //cmdRecruiter.Parameters.Add(new SqlParameter("@RecruiterBulkData", SqlDbType.Structured));
                        //cmdRecruiter.Parameters["@RecruiterBulkData"].Value = recruiterDt;
                        //con.Open();
                        //if (cmdRecruiter.ExecuteNonQuery() != 0)
                        //{
                        //    con.Close();
                        //}
                        //DynamicParameters recomParameters = new DynamicParameters();
                        //recomParameters.Add("IRFId", irfID);
                        //recomParameters.Add("Status", irfDetails.IRFRecommendations.Status);
                        //recomParameters.Add("CategoryId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CategoryId : null);
                        //recomParameters.Add("CompetencyLevelId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CompetencyLevelId : null);
                        //recomParameters.Add("SeniorityLevelId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.SeniorityLevelId : null);
                        //recomParameters.Add("CountryType", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CountryType : null);
                        //recomParameters.Add("CreatedOn", DateTime.Now);
                        //SqlMapper.Query(con, "usp_AddIRFRecommendation", recomParameters,
                        //commandType: CommandType.StoredProcedure);

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

        //public async Task<bool> UpdateIRFDetails(IRFVm irfDetails, List<InterviewerSuccessRateChangeData> interviewerRateChangesList = null, List<RemovedInterviewersFromSkillsChannelData> removedInterviewers = null, List<AddedNewInterviewersInSkillsChannelData> addedNewInterviewers = null, List<RemovedTechSkillsFromIRF> removedTechSkills = null)
        //{
        //    try
        //    {

        //        DynamicParameters parameters = SetStoredProcedureParameterForIRF(irfDetails, true);
        //        parameters.Add("@id", irfDetails.id);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            await SqlMapper.QueryAsync<int>(con, "usp_UpdateIRFDetails", parameters,
        //               commandType: CommandType.StoredProcedure);


        //            //if (irfDetails.Joinability != null)
        //            //{
        //            //    foreach (var item in irfDetails.Joinability)
        //            //    {
        //            //        string sqlJoinability = "INSERT INTO TransJoinability (IRFId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
        //            //        var affectedRows = con.ExecuteAsync(sqlJoinability, new
        //            //        {
        //            //            IRFId = irfDetails.id,
        //            //            LevelId = item.LevelId,
        //            //            Type = item.Type,
        //            //            Value = item.Value,
        //            //            Weightage = item.Weightage
        //            //        }).Result;
        //            //    }
        //            //}

        //            try
        //            {
        //                if (irfDetails.Joinability != null && irfDetails.Joinability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
        //                {
        //                    DataTable joinabilityDt = new DataTable();
        //                    joinabilityDt.Columns.Add("IRFId", typeof(int));
        //                    joinabilityDt.Columns.Add("LevelId", typeof(int));
        //                    joinabilityDt.Columns.Add("Type", typeof(string));
        //                    joinabilityDt.Columns.Add("Value", typeof(string));
        //                    joinabilityDt.Columns.Add("Weightage", typeof(int));


        //                    foreach (var item in irfDetails.Joinability)
        //                    {
        //                        joinabilityDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
        //                    }

        //                    SqlCommand cmdJoinability = new SqlCommand("usp_JoinabilityBulkInsert", con);
        //                    cmdJoinability.CommandType = CommandType.StoredProcedure;
        //                    cmdJoinability.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
        //                    cmdJoinability.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
        //                    con.Open();
        //                    if (cmdJoinability.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //            }

        //            try
        //            {
        //                if (irfDetails.Selectability != null && irfDetails.Selectability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
        //                {
        //                    DataTable selectabilityDt = new DataTable();
        //                    selectabilityDt.Columns.Add("IRFId", typeof(int));
        //                    selectabilityDt.Columns.Add("LevelId", typeof(int));
        //                    selectabilityDt.Columns.Add("Type", typeof(string));
        //                    selectabilityDt.Columns.Add("Value", typeof(string));
        //                    selectabilityDt.Columns.Add("Weightage", typeof(int));

        //                    foreach (var item in irfDetails.Selectability)
        //                    {
        //                        selectabilityDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
        //                    }

        //                    SqlCommand cmdSelectability = new SqlCommand("usp_SelectabilityBulkInsert", con);
        //                    cmdSelectability.CommandType = CommandType.StoredProcedure;
        //                    cmdSelectability.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
        //                    cmdSelectability.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
        //                    con.Open();
        //                    if (cmdSelectability.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

        //            }
        //            //update general skill table

        //            //if (irfDetails.SkillGeneral != null)
        //            //{
        //            //    foreach (var item in irfDetails.SkillGeneral)
        //            //    {

        //            //        string sqlSkillGeneral = "INSERT INTO TransSkillGeneral (IRFId, LevelId, Type, Value, Weightage) Values (@IRFId, @LevelId, @Type, @Value, @Weightage);";
        //            //        var affectedRows = con.ExecuteAsync(sqlSkillGeneral, new
        //            //        {
        //            //            IRFId = irfDetails.id,
        //            //            LevelId = item.LevelId,
        //            //            Type = item.Type,
        //            //            Value = item.Value,
        //            //            Weightage = item.Weightage
        //            //        }).Result;
        //            //    }
        //            //}
        //            if (irfDetails.SkillGeneral != null && irfDetails.SkillGeneral.Count() > 0 && irfDetails.IsGeneralSkillRequired)
        //            {
        //                try
        //                {
        //                    DataTable skillGeneralDt = new DataTable();
        //                    skillGeneralDt.Columns.Add("IRFId", typeof(int));
        //                    skillGeneralDt.Columns.Add("LevelId", typeof(int));
        //                    skillGeneralDt.Columns.Add("Type", typeof(string));
        //                    skillGeneralDt.Columns.Add("Value", typeof(string));
        //                    skillGeneralDt.Columns.Add("Weightage", typeof(int));

        //                    foreach (var item in irfDetails.SkillGeneral)
        //                    {
        //                        skillGeneralDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
        //                    }

        //                    SqlCommand cmdSkillGeneral = new SqlCommand("usp_SkillGeneralBulkInsert", con);
        //                    cmdSkillGeneral.CommandType = CommandType.StoredProcedure;
        //                    cmdSkillGeneral.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
        //                    cmdSkillGeneral.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
        //                    con.Open();
        //                    if (cmdSkillGeneral.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                }
        //            }


        //            //update tech skill table
        //            //if (irfDetails.SkillCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.SkillCollection)
        //            //    {

        //            //        string sqlSkillTech = "INSERT INTO TransTechSkill (IRFId, LevelId, SkillId, Weightage) Values (@IRFId, @LevelId, @SkillId, @Weightage);";
        //            //        var affectedRows = con.Execute(sqlSkillTech, new
        //            //        {
        //            //            IRFId = irfDetails.id,
        //            //            LevelId = item.LevelId,
        //            //            SkillId = item.SkillId,
        //            //            Weightage = item.Weightage
        //            //        });
        //            //    }
        //            //}

        //            try
        //            {
        //                DataTable techSkillDt = new DataTable();
        //                techSkillDt.Columns.Add("IRFId", typeof(int));
        //                techSkillDt.Columns.Add("LevelId", typeof(int));
        //                techSkillDt.Columns.Add("SkillId", typeof(int));
        //                techSkillDt.Columns.Add("Weightage", typeof(int));
        //                techSkillDt.Columns.Add("Concepts", typeof(string));
        //                techSkillDt.Columns.Add("Natureofquestion", typeof(string));
        //                techSkillDt.Columns.Add("Timelimit", typeof(int));

        //                foreach (var item in irfDetails.SkillCollection)
        //                {
        //                    techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
        //                }

        //                SqlCommand cmdTechSkill = new SqlCommand("usp_TechSkillBulkInsert", con);
        //                cmdTechSkill.CommandType = CommandType.StoredProcedure;
        //                cmdTechSkill.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
        //                cmdTechSkill.Parameters["@TechSkillBulkData"].Value = techSkillDt;
        //                con.Open();
        //                if (cmdTechSkill.ExecuteNonQuery() != 0)
        //                {
        //                    con.Close();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //            }

        //            if (removedTechSkills != null)
        //            {
        //                foreach (var removedTechSkill in removedTechSkills)
        //                {
        //                    try
        //                    {
        //                        UpdateTechSkillQuestionsForRemovedSkills(removedTechSkill);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }

        //            //update trans interviewer table

        //            //if (irfDetails.InterviewerCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.InterviewerCollection)
        //            //    {
        //            //        DynamicParameters param = new DynamicParameters();
        //            //        param.Add("@irfIntId", irfDetails.id);
        //            //        param.Add("@InterviewerUserId", item.UserId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransInterviewer", param,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}

        //            if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
        //            {
        //                try
        //                {
        //                    DataTable interviewerDt = new DataTable();
        //                    interviewerDt.Columns.Add("IRFId", typeof(int));
        //                    interviewerDt.Columns.Add("UserId", typeof(int));
        //                    interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
        //                    interviewerDt.Columns.Add("IsActive", typeof(bool));
        //                    interviewerDt.Columns.Add("IsAvailable", typeof(bool));

        //                    foreach (var item in irfDetails.InterviewerCollection)
        //                    {
        //                        interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive, item.IsAvailable);
        //                    }

        //                    SqlCommand cmdInterviewer = new SqlCommand("usp_InterviewerBulkInsert", con);
        //                    cmdInterviewer.CommandType = CommandType.StoredProcedure;
        //                    cmdInterviewer.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
        //                    cmdInterviewer.Parameters["@InterviewerBulkData"].Value = interviewerDt;
        //                    con.Open();
        //                    if (cmdInterviewer.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                }
        //            }

        //            if (interviewerRateChangesList != null)
        //            {
        //                foreach (var interviewerChangeDetails in interviewerRateChangesList.Where(x => x.Type == "Interviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateInterviewerAmountBasedOnRateChanges(interviewerChangeDetails);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }

        //            if (removedInterviewers != null)
        //            {
        //                foreach (var removedInterviewer in removedInterviewers.Where(x => x.Type == "Interviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateInterviewerAmountForRemovedInterviewers(removedInterviewer);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }

        //            if (addedNewInterviewers != null)
        //            {
        //                foreach (var addedNewInterviewer in addedNewInterviewers.Where(x => x.Type == "Interviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateAmountForNewlyAddedInterviewers(addedNewInterviewer);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }
        //            // update trans emergency interviewers table
        //            //if (irfDetails.EmergencyInterviewerCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.EmergencyInterviewerCollection)
        //            //    {
        //            //        DynamicParameters param = new DynamicParameters();
        //            //        param.Add("@irfIntId", irfDetails.id);
        //            //        param.Add("@InterviewerUserId", item.UserId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransEmergencyInterviewer", param,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}

        //            if (irfDetails.EmergencyInterviewerCollection != null && irfDetails.EmergencyInterviewerCollection.Count() != 0 && !(irfDetails.EmergencyInterviewerCollection.Where(x => x.UserId == 0).Any()))
        //            {
        //                try
        //                {
        //                    DataTable emergencyInterviewerDt = new DataTable();
        //                    emergencyInterviewerDt.Columns.Add("IRFId", typeof(int));
        //                    emergencyInterviewerDt.Columns.Add("UserId", typeof(int));
        //                    emergencyInterviewerDt.Columns.Add("PerSuccessRate", typeof(int));
        //                    emergencyInterviewerDt.Columns.Add("IsActive", typeof(bool));


        //                    foreach (var item in irfDetails.EmergencyInterviewerCollection)
        //                    {
        //                        emergencyInterviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive);
        //                    }

        //                    SqlCommand cmdEmergencyInterviewer = new SqlCommand("usp_EmergencyInterviewerBulkInsert", con);
        //                    cmdEmergencyInterviewer.CommandType = CommandType.StoredProcedure;
        //                    cmdEmergencyInterviewer.Parameters.Add(new SqlParameter("@EmergencyInterviewerBulkData", SqlDbType.Structured));
        //                    cmdEmergencyInterviewer.Parameters["@EmergencyInterviewerBulkData"].Value = emergencyInterviewerDt;
        //                    con.Open();
        //                    if (cmdEmergencyInterviewer.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                }
        //            }

        //            if (interviewerRateChangesList != null)
        //            {
        //                foreach (var interviewerChangeDetails in interviewerRateChangesList.Where(x => x.Type == "EmergencyInterviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateInterviewerAmountBasedOnRateChanges(interviewerChangeDetails);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }

        //            if (removedInterviewers != null)
        //            {
        //                foreach (var removedEmergencyInterviewer in removedInterviewers.Where(x => x.Type == "EmergencyInterviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateInterviewerAmountForRemovedInterviewers(removedEmergencyInterviewer);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }
        //            if (addedNewInterviewers != null)
        //            {
        //                foreach (var addedNewInterviewer in addedNewInterviewers.Where(x => x.Type == "EmergencyInterviewer"))
        //                {
        //                    try
        //                    {
        //                        UpdateAmountForNewlyAddedInterviewers(addedNewInterviewer);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                        continue;
        //                    }
        //                }
        //            }
        //            //update trans Client table

        //            //if (irfDetails.ClientCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.ClientCollection)
        //            //    {
        //            //        DynamicParameters param1 = new DynamicParameters();
        //            //        param1.Add("@irfIntId", irfDetails.id);
        //            //        param1.Add("@ClientUserId", item.ClientUserId);
        //            //        //param1.Add("@ClientRoleId", item.ClientRoleId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransClientUser", param1,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}

        //            if (irfDetails.ClientCollection != null)
        //            {
        //                try
        //                {
        //                    DataTable clientUserDt = new DataTable();
        //                    clientUserDt.Columns.Add("IRFId", typeof(int));
        //                    clientUserDt.Columns.Add("ClientUserId", typeof(int));

        //                    foreach (var item in irfDetails.ClientCollection)
        //                    {
        //                        clientUserDt.Rows.Add(irfDetails.id, item.ClientUserId);
        //                    }


        //                    SqlCommand cmdClientUser = new SqlCommand("usp_ClientUserBulkInsert", con);
        //                    cmdClientUser.CommandType = CommandType.StoredProcedure;
        //                    cmdClientUser.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
        //                    cmdClientUser.Parameters["@ClientUserBulkData"].Value = clientUserDt;
        //                    con.Open();
        //                    if (cmdClientUser.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                }
        //            }


        //            //update trans QC table

        //            //if (irfDetails.QualityController != null)
        //            //{
        //            //    foreach (var item in irfDetails.QualityController)
        //            //    {
        //            //        DynamicParameters param2 = new DynamicParameters();
        //            //        param2.Add("@irfIntId", irfDetails.id);
        //            //        param2.Add("@InterviewerUserId", item.UserId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransQualityController", param2,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}
        //            if (irfDetails.QualityController != null && irfDetails.QualityController.Count() != 0 && !(irfDetails.QualityController.Where(x => x.UserId == 0).Any()) && irfDetails.IsQCRequired)
        //            {
        //                try
        //                {
        //                    DataTable qualityControllerDt = new DataTable();
        //                    qualityControllerDt.Columns.Add("IRFId", typeof(int));
        //                    qualityControllerDt.Columns.Add("UserId", typeof(int));
        //                    qualityControllerDt.Columns.Add("PerSuccessRate", typeof(int));
        //                    qualityControllerDt.Columns.Add("IsActive", typeof(bool));

        //                    foreach (var item in irfDetails.QualityController)
        //                    {
        //                        qualityControllerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive);
        //                    }

        //                    SqlCommand cmdQualityController = new SqlCommand("usp_QCBulkInsert", con);
        //                    cmdQualityController.CommandType = CommandType.StoredProcedure;
        //                    cmdQualityController.Parameters.Add(new SqlParameter("@QCBulkData", SqlDbType.Structured));
        //                    cmdQualityController.Parameters["@QCBulkData"].Value = qualityControllerDt;
        //                    con.Open();
        //                    if (cmdQualityController.ExecuteNonQuery() != 0)
        //                    {
        //                        con.Close();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //                }
        //            }


        //            //if (irfDetails.ResumeEvaluatorCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.ResumeEvaluatorCollection)
        //            //    {
        //            //        DynamicParameters param2 = new DynamicParameters();
        //            //        param2.Add("@irfId", irfDetails.id);
        //            //        param2.Add("@ResumeEvaluatorUserId", item.UserId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransResumeEvaluator", param2,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}

        //            //DataTable resumeEvaluatorDt = new DataTable();
        //            //resumeEvaluatorDt.Columns.Add("IRFId", typeof(int));
        //            //resumeEvaluatorDt.Columns.Add("UserId", typeof(int));

        //            //foreach (var item in irfDetails.ResumeEvaluatorCollection)
        //            //{
        //            //    resumeEvaluatorDt.Rows.Add(irfDetails.id, item.UserId);
        //            //}

        //            //SqlCommand cmdResumeEvaluator = new SqlCommand("usp_ResumeEvaluatorBulkInsert", con);
        //            //cmdResumeEvaluator.CommandType = CommandType.StoredProcedure;
        //            //cmdResumeEvaluator.Parameters.Add(new SqlParameter("@ResumeEvaluatorBulkData", SqlDbType.Structured));
        //            //cmdResumeEvaluator.Parameters["@ResumeEvaluatorBulkData"].Value = resumeEvaluatorDt;
        //            //con.Open();
        //            //if (cmdResumeEvaluator.ExecuteNonQuery() != 0)
        //            //{
        //            //    con.Close();
        //            //}

        //            //if (irfDetails.HRCollection != null)
        //            //{
        //            //    foreach (var item in irfDetails.HRCollection)
        //            //    {
        //            //        DynamicParameters param2 = new DynamicParameters();
        //            //        param2.Add("@irfId", irfDetails.id);
        //            //        param2.Add("@HRUserId", item.UserId);
        //            //        var result = await SqlMapper.QueryAsync<int>(con, "usp_AddTransHR", param2,
        //            //                commandType: CommandType.StoredProcedure);

        //            //    }
        //            //}

        //            try
        //            {
        //                DataTable hRDt = new DataTable();
        //                hRDt.Columns.Add("IRFId", typeof(int));
        //                hRDt.Columns.Add("UserId", typeof(int));

        //                foreach (var item in irfDetails.HRCollection)
        //                {
        //                    hRDt.Rows.Add(irfDetails.id, item.UserId);
        //                }

        //                SqlCommand cmdHR = new SqlCommand("usp_HRBulkInsert", con);
        //                cmdHR.CommandType = CommandType.StoredProcedure;
        //                cmdHR.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
        //                cmdHR.Parameters["@HRBulkData"].Value = hRDt;
        //                con.Open();
        //                if (cmdHR.ExecuteNonQuery() != 0)
        //                {
        //                    con.Close();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //            }



        //            return true;
        //        }

        //    }

        //    catch (SqlException e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}

        public async Task<bool> UpdateIRFDetails(IRFVm irfDetails, List<InterviewerSuccessRateChangeData> interviewerRateChangesList = null, List<RemovedInterviewersFromSkillsChannelData> removedInterviewers = null, List<AddedNewInterviewersInSkillsChannelData> addedNewInterviewers = null, List<RemovedTechSkillsFromIRF> removedTechSkills = null)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable joinabilityDt = new DataTable();
                    if (irfDetails.Joinability != null && irfDetails.Joinability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
                    {
                        //DataTable joinabilityDt = new DataTable();
                        joinabilityDt.Columns.Add("IRFId", typeof(int));
                        joinabilityDt.Columns.Add("LevelId", typeof(int));
                        joinabilityDt.Columns.Add("Type", typeof(string));
                        joinabilityDt.Columns.Add("Value", typeof(string));
                        joinabilityDt.Columns.Add("Weightage", typeof(int));


                        foreach (var item in irfDetails.Joinability)
                        {
                            joinabilityDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable selectabilityDt = new DataTable();
                    if (irfDetails.Selectability != null && irfDetails.Selectability.Count() > 0 && irfDetails.IsJoinabilitySelectabilityRequired)
                    {
                        selectabilityDt.Columns.Add("IRFId", typeof(int));
                        selectabilityDt.Columns.Add("LevelId", typeof(int));
                        selectabilityDt.Columns.Add("Type", typeof(string));
                        selectabilityDt.Columns.Add("Value", typeof(string));
                        selectabilityDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in irfDetails.Selectability)
                        {
                            selectabilityDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

                    DataTable skillGeneralDt = new DataTable();
                    if (irfDetails.SkillGeneral != null && irfDetails.SkillGeneral.Count() > 0 && irfDetails.IsGeneralSkillRequired)
                    {
                        skillGeneralDt.Columns.Add("IRFId", typeof(int));
                        skillGeneralDt.Columns.Add("LevelId", typeof(int));
                        skillGeneralDt.Columns.Add("Type", typeof(string));
                        skillGeneralDt.Columns.Add("Value", typeof(string));
                        skillGeneralDt.Columns.Add("Weightage", typeof(int));

                        foreach (var item in irfDetails.SkillGeneral)
                        {
                            skillGeneralDt.Rows.Add(irfDetails.id, item.LevelId, item.Type, item.Value, item.Weightage);
                        }

                    }

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
                        if (String.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (String.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit);
                    }

                    DataTable interviewerDt = new DataTable();

                    if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in irfDetails.InterviewerCollection)
                        {
                            interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive, item.IsAvailable);
                        }
                    }

                    DataTable emergencyInterviewerDt = new DataTable();
                    if (irfDetails.EmergencyInterviewerCollection != null && irfDetails.EmergencyInterviewerCollection.Count() != 0 && !(irfDetails.EmergencyInterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        emergencyInterviewerDt.Columns.Add("IRFId", typeof(int));
                        emergencyInterviewerDt.Columns.Add("UserId", typeof(int));
                        emergencyInterviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        emergencyInterviewerDt.Columns.Add("IsActive", typeof(bool));


                        foreach (var item in irfDetails.EmergencyInterviewerCollection)
                        {
                            emergencyInterviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive);
                        }

                    }

                    DataTable clientUserDt = new DataTable();

                    if (irfDetails.ClientCollection != null)
                    {
                        clientUserDt.Columns.Add("IRFId", typeof(int));
                        clientUserDt.Columns.Add("ClientUserId", typeof(int));

                        foreach (var item in irfDetails.ClientCollection)
                        {
                            clientUserDt.Rows.Add(irfDetails.id, item.ClientUserId);
                        }
                    }

                    DataTable qualityControllerDt = new DataTable();

                    if (irfDetails.QualityController != null && irfDetails.QualityController.Count() != 0 && !(irfDetails.QualityController.Where(x => x.UserId == 0).Any()) && irfDetails.IsQCRequired)
                    {
                        qualityControllerDt.Columns.Add("IRFId", typeof(int));
                        qualityControllerDt.Columns.Add("UserId", typeof(int));
                        qualityControllerDt.Columns.Add("PerSuccessRate", typeof(int));
                        qualityControllerDt.Columns.Add("IsActive", typeof(bool));

                        foreach (var item in irfDetails.QualityController)
                        {
                            qualityControllerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive);
                        }
                    }

                    DataTable hRDt = new DataTable();
                    hRDt.Columns.Add("IRFId", typeof(int));
                    hRDt.Columns.Add("UserId", typeof(int));

                    foreach (var item in irfDetails.HRCollection)
                    {
                        hRDt.Rows.Add(irfDetails.id, item.UserId);
                    }

                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_UpdateIRFDetails", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@id"].Value = irfDetails.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.NVarChar));
                    cmdUpdateIRF.Parameters["@IRFId"].Value = irfDetails.IRFId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientName", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@ClientName"].Value = irfDetails.ClientName;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDTiltle", SqlDbType.NVarChar));
                    cmdUpdateIRF.Parameters["@JDTiltle"].Value = irfDetails.JDTiltle;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyType"].Value = irfDetails.CompanyType;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@ClientId"].Value = irfDetails.ClientId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JobCountry", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JobCountry"].Value = irfDetails.JobCountry;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@StartDate"].Value = irfDetails.StartDate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@EndDate"].Value = irfDetails.EndDate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JDLink"].Value = irfDetails.JDLink ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfPosition", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfPosition"].Value = irfDetails.NoOfPosition;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfIntterview", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfIntterview"].Value = irfDetails.NoOfIntterview;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RisebirdHr", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RisebirdHr"].Value = irfDetails.RisebirdHr;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PerMintuteRate", SqlDbType.Real));
                    cmdUpdateIRF.Parameters["@PerMintuteRate"].Value = irfDetails.PerMintuteRate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JobLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@JobLevel"].Value = irfDetails.JobLevel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CalendlyLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CalendlyLink"].Value = irfDetails.CalendlyLink;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TotalBudget", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@TotalBudget"].Value = irfDetails.TotalBudget;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsBiometricEnabled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsBiometricEnabled"].Value = irfDetails.IsBiometricEnabled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PriorityLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@PriorityLevel"].Value = irfDetails.PriorityLevel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyDetails", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyDetails"].Value = irfDetails.CompanyDetails ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Type"].Value = irfDetails.Type;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Heuristicsscore", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@Heuristicsscore"].Value = irfDetails.Heuristicsscore;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PrimarySkillInvite", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@PrimarySkillInvite"].Value = irfDetails.PrimarySkillInvite;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@BillingOwner", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@BillingOwner"].Value = irfDetails.BillingOwner;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@AssessmentGroupId"].Value = irfDetails.AssessmentGroupId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsQCRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsQCRequired"].Value = irfDetails.IsQCRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsJoinabilitySelectabilityRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsJoinabilitySelectabilityRequired"].Value = irfDetails.IsJoinabilitySelectabilityRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsGeneralSkillRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsGeneralSkillRequired"].Value = irfDetails.IsGeneralSkillRequired;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@idRecommendation", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@idRecommendation"].Value = irfDetails.IRFRecommendations.id;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationStatus", SqlDbType.Bit));
                    //cmdUpdateIRF.Parameters["@RecommendationStatus"].Value = irfDetails.IRFRecommendations.Status;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCategoryId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@RecommendationCategoryId"].Value = irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CategoryId : null;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCompetencyLevelId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@RecommendationCompetencyLevelId"].Value = irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CompetencyLevelId : null;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationSeniorityLevelId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@RecommendationSeniorityLevelId"].Value = irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.SeniorityLevelId : null;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationCountryType", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@RecommendationCountryType"].Value = irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CountryType : null;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@RecommendationModifiedOn", SqlDbType.DateTime));
                    //cmdUpdateIRF.Parameters["@RecommendationModifiedOn"].Value = DateTime.Now;


                    //DynamicParameters recomParameters = new DynamicParameters();
                    //recomParameters.Add("id", irfDetails.IRFRecommendations.id);
                    //recomParameters.Add("IRFId", irfDetails.id);
                    //recomParameters.Add("Status", irfDetails.IRFRecommendations.Status);
                    //recomParameters.Add("CategoryId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CategoryId : null);
                    //recomParameters.Add("CompetencyLevelId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CompetencyLevelId : null);
                    //recomParameters.Add("SeniorityLevelId", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.SeniorityLevelId : null);
                    //recomParameters.Add("CountryType", irfDetails.IRFRecommendations.Status == true ? irfDetails.IRFRecommendations.CountryType : null);
                    //recomParameters.Add("ModifiedOn", DateTime.Now);
                    //SqlMapper.Query(con, "usp_UpdateIRFRecommendation", recomParameters,
                    //commandType: CommandType.StoredProcedure);


                    if (joinabilityDt != null && joinabilityDt.Rows.Count>0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
                    }
                    if (selectabilityDt != null && selectabilityDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
                    }
                    if (skillGeneralDt != null && skillGeneralDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
                    }
                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    }
                    if (emergencyInterviewerDt != null && emergencyInterviewerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@EmergencyInterviewerBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@EmergencyInterviewerBulkData"].Value = emergencyInterviewerDt;
                    }
                    if (clientUserDt != null && clientUserDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    }
                    if (qualityControllerDt != null && qualityControllerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@QCBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@QCBulkData"].Value = qualityControllerDt;
                    }
                    if (hRDt != null && hRDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@HRBulkData"].Value = hRDt;
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    if (removedTechSkills != null)
                    {
                        foreach (var removedTechSkill in removedTechSkills)
                        {
                            try
                            {
                                UpdateTechSkillQuestionsForRemovedSkills(removedTechSkill);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }

                    if (interviewerRateChangesList != null)
                    {
                        foreach (var interviewerChangeDetails in interviewerRateChangesList.Where(x => x.Type == "Interviewer"))
                        {
                            try
                            {
                                UpdateInterviewerAmountBasedOnRateChanges(interviewerChangeDetails);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }

                    if (removedInterviewers != null)
                    {
                        foreach (var removedInterviewer in removedInterviewers.Where(x => x.Type == "Interviewer"))
                        {
                            try
                            {
                                UpdateInterviewerAmountForRemovedInterviewers(removedInterviewer);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }

                    if (addedNewInterviewers != null)
                    {
                        foreach (var addedNewInterviewer in addedNewInterviewers.Where(x => x.Type == "Interviewer"))
                        {
                            try
                            {
                                UpdateAmountForNewlyAddedInterviewers(addedNewInterviewer);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }


                    if (interviewerRateChangesList != null)
                    {
                        foreach (var interviewerChangeDetails in interviewerRateChangesList.Where(x => x.Type == "EmergencyInterviewer"))
                        {
                            try
                            {
                                UpdateInterviewerAmountBasedOnRateChanges(interviewerChangeDetails);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }

                    if (removedInterviewers != null)
                    {
                        foreach (var removedEmergencyInterviewer in removedInterviewers.Where(x => x.Type == "EmergencyInterviewer"))
                        {
                            try
                            {
                                UpdateInterviewerAmountForRemovedInterviewers(removedEmergencyInterviewer);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
                    }
                    if (addedNewInterviewers != null)
                    {
                        foreach (var addedNewInterviewer in addedNewInterviewers.Where(x => x.Type == "EmergencyInterviewer"))
                        {
                            try
                            {
                                UpdateAmountForNewlyAddedInterviewers(addedNewInterviewer);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                continue;
                            }
                        }
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

        private async void UpdateInterviewerAmountBasedOnRateChanges(InterviewerSuccessRateChangeData interviewerChangeDetails)
        {
            DynamicParameters dynamicParam = new DynamicParameters();
            dynamicParam.Add("@userId", interviewerChangeDetails.UserId);
            dynamicParam.Add("@IRFId", interviewerChangeDetails.IRFId);
            dynamicParam.Add("@percentageChange", interviewerChangeDetails.PercentChange);
            dynamicParam.Add("@type", interviewerChangeDetails.Type);
            using (SqlConnection conChange = new SqlConnection(connectionString))
            {
                await SqlMapper.QueryAsync(conChange, "usp_UpdateAmountBasedOnPerSuccessRate", dynamicParam,
                   commandType: CommandType.StoredProcedure);
            }
        }

        private async void UpdateTechSkillQuestionsForRemovedSkills(RemovedTechSkillsFromIRF removedTechSkill)
        {
            DynamicParameters dynamicParam = new DynamicParameters();
            dynamicParam.Add("@IRFId", removedTechSkill.IRFId);
            dynamicParam.Add("@SkillId", removedTechSkill.SkillId);
            using (SqlConnection conChange = new SqlConnection(connectionString))
            {
                await SqlMapper.QueryAsync(conChange, "usp_UpdateTechSkillQuestionsForRemovedTechSkill", dynamicParam,
                   commandType: CommandType.StoredProcedure);
            }
        }

        private async void UpdateInterviewerAmountForRemovedInterviewers(RemovedInterviewersFromSkillsChannelData removedInterviewer)
        {
            DynamicParameters dynamicParam = new DynamicParameters();
            dynamicParam.Add("@userId", removedInterviewer.UserId);
            dynamicParam.Add("@IRFId", removedInterviewer.IRFId);
            dynamicParam.Add("@type", removedInterviewer.Type);
            using (SqlConnection conChange = new SqlConnection(connectionString))
            {
                await SqlMapper.QueryAsync(conChange, "usp_UpdateAmountToZeroForRemovedInterviewers", dynamicParam,
                   commandType: CommandType.StoredProcedure);
            }
        }

        private async void UpdateAmountForNewlyAddedInterviewers(AddedNewInterviewersInSkillsChannelData addedNewInterviewer)
        {
            DynamicParameters dynamicParam = new DynamicParameters();
            dynamicParam.Add("@userId", addedNewInterviewer.UserId);
            dynamicParam.Add("@IRFId", addedNewInterviewer.IRFId);
            dynamicParam.Add("@PerSuccessRate", addedNewInterviewer.PerSuccessRate);
            dynamicParam.Add("@Type", addedNewInterviewer.Type);
            using (SqlConnection conChange = new SqlConnection(connectionString))
            {
                await SqlMapper.QueryAsync(conChange, "usp_UpdateAmountForNewlyAddedInterviewers", dynamicParam,
                   commandType: CommandType.StoredProcedure);
            }
        }
        public IEnumerable<TransSkill> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select tts.*, Skill.[Name] as SkillName from TransTechSkill tts join Skill on tts.SkillId=Skill.id  where IRFId = @IRFId";
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
        public IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id)
        {
            try
            {
                var query = "Select * from TransSkillGeneral where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkillGeneral>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransJoinability> GetTransJoinability(int id)
        {
            try
            {
                var query = "Select * from TransJoinability where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransJoinability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransSelectability> GetTransSelectability(int id)
        {
            try
            {
                var query = "Select * from TransSelectability where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSelectability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransInterviewer> GetTransInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransInterviewer>(con, "usp_GetTransInterviewer", param,
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
        public IEnumerable<TransEmergencyInterviewer> GetTransEmergencyInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransEmergencyInterviewer>(con, "usp_GetTransEmergencyInterviewer", param,
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

        public IEnumerable<TransResumeEvaluator> GetTransResumeEvaluator(int id)
        {
            try
            {
                var query = "select tre.*, re.UserId from TransResumeEvaluator tre inner join ResumeEvaluator re on tre.ResumeEvaluatorId = re.id where IRFId=@irfId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransResumeEvaluator>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public IEnumerable<TransHR> GetTransHR(int id)
        {
            try
            {
                var query = "select thr.*, HR.UserId from TransHR thr inner join HR  on thr.HRId = HR.id where IRFId=@irfId";
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


        public IEnumerable<TransClientUser> GetTransClientUser(int id)
        {
            try
            {
                //"select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id where IRFId = @IRFId";
                var query = "select a.*, ClientRole.[Name] as [Role] from (select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id) as a inner join ClientRole on ClientRole.id =a.ClientRoleId where IRFId =  @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public IEnumerable<TransClientUser> GetTransClientUserRole()
        {
            try
            {
                //"select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id where IRFId = @IRFId";
                var query = "select a.IRFId,a.ClientRoleId, ClientRole.[Name] as [Role] from (select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id) as a inner join ClientRole on ClientRole.id =a.ClientRoleId";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransClientUser> GetTransClientUserRole(int id)
        {
            try
            {
                //"select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id where IRFId = @IRFId";
                var query = "select a.ClientRoleId, a.EmailId from (select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id) as a inner join ClientRole on ClientRole.id =a.ClientRoleId where IRFId =  @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransClientUser>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransQualityController> GetTransQualityController(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransQualityController>(con, "usp_GetTransQualityController", param,
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

        public IEnumerable<TransRecruiter> GetTransRecruiter(int id)
        {
            try
            {
                var query = "select tr.*, re.Domain  from TransRecruiter tr join Recruiter re on tr.UserId = re.UserId where IRFId=@irfId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfId", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransRecruiter>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public bool AddCandidateCuratedScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string sqlDeletePrevious = "Delete from CandidateCuratedScore where IRFId =@IRFId;";
                    var deletedRows = con.Execute(sqlDeletePrevious, new
                    {
                        IRFId = IrfId,

                    });
                    foreach (var item in candidateCuratedScore)
                    {

                        string sqlCuratedCandidate = "INSERT INTO CandidateCuratedScore (IRFId, CandidateEmailId, CuratedEmailId,Score) Values (@IRFId, @CandidateEmailId, @CuratedEmailId,@Score);";
                        var affectedRows = con.Execute(sqlCuratedCandidate, new
                        {
                            IRFId = IrfId,
                            item.CandidateEmailId,
                            item.CuratedEmailId,
                            item.Score

                        });
                    }
                    return true;
                }

            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> AddSkillComments(TechSkillComments obj)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    DataTable techSkillCommentsDt = new DataTable();
                    techSkillCommentsDt.Columns.Add("IRFId", typeof(int));
                    techSkillCommentsDt.Columns.Add("Skill", typeof(int));
                    techSkillCommentsDt.Columns.Add("Comment", typeof(string));
                    techSkillCommentsDt.Columns.Add("UserId", typeof(int));

                    foreach (var item in obj.SkillCollection)
                    {
                        techSkillCommentsDt.Rows.Add(obj.IRFId, Convert.ToInt16(item.Skill), item.Comment, item.UserId);
                    }

                    SqlCommand cmdAddSkillComments = new SqlCommand("usp_AddTechSkillComments", con);
                    cmdAddSkillComments.CommandType = CommandType.StoredProcedure;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@TechSkillCommentsBulkData", SqlDbType.Structured));
                    cmdAddSkillComments.Parameters["@TechSkillCommentsBulkData"].Value = techSkillCommentsDt;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int));
                    cmdAddSkillComments.Parameters["@IRFId"].Value = obj.IRFId;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int));
                    cmdAddSkillComments.Parameters["@userId"].Value = obj.UserId;
                    con.Open();

                    if (await cmdAddSkillComments.ExecuteNonQueryAsync() != 0)
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

        public IEnumerable<SkillComment> GetTechSkillComments(int id, string emailId = null)
        {
            try
            {


                //var query = "Select tsc.*, sk.[Name] as SkillName from TechSkillComments tsc join Skill sk on sk.id = tsc.Skill where IRFId = @IRFId";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@IRFId", id);
                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    var results = con.Query<SkillComment>(query, dynamicParameters);
                //    return results;
                //}

                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.QueryAsync<SkillComment>(con, "usp_GetTechSkillComments", dynamicParameters,
                        commandType: CommandType.StoredProcedure).Result;
                    return result;

                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<CandidateCuratedScore> GetCandidateCuratedScore(int id)
        {
            try
            {
                var query = "Select * from CandidateCuratedScore where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<CandidateCuratedScore>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> ArchiveIRF(int IRFId)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", IRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteIRF", parameters,
                        commandType: CommandType.StoredProcedure);
                    if (result != null && result.FirstOrDefault() == 0)
                    {
                        return true;
                    }
                    return false;

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

        public async Task<bool> UnArchiveIRF(int IRFId)
        {
            try
            {
                var query = "update IRF set IsDeleted=0 where id = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await con.QueryAsync<int>(query, dynamicParameters);
                    if (result != null && result.FirstOrDefault() == 0)
                    {
                        return true;
                    }
                    return false;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }


        public async Task<IEnumerable<MeetingVM>> GetIRFInterviewMeetings(int id, string emailId = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetIRFInterviewMeetings", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<MeetingVM> GetIRFInterviewMeetingsCountData(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<MeetingVM>(con, "usp_GetIRFInterviewMeetingsCountData", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
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

        /// <summary>
        /// Based on IRFID get list of interviwers
        /// </summary>
        /// <param name="irfId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetListOfInterviwerBasedOnIRFId(string irfId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                var parameters = new DynamicParameters();
                parameters.Add("@IRFID", irfId); // Normal parametersmeters 
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_ListOfIRFInterviwer", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<IEnumerable<string>> GetEmergencyInterviwerBasedOnIRFId(int irfId)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                var parameters = new DynamicParameters();
                parameters.Add("@IRFId", irfId); // Normal parametersmeters 
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_ListEmergencyInterviewers", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }


        public IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<JoinabilityMatrix>(
                        con, "select * from JoinabilityMatrix", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<SelectabilityMatrix>(
                        con, "select * from SelectabilityMatrix", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<GeneralSkillMatrix>(
                        con, "select * from GeneralSkillMatrix", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<Users> GetClientUser(int id)
        {
            IEnumerable<ClientUserDTO> clientUsers = new List<ClientUserDTO>();
            IEnumerable<Users> clients = new List<Users>();
            try
            {
                var query = "select * from ClientUser where ClientId = @ClientId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ClientId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    clientUsers = con.Query<ClientUserDTO>(query, dynamicParameters);
                    if (clientUsers == null || clientUsers.Count() == 0)
                        return clients;

                    //clientUsers = SqlMapper.Query<ClientUserDTO>(
                    //con, "select * from ClientUser", null, commandType: Text);
                    var idList = new List<int>();
                    foreach (var item in clientUsers)
                    {
                        idList.Add(item.UserId);
                    }
                    string ids = string.Join<int>(",", idList);
                    clients = SqlMapper.Query<Users>(
                        con, "select * from UserList where id in (" + ids + ")", null, commandType: Text);
                    return clients;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public string GetIRFNameBasedOnId(int irfID)
        {
            try
            {
                var query = "Select IRFId from IRF where id = @irfID";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfID", irfID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string GetIRFJDTitleBasedOnId(int irfID)
        {
            try
            {
                var query = "Select JDTiltle from IRF where id = @irfID";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfID", irfID);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<Candidate>> GetIRFCandidates(int irfId, string emailId = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetIRFCandidates", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public IEnumerable<string> GetIRFCandidatesEmail(string irfName)
        {
            try
            {
                var query = "select candidateEmail from InterviewMeetings where IRFId =@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        //GetCandidateIRFInvites
        public async Task<IEnumerable<Candidate>> GetCandidateIRFInvites(string emailId)
        {
            try
            {

                var query = "select Candidate.*,ul.EmailId, IRF.EndDate, IRF.StartDate, IRF.ClientName,IRF.JDTiltle, IRF.JDLink, IRF.IRFId as IRFName,IRF.CalendlyLink, Client.[Name] as ClientCompName from Candidate join IRF on Candidate.IRFId=IRF.id join Client on IRF.ClientId = Client.id join CandidatesMaster cm on cm.id = Candidate.CandidateMasterId join UserList ul on ul.id=cm.UserId where ul.EmailId = @EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<Candidate>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public async Task<InterviewPlanner> GetInterviewPlanner(int id)
        {
            try
            {
                //var query = "Select InterviewPlanner.*,IRF.CreatedOn as [IRFCreatedOn] from InterviewPlanner inner join [IRF] on InterviewPlanner.IRFId = [IRF].id  where InterviewPlanner.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<InterviewPlanner>(con, "usp_GetInterviewPlanner", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", model.IRFId);
                dynamicParameters.Add("@InviteOwnedBy", model.InviteOwnedBy);
                dynamicParameters.Add("@ConfirmationOwnedBy", model.ConfirmationOwnedBy);
                dynamicParameters.Add("@CompletionOwnedBy", model.CompletionOwnedBy);
                dynamicParameters.Add("@RisebirdSelectionOwnedBy", model.RisebirdSelectionOwnedBy);
                dynamicParameters.Add("@ApprovalOwnedBy", model.ApprovalOwnedBy);
                dynamicParameters.Add("@FinalSelectionOwnedBy", model.FinalSelectionOwnedBy);
                dynamicParameters.Add("@OfferOwnedBy", model.OfferOwnedBy);
                dynamicParameters.Add("@JoiningOwnedBy", model.JoiningOwnedBy);
                dynamicParameters.Add("@OverAllProcessingTime", model.OverAllProcessingTime);
                dynamicParameters.Add("@InviteProcessingTime", model.InviteProcessingTime);
                dynamicParameters.Add("@ConfirmationProcessingTime", model.ConfirmationProcessingTime);
                dynamicParameters.Add("@CompletionProcessingTime", model.CompletionProcessingTime);
                dynamicParameters.Add("@RisebirdSelectionProcessingTime", model.RisebirdSelectionProcessingTime);
                dynamicParameters.Add("@ApprovalProcessingTime", model.ApprovalProcessingTime);
                dynamicParameters.Add("@FinalSelectionProcessingTime", model.FinalSelectionProcessingTime);
                dynamicParameters.Add("@OfferProcessingTime", model.OfferProcessingTime);
                dynamicParameters.Add("@JoiningProcessingTime", model.JoiningProcessingTime);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = SqlMapper.Query<int>(con, "usp_AddUpdateInterviewPlanner", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<string> AddCandidate(Candidate obj)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", obj.IRFId);
                parameters.Add("@Name", obj.Name?.Trim());
                parameters.Add("@EmailId", obj.EmailId?.Trim().ToLower());
                parameters.Add("@ResumeLink", obj.ResumeLink);
                parameters.Add("@Type", obj.Type);
                parameters.Add("@Location", obj.Location);
                parameters.Add("@Skill", obj.Skill);
                parameters.Add("@WorkAuth", obj.WorkAuth);
                parameters.Add("@CurrentLocation", obj.CurrentLocation);
                parameters.Add("@CurrentEmployer", obj.CurrentEmployer);
                parameters.Add("@Phone", obj.Phone);
                parameters.Add("@TotalExperience", obj.TotalExperience);
                parameters.Add("@CurrentSalary", obj.CurrentSalary);
                parameters.Add("@ExpectedSalary", obj.ExpectedSalary);
                parameters.Add("@Notice", obj.Notice);
                parameters.Add("@AddedByUserId", obj.AddedByUserId);
                parameters.Add("@CostEstimate", obj.CostEstimate);
                parameters.Add("@EncodedResumeLink", obj.EncodedResumeLink);
                parameters.Add("@Heuristicsscore", obj.Heuristicsscore);
                parameters.Add("@AvailableFullTime", obj.AvailableFullTime);
                parameters.Add("@AvailableFreeLancing", obj.AvailableFreeLancing);
                parameters.Add("@ClientHRAttemptContact", obj.ClientHRAttemptContact);
                parameters.Add("@ProfileSource", obj.ProfileSource);
                parameters.Add("@WhatsappNumber", obj.WhatsappNumber);
                parameters.Add("@IsProctor", obj.IsProctor);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "usp_AddCandidate", parameters, commandType: CommandType.StoredProcedure);
                    return "Candidate added successfully.";
                }

                //using (SqlConnection con = new SqlConnection(connectionString))
                //{


                //    string sqlInsertReport = "INSERT INTO Candidate (IRFId,Name, EmailId,ResumeLink,Type, Location, Skill, WorkAuth, CurrentLocation, CurrentEmployer, Phone, TotalExperience, CurrentSalary, ExpectedSalary, Notice) Values (@IRFId,@Name,@EmailId,@ResumeLink ,@Type, @Location, @Skill, @WorkAuth, @CurrentLocation, @CurrentEmployer, @Phone, @TotalExperience, @CurrentSalary, @ExpectedSalary, @Notice);";
                //    var affectedRows = con.Execute(sqlInsertReport, new
                //    {
                //        IRFId = obj.IRFId,
                //        Name = obj.Name,
                //        EmailId = obj.EmailId,
                //        ResumeLink = obj.ResumeLink,
                //        Type = obj.Type,
                //        Location = obj.Location,
                //        Skill = obj.Skill,
                //        WorkAuth = obj.WorkAuth,
                //        CurrentLocation = obj.CurrentLocation,
                //        CurrentEmployer = obj.CurrentEmployer,
                //        Phone = obj.Phone,
                //        TotalExperience = obj.TotalExperience,
                //        CurrentSalary = obj.CurrentSalary,
                //        ExpectedSalary = obj.ExpectedSalary,
                //        Notice = obj.Notice

                //    });

                //    return "Candidate added successfully.";
                //}

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not add candidate.";
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not add candidate.";
            }
        }

        public async Task<string> AddMultipleCandidates(List<Candidate> candidateList, int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    if (candidateList != null && candidateList.Count() > 0)
                    {
                        foreach (var obj in candidateList)
                        {
                            DynamicParameters parameters = new DynamicParameters();
                            parameters.Add("@IRFId", obj.IRFId);
                            parameters.Add("@Name", obj.Name);
                            parameters.Add("@EmailId", obj.EmailId);
                            parameters.Add("@ResumeLink", obj.ResumeLink);
                            parameters.Add("@Type", obj.Type);
                            parameters.Add("@Location", obj.Location);
                            parameters.Add("@Skill", obj.Skill);
                            parameters.Add("@WorkAuth", obj.WorkAuth);
                            parameters.Add("@CurrentLocation", obj.CurrentLocation);
                            parameters.Add("@CurrentEmployer", obj.CurrentEmployer);
                            parameters.Add("@Phone", obj.Phone);
                            parameters.Add("@TotalExperience", obj.TotalExperience);
                            parameters.Add("@CurrentSalary", obj.CurrentSalary);
                            parameters.Add("@ExpectedSalary", obj.ExpectedSalary);
                            parameters.Add("@Notice", obj.Notice);
                            parameters.Add("@AddedByUserId", userId);
                            parameters.Add("@EncodedResumeLink", obj.EncodedResumeLink);
                            parameters.Add("@Heuristicsscore", obj.Heuristicsscore);
                            parameters.Add("@AvailableFullTime", obj.AvailableFullTime);
                            parameters.Add("@AvailableFreeLancing", obj.AvailableFreeLancing);
                            parameters.Add("@ClientHRAttemptContact", obj.ClientHRAttemptContact);
                            parameters.Add("@ProfileSource", obj.ProfileSource);
                            parameters.Add("@WhatsappNumber", obj.WhatsappNumber);

                            SqlMapper.Query<string>(con, "usp_AddCandidate", parameters, commandType: CommandType.StoredProcedure);

                        }
                    }


                    return "Candidates imported successfully.";
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not import candidates.";
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not import candidates.";
            }
        }

        public async Task<bool> DeleteCandidate(int id)
        {
            try
            {

                var query = "Delete from Candidate where id = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<int>(query, dynamicParameters);
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

        public bool UpdateCandidate(Candidate obj)
        {
            try
            {
                //var query = "update Candidate set [Name] = @Name, EmailId=@EmailId, ResumeLink = @ResumeLink, Type=@Type, Location=@Location, Skill=@Skill, WorkAuth=@WorkAuth, CurrentLocation=@CurrentLocation, CurrentEmployer=@CurrentEmployer, Phone=@Phone, TotalExperience=@TotalExperience, CurrentSalary=@CurrentSalary, ExpectedSalary=@ExpectedSalary, Notice=@Notice  where id=@id";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Name", obj.Name);
                dynamicParameters.Add("@EmailId", obj.EmailId?.Trim().ToLower());
                dynamicParameters.Add("@ResumeLink", obj.ResumeLink);
                dynamicParameters.Add("@Type", obj.Type);
                dynamicParameters.Add("@Location", obj.Location);
                dynamicParameters.Add("@Skill", obj.Skill);
                dynamicParameters.Add("@WorkAuth", obj.WorkAuth);
                dynamicParameters.Add("@CurrentLocation", obj.CurrentLocation);
                dynamicParameters.Add("@CurrentEmployer", obj.CurrentEmployer);
                dynamicParameters.Add("@Phone", obj.Phone);
                dynamicParameters.Add("@TotalExperience", obj.TotalExperience);
                dynamicParameters.Add("@CurrentSalary", obj.CurrentSalary);
                dynamicParameters.Add("@ExpectedSalary", obj.ExpectedSalary);
                dynamicParameters.Add("@Notice", obj.Notice);
                dynamicParameters.Add("@id", obj.id);
                dynamicParameters.Add("@CostEstimate", obj.CostEstimate);
                dynamicParameters.Add("@EncodedResumeLink", obj.EncodedResumeLink);
                dynamicParameters.Add("@Heuristicsscore", obj.Heuristicsscore);
                dynamicParameters.Add("@AvailableFullTime", obj.AvailableFullTime);
                dynamicParameters.Add("@AvailableFreeLancing", obj.AvailableFreeLancing);
                dynamicParameters.Add("@ClientHRAttemptContact", obj.ClientHRAttemptContact);
                dynamicParameters.Add("@ProfileSource", obj.ProfileSource);
                dynamicParameters.Add("@WhatsappNumber", obj.WhatsappNumber);
                dynamicParameters.Add("@IsProctor", obj.IsProctor);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<int>(
                        con, "usp_UpdateCandidate", dynamicParameters, commandType: CommandType.StoredProcedure);
                    return true;
                }


            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }


        public bool UpdateEmergencyFlag(int id, string emailId, DateTime interviewDate)
        {
            try
            {
                //var query = "update InterviewMeetings set [IsEmergency] = 1 where id=@id"; 
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", id);

                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    var results = con.Query<int>(query, dynamicParameters);
                //    return true;
                //}

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@EmailId", emailId);
                parameters.Add("@interviewDate", interviewDate);
                parameters.Add("@timeNow", DateTime.Now);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "usp_UpdateEmergencyFlag", parameters, commandType: CommandType.StoredProcedure);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> UnassignYourself(int meetingid)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", meetingid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query<string>(
                        con, "usp_UnAssignInterviewer", parameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public int InterviewMeetings(string irfName, string candidateEmail)
        {
            int result = 0;
            try
            {
                var query = "select id from InterviewMeetings where IRFId=@IRFId and candidateEmail=@candidateEmail and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfName);
                dynamicParameters.Add("@candidateEmail", candidateEmail);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return result;
            }
        }
        public IEnumerable<InterviewComments> GetInterviewComments(string idList)
        {
            try
            {
                var query = "select InterviewId, InterviewStatus, MeetingStatus, OfferStatus, JoiningStatus, IsSelected, IsApproved from InterviewComments  where InterviewId in (" + idList + ")";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<InterviewComments>(query);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public int GetUserId(string emailId, string profile = null)
        {
            int result = 0;
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profile);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    result = SqlMapper.QuerySingle<int>(con, "usp_GetUserId", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }



        public IEnumerable<Recruiter> GetRecruiters()
        {
            try
            {
                var query = "select Recruiter.UserId, ul.EmailId from Recruiter join UserList ul on Recruiter.UserId=ul.id";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Recruiter>(query);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public string GetRecruiterDomain(int userId)
        {
            try
            {
                var query = "select re.Domain from Recruiter re join UserList ul on re.UserId= ul.id where re.UserId=@UserId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@UserId", userId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return result.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<IRFVm> GetUpComingIRFList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<IRFVm>(con, "usp_ComingDaysIRF", null,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<InterviewVM> GetUpComingInterviewList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<InterviewVM>(con, "usp_ComingDaysInterview", null,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<IEnumerable<MeetingVM>> UpdateMeetingStatus(string IRFId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", IRFId);
                    dynamicParameters.Add("@date", DateTime.Now);
                    return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_UpdateMeetingStatus", dynamicParameters,
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

        public bool UpdateIRFPauseStatus(int id)
        {
            try
            {
                var query = "update IRF set HREmailId=null where id=@id";//select * from TransClientUser where IRFId = (select id from IRF where IRFId=@IRFId)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);


                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        //public bool AddPausedMeetings(int irfId, int meetingId)
        //{
        //    try
        //    {
        //        var query = "Insert into PausedIRFMeetings values (@IRFId, @MeetingId)";
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@IRFId", irfId);
        //        dynamicParameters.Add("@MeetingId", meetingId);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var result = con.Query(query, dynamicParameters);
        //        }
        //        return true;
        //    }
        //    catch (SqlException ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return false;
        //}

        //public async Task<IEnumerable<MeetingVM>> PlayIRF(string IRFId)
        //{
        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var dynamicParameters = new DynamicParameters();
        //            dynamicParameters.Add("@IRFId", IRFId);
        //            dynamicParameters.Add("@date", DateTime.Now);
        //            return await SqlMapper.QueryAsync<MeetingVM>(con, "usp_UpdateIRFPauseStatus", dynamicParameters,
        //                 commandType: CommandType.StoredProcedure);
        //        }

        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return null;

        //}
        public bool PlayIRF(string IRFId)
        {
            try
            {
                var query = "update IRF set IsPaused = 0 where IRFId=@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);
                //dynamicParameters.Add("@MeetingId", meetingId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query(query, dynamicParameters);
                }
                return true;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;

        }
        //public bool UpdatePausedMeetingstoPlay(string meetingIds)
        //{
        //    try
        //    {
        //        var query = "Update InterviewMeetings set confirmationStatus=1 where id in (" + meetingIds + ")";
        //        //var dynamicParameters = new DynamicParameters();
        //        //dynamicParameters.Add("@IRFId", irfId);
        //        //dynamicParameters.Add("@MeetingId", meetingId);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var result = con.Query(query);
        //        }
        //        return true;
        //    }
        //    catch (SqlException ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return false;
        //}
        //public bool DeletePlayedMeetings(string meetingIds)
        //{
        //    try
        //    {
        //        var query = "Delete from PausedIRFMeetings where MeetingId in (" + meetingIds + ")";
        //        //var dynamicParameters = new DynamicParameters();
        //        //dynamicParameters.Add("@IRFId", irfId);
        //        //dynamicParameters.Add("@MeetingId", meetingId);
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var result = con.Query(query);
        //        }
        //        return true;
        //    }
        //    catch (SqlException ex)
        //    {
        //        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return false;
        //}

        public async Task<bool> SubscribeCuratedGroup(int id, string emailId, string profile)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@CuratedGroupId", id);
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profile);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_SubscribeCuratedGroup", parameters,
                        commandType: CommandType.StoredProcedure);
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

        public async Task<bool> UnSubscribeCuratedGroup(int id, string emailId, string profile)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@CuratedGroupId", id);
                parameters.Add("@EmailId", emailId);
                parameters.Add("@Profile", profile);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "[usp_UnSubscribeCuratedGroup]", parameters,
                        commandType: CommandType.StoredProcedure);
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
        public IEnumerable<string> GetCuratedUserMailId(int irfId)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@irfId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<string>(con, "usp_GetCuratedUserMailId", param,
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
        public bool SaveCandidateStatus(int id, int status)
        {
            try
            {
                var query = "update Candidate set [Status]=@Status where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Status", status);
                dynamicParameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query(query, dynamicParameters);
                    return true;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public IEnumerable<Users> GetAllHRs()
        {
            try
            {
                var query = "select id, EmailId from UserList where idProfile=2";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return con.Query<Users>(query);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        /// <summary>
        /// Gets Users having one of the profiles among Admin, HR and Finance
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Users> GetEligibleChannelOwners()
        {
            try
            {
                var query = "select id, EmailId from UserList where idProfile in (1,2,28,31)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return con.Query<Users>(query);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<CuratedGroup>> GetAllCuratedGroups()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CuratedGroup>(con, "usp_GetAllCuratedGroups",
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
        public IEnumerable<CuratedIRFMapping> GetIRFListForCuratedMapping()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<CuratedIRFMapping>(con, "usp_GetIRFListForCuratedMapping",
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> AddCuratedGroup(int[] irfList, string cgName)
        {
            try
            {
                if (irfList != null && irfList.Count() > 0)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        DynamicParameters parameters = new DynamicParameters();
                        parameters.Add("@Name", cgName);
                        parameters.Add("@CreatedOn", DateTime.Now);
                        var obj = SqlMapper.Query<int>(con, "usp_AddCuratedGroup", parameters,
                             commandType: CommandType.StoredProcedure);

                        var curatedGroupId = obj.FirstOrDefault();

                        DataTable curatedIRFDt = new DataTable();
                        curatedIRFDt.Columns.Add("CuratedGroupId", typeof(int));
                        curatedIRFDt.Columns.Add("IRFId", typeof(int));
                        curatedIRFDt.Columns.Add("IsAdded", typeof(bool));

                        foreach (var item in irfList)
                        {
                            curatedIRFDt.Rows.Add(curatedGroupId, item, true);
                        }

                        SqlCommand cmdCuratedIRF = new SqlCommand("usp_CuratedIRFBulkInsert", con);
                        cmdCuratedIRF.CommandType = CommandType.StoredProcedure;
                        cmdCuratedIRF.Parameters.Add(new SqlParameter("@CuratedIRFBulkData", SqlDbType.Structured));
                        cmdCuratedIRF.Parameters["@CuratedIRFBulkData"].Value = curatedIRFDt;
                        con.Open();
                        if (cmdCuratedIRF.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }

                    }

                }
                return true;

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
        public async Task<IEnumerable<CuratedIRFMapping>> GetCuratedGroupMappingsById(int curatedGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CuratedGroupId", curatedGroupId);
                    return await SqlMapper.QueryAsync<CuratedIRFMapping>(con, "usp_GetCuratedIRFMappingById", parameters,
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

        public async Task<bool> UpdateCuratedGroup(int[] irfList, string cgName, int cgId)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", cgId);
                parameters.Add("@Name", cgName);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateCuratedGroup", parameters,
                       commandType: CommandType.StoredProcedure);



                    if (irfList != null && irfList.Count() > 0)
                    {
                        DataTable curatedIRFDt = new DataTable();
                        curatedIRFDt.Columns.Add("CuratedGroupId", typeof(int));
                        curatedIRFDt.Columns.Add("IRFId", typeof(int));
                        curatedIRFDt.Columns.Add("IsAdded", typeof(bool));

                        foreach (var item in irfList)
                        {
                            curatedIRFDt.Rows.Add(cgId, item, true);
                        }

                        SqlCommand cmdCuratedIRF = new SqlCommand("usp_CuratedIRFBulkInsert", con);
                        cmdCuratedIRF.CommandType = CommandType.StoredProcedure;
                        cmdCuratedIRF.Parameters.Add(new SqlParameter("@CuratedIRFBulkData", SqlDbType.Structured));
                        cmdCuratedIRF.Parameters["@CuratedIRFBulkData"].Value = curatedIRFDt;
                        con.Open();
                        if (cmdCuratedIRF.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
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
        public async Task<IEnumerable<CuratedGroupUsers>> GetCuratedGroupUsers(int curatedGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CuratedGroupId", curatedGroupId);
                    return await SqlMapper.QueryAsync<CuratedGroupUsers>(con, "usp_GetCuratedGroupUsers", parameters,
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


        public async Task<bool> AddUpdateCuratedGroupUsers(int[] users, int curatedGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "delete from CuratedGroupUsers where CuratedGroupId=@CuratedGroupId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@CuratedGroupId", curatedGroupId);
                    con.Query(query, dynamicParameters);

                    if (users != null && users.Count() != 0)
                    {
                        DataTable curatedUsersDt = new DataTable();
                        curatedUsersDt.Columns.Add("UserId", typeof(int));
                        curatedUsersDt.Columns.Add("CuratedGroupId", typeof(int));
                        curatedUsersDt.Columns.Add("IsAdded", typeof(bool));

                        foreach (var item in users)
                        {
                            curatedUsersDt.Rows.Add(item, curatedGroupId, true);
                        }

                        SqlCommand cmdCuratedUsers = new SqlCommand("usp_CuratedGroupUsersBulkInsert", con);
                        cmdCuratedUsers.CommandType = CommandType.StoredProcedure;
                        cmdCuratedUsers.Parameters.Add(new SqlParameter("@CuratedGroupUsersBulkData", SqlDbType.Structured));
                        cmdCuratedUsers.Parameters["@CuratedGroupUsersBulkData"].Value = curatedUsersDt;
                        con.Open();
                        if (cmdCuratedUsers.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                        }
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

        public async Task<IEnumerable<CuratedUserGroupData>> GetCuratedGroupsForCuratedUser(string emailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailId);
                    //parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CuratedUserGroupData>(con, "usp_GetCuratedGroupsForCuratedUser", parameters,
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

        public IEnumerable<FetchInterviewerDetails> GetSkillQuality()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransSkillQuality", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetCertificationQuality()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransCertificationQuality", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetEducationQuality()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransEducationQuality", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetEmployersQuality()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransEmployersQuality", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<FetchInterviewerDetails> GetCareerProgression()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransCareerProgression", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetProjectsQuality()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransProjectsQuality", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetOnlinePresence()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransOnlinePresence", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetBehavioralScore()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransBehavioralScore", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetInterviwerCommunication()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviwerCommunication", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetInterviewexperience()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewexperience", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetCompanyStage()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewerCompanyStage", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetSkillCategory()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewerSkillCategory", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetInterviewerExprienceYear()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewerExprienceYears", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetSenioritylevel()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewerSeniorityLevel", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public IEnumerable<FetchInterviewerDetails> GetSkillNature()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from TransInterviewerSkillNature", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string pickInterviewerSills(string Interviewerids)
        {
            try
            {
                var interviewer = Interviewerids.Split(',');
                var resultset = String.Empty;
                for (int i = 0; i < interviewer.Count(); i++)
                {
                    var query = "select PrimarySkill from Interviewer where Userid =@Interviewerids";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@Interviewerids", interviewer[i].Trim());

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<string>(query, dynamicParameters);
                        var result = results.FirstOrDefault().Split(',');
                        var resultsets = resultset.Split(',');
                        var flag = true;
                        foreach (var item in resultsets)
                        {
                            if (result[0].ToUpper() == item.ToUpper())
                            {
                                flag = false;
                            }
                        }
                        if (flag == true)
                        {
                            if (String.IsNullOrEmpty(resultset))
                            {
                                resultset = result[0];
                            }
                            else
                            {
                                resultset = resultset + "," + result[0];
                            }
                        }
                    }
                }
                var Skills = resultset.Split(',');
                var Skillset = String.Empty;
                for (int i = 0; i < Skills.Count(); i++)
                {
                    var query = "Select Id from skill where Name=@Skill";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@Skill", Skills[i].Trim());

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<string>(query, dynamicParameters);
                        var result = results.FirstOrDefault();
                        if (String.IsNullOrEmpty(Skillset))
                        {
                            Skillset = result;
                        }
                        else
                        {
                            Skillset = Skillset + "," + result;
                        }
                    }

                }

                return Skillset;

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "";
            }

        }

        public async Task<IEnumerable<IRFInterviewerMembers>> GetIRFInterviewerMembers(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    //parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_GetIRFInterviewerMembers", parameters,
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
        public IEnumerable<InterviewerVM> GetSkillDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "select sk.id as id,LTRIM(RTRIM(sk.Name)) as PrimarySkill from Skill sk order by LTRIM(RTRIM(sk.Name))";
                    var parameters = new DynamicParameters();
                    var results = con.Query<InterviewerVM>(query, parameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<string> Addassignmentgroup(Skilldetails _skilldetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@AssessmentGroup", _skilldetails.AssessmentGroupName);
                    parameters.Add("@PrimarySkills", _skilldetails.PrimarySkill);
                    parameters.Add("@id", _skilldetails.id);
                    parameters.Add("@Isinstaavailable", _skilldetails.Isinstaavailable);
                    parameters.Add("@Category", _skilldetails.Category);
                    parameters.Add("@CompanyType", _skilldetails.CompanyType);
                    parameters.Add("@IsRelevant", _skilldetails.IsRelevant);
                    parameters.Add("@ProfileType", _skilldetails.ProfileType);
                    parameters.Add("@CompetencyLevel", _skilldetails.CompetencyLevel);
                    parameters.Add("@Country", _skilldetails.Country);
                    parameters.Add("@PerHourRate", _skilldetails.PerHourRate);
                    await SqlMapper.QueryAsync(con, "usp_AddAssessmentGroup", parameters,
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
            return "Failed";
        }
        public IEnumerable<AssignmentVM> FetchAssessmentdetails(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "Select ia.*,ag.AssessmentGroup from InterviewerAssessments ia join assessmentgroup ag on ia.assessmentgroupid=ag.id where ia.id=@id";
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", id);
                    var results = con.Query<AssignmentVM>(query, parameters);
                    var finalquery = "Select id,Assessmentid,QuestionCovered,Skill as Skills,QuestionLevel as Level,Time,VideoLink from AssessmentQuestions  where Assessmentid=@id";
                    var finalparameters = new DynamicParameters();
                    finalparameters.Add("@id", id);
                    var Questions = con.Query<AssessmentQuestions>(finalquery, finalparameters);
                    results.FirstOrDefault().Questionslist = Questions.ToList();
                    return results;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public string AssessmentLike(int id, string emailid)
        {
            try
            {
                int userid = 0;
                var query = "Select id from UserList where Emailid = @Emailid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    userid = con.Query<int>(query, dynamicParameters).FirstOrDefault();
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var queryfinal = "INSERT INTO AssessmentLike (Assessmentid,LikedBy) Values (@Assessmentid,@LikedBy);";
                    var affectedRows = con.Execute(queryfinal, new
                    {
                        Assessmentid = id,
                        LikedBy = userid
                    });
                    return "Success";
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }

        public async Task<IEnumerable<string>> GetPrimarySkillFromIRF()
        {
            try
            {
                var query = "select (CASE WHEN CHARINDEX('|', JDTiltle) > 0 THEN SUBSTRING(JDTiltle, 1, CHARINDEX('|', JDTiltle)-1) ELSE JDTiltle END) from IRF where IsDeleted=0";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.QueryAsync<string>(query);
                    return await results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> AddCandidateStatus(CandidateStatus model)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", model.IRFId); // Normal parametersmeters           
                parameters.Add("@CandidateId", model.CandidateId);
                parameters.Add("@Comments", model.Comments);
                parameters.Add("@Date", DateTime.Now);
                parameters.Add("@AudioFileLink", model.AudioFileLink);
                parameters.Add("@Status", model.Status);
                parameters.Add("@PerformedBy", model.PerformedBy);
                parameters.Add("@InterviewId", model.InterviewId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_AddCandidateStatus", parameters,
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


        public async Task<bool> AddQCCandidateStatus(int interviewId, string performedBy)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@InterviewId", interviewId);
                parameters.Add("@Date", DateTime.Now);
                parameters.Add("@PerformedBy", performedBy);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_AddQCCandidateStatus", parameters,
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

        public async Task<IEnumerable<CandidateStatus>> GetCandidatesStatusList(int irfId, int candidateId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@CandidateId", candidateId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<CandidateStatus>(con, "usp_GetCandidateStatusList", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidateStatus> GetCandidateStatusById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<CandidateStatus>(con, "usp_GetCandidateStatusById", dynamicParameters,
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

        public async Task<bool> EditCandidateStatus(CandidateStatus model)
        {
            try
            {
                //Set the parameters to be passed to store procedure
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", model.id); // Normal parametersmeters           
                parameters.Add("@IRFId", model.IRFId); // Normal parametersmeters           
                parameters.Add("@CandidateId", model.CandidateId);
                parameters.Add("@Comments", model.Comments);
                parameters.Add("@Date", DateTime.Now);
                parameters.Add("@AudioFileLink", model.AudioFileLink);
                parameters.Add("@Status", model.Status);
                parameters.Add("@PerformedBy", model.PerformedBy);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateCandidateStatus", parameters,
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

        public async Task<bool> DeleteCandidateStatus(int id)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteCandidateStatus", parameters,
                        commandType: CommandType.StoredProcedure);
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

        public async Task<int> GetCandidateIdByEmail(string emailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_GetCandidateIdByEmail", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<IEnumerable<string>> GetIRFClientsEmail(int irfId)
        {
            try
            {
                var query = "select UserList.EmailId from  TransClientUser join UserList on TransClientUser.ClientUserId = UserList.Id where TransClientUser.IRFId=@IRFId and ClientRoleId in (1,2)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = con.Query<string>(query, dynamicParameters);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<InterviewerProfileVM>> GetInterviewersByTag(string tag)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Tag", tag);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewerProfileVM>(con, "usp_GetInterviewersByTag", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(int id, string emailId = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@irfid", id);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CandidateTimelineData>(con, "usp_GetCandidateTimelineByIRFId", parameters,
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

        public async Task<Candidate> GetCandidateByIRF(int irfId, string emailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                dynamicParameters.Add("@EmailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetCandidateByIRF", dynamicParameters,
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
        public async Task<DataTable> AddQuickMultipleCandidates(List<CandidateQuickAddModel> candidateList, int userId)
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[2] { new DataColumn("EmailId", typeof(string)),
                    new DataColumn("Import Status", typeof(string)) });
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (candidateList != null && candidateList.Count() > 0)
                    {
                        foreach (var obj in candidateList)
                        {
                            DynamicParameters parameters = new DynamicParameters();
                            parameters.Add("@IRFId", obj.IRFId);
                            parameters.Add("@Name", obj.Name);
                            parameters.Add("@EmailId", obj.EmailId);
                            parameters.Add("@ResumeLink", obj.ResumeLink);
                            parameters.Add("@Phone", obj.Phone);
                            parameters.Add("@AddedByUserId", userId);
                            parameters.Add("@date", DateTime.Now);
                            parameters.Add("@ClientHREmail", obj.ClientHREmail);
                            parameters.Add("@ProfileSource", obj.ProfileSource);
                            parameters.Add("@WhatsappNumber", obj.WhatsappNumber);
                            parameters.Add("@operationStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 5215585);


                            await SqlMapper.QueryAsync(con, "usp_AddQuickCandidate", parameters, commandType: CommandType.StoredProcedure);

                            var result = parameters.Get<string>("@operationStatus");
                            dt.Rows.Add(obj.EmailId, result);

                        }
                        return dt;

                    }

                    return null;

                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRF> GetIRFForCall(int irfId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<IRF>(con, "usp_GetIRFById", dynamicParameters,
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
        public IEnumerable<FetchInterviewerDetails> GetDomainList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<FetchInterviewerDetails>(
                        con, "select * from Domains", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<string> GetInterviewerSkillsByUserId(int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("userId", userId);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetInterviewerSkillsByUserId", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
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

        public bool CheckChannelOwnerByIrfid(string IRFId, string emailid)
        {
            try
            {
                var query = "Select 1 from IRF join UserList ul on ul.id=IRF.RisebirdHr where ul.EmailId=@Emailid and IRF.id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", IRFId);
                dynamicParameters.Add("@Emailid", emailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    if (results.FirstOrDefault() == 1)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<bool> DumpCalendlyResourceURI(DataTable calendlyDt)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    SqlCommand cmd = new SqlCommand("usp_DumpCalendlyResourceURI", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@SkillChannelCalendlyDetailsBulkData", SqlDbType.Structured));
                    cmd.Parameters["@SkillChannelCalendlyDetailsBulkData"].Value = calendlyDt;

                    con.Open();
                    if (await cmd.ExecuteNonQueryAsync() != 0)
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
        public async Task<IEnumerable<IRF>> GetPendingCalendlyResourceUriUpdateIRF()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetPendingCalendlyResourceUriUpdateIRF",
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

        public async Task<string> GetIRFCalendlyResourceUri(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetIRFCalendlyResourceUri", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
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

        public async Task<IEnumerable<IRF>> GetAllIRFDetails(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFCandidateStats", parameters, //usp_GetAllIRFDetails
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
        public async Task<IEnumerable<SkillCahnnelDetailsVM>> GetAllSkillChannelDetails()
        {
            try
            {
                var query = "Select IRF.id,IRF.IRFId,ag.AssessmentGroup, ag.id as AssessmentGroupId from IRF left join AssessmentGroup ag on ag.id=IRF.AssessmentGroupId order by IRF.id desc";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SkillCahnnelDetailsVM>(query, null);
                    return results;
                }
                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    var parameters = new DynamicParameters();
                //    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetAllIRF", null,
                //         commandType: CommandType.StoredProcedure);
                //}
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

        //

        public async Task<bool> IsQuestionUsedInReport(string question)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("question", question);
                    var result = await SqlMapper.QueryAsync<bool>(con, "usp_IsQuestionUsedInReport", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
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
        public async Task<bool> UpdateCancellationSource(int interviewid, string emailId)
        {
            try
            {
                var query = "Update InterviewMeetings set Cancellationsource=@Cancellationsource where id=@meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", interviewid);
                dynamicParameters.Add("@Cancellationsource", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SkillCahnnelDetailsVM>(query, null);
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
        public async Task<IEnumerable<IRF>> GetAllIRFDetailsByIRFId(int IRFId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", IRFId);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFCandidateStatsByIRFId", parameters, commandType: CommandType.StoredProcedure);
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
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAssessmentGroupInterviewerMembers(int Assessmentid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_GetAssessmentGroupInterviewerMembers", parameters,
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

        public async Task<IRFRecommendations> GetIRFRecommendation(int cloneIRFId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@cloneIRFId", cloneIRFId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<IRFRecommendations>(con, "usp_GetIRFRecommendation", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<AssessmentIRFListVM>> GetAssessmentGroupIRFList(int Assessmentid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    return await SqlMapper.QueryAsync<AssessmentIRFListVM>(con, "usp_GetAssessmentGroupIRFList", parameters,
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
        public string FetchIRFAudioLink(int irfid)
        {
            try
            {
                var query = "Select IsNull(AudioComment,'') as AudioComment From IRF where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<string>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return "";
        }
        public bool SaveIRFAudioLink(int irfid, string commentAudioLink)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                dynamicParameters.Add("@AudioComment", commentAudioLink);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_SaveCommentAudioIRF", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<AssessmentIRFListVM>> GetAssessmentGroupQuestionList(int Assessmentid)
        {
            try
            {
                var query = "Select IRF.IRFId,sk.Name as PrimarySkillInvite,ts.Comment From TechSkillComments ts join IRF on IRF.id=ts.IRFId join Skill sk on sk.id=ts.Skill where AssessmentGroupId = @Assessmentid order by IRF.IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Assessmentid", Assessmentid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<AssessmentIRFListVM>(query, dynamicParameters);
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

        public async Task<IEnumerable<ClonableSkillChannelVM>> GetAllClonableSkillChannels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //DynamicParameters parameters = new DynamicParameters();
                    //parameters.Add("@startDate", startDate);
                    //parameters.Add("@endDate", endDate);
                    return await SqlMapper.QueryAsync<ClonableSkillChannelVM>(con, "usp_GetAllClonableSkillChannels", commandType: CommandType.StoredProcedure);
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

        public async Task<IRFVm> GetClonableSkillChannelById(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<IRFVm>(con, "usp_GetClonableSkillChannelById", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IEnumerable<TransSkill> GetClonableTechSkillsbyId(int id)
        {
            try
            {
                var query = "Select tcits.*, Skill.[Name] as SkillName from TransClonableIRFTechSkill tcits join Skill on tcits.SkillId=Skill.id  where tcits.ClonableIRFId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
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
        public IEnumerable<TransSkillGeneral> GetClonableIRFSkillsGeneralbyId(int id)
        {
            try
            {
                var query = "select * from TransClonableIRFSkillGeneral where ClonableIRFId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSkillGeneral>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransJoinability> GetClonableIRFTransJoinability(int id)
        {
            try
            {
                var query = "Select * from TransClonableIRFJoinability where ClonableIRFId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransJoinability>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransSelectability> GetClonableIRFTransSelectability(int id)
        {
            try
            {
                var query = "Select * from TransClonableIRFSelectability where ClonableIRFId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<TransSelectability>(query, dynamicParameters);
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

        public IEnumerable<ClonableTemplateQuestion> GetClonableTemplateQuestions(int id)
        {
            try
            {
                var query = "select * from ClonableTemplateQuestions where ClonableIRFId = @id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ClonableTemplateQuestion>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<TransEmergencyInterviewer> GetClonableIRFTransEmergencyInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransEmergencyInterviewer>(con, "usp_GetClonableIRFTransEmergencyInterviewer", param,
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

        public IEnumerable<TransHR> GetTransClonableIRFHR(int id)
        {
            try
            {
                var query = "select tcihr.*, HR.UserId from TransClonableIRFHR tcihr inner join HR  on tcihr.HRId = HR.id where ClonableIRFId=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);

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

        public IEnumerable<TransQualityController> GetTransClonableIRFQualityController(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransQualityController>(con, "usp_GetTransClonableIRFQualityController", param,
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

        public IEnumerable<TransClientUser> GetTransClonableIRFClientUser(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransClientUser>(con, "usp_GetTransClonableIRFClientUser", param,
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

        public IEnumerable<TransInterviewer> GetInterviewersByAssessmentGroup(int assessmentGroupId)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@assessmentGroupId", assessmentGroupId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = SqlMapper.Query<TransInterviewer>(con, "usp_GetInterviewersByAssessmentGroup", param,
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

        public async Task<IEnumerable<ClonableTemplateQuestion>> GetQuestionsForAssessmentGroup(int assessmentGroupId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@assessmentGroupId", assessmentGroupId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<ClonableTemplateQuestion>(con, "usp_GetQuestionsForAssessmentGroup", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> IsClonableTemplateNameInUse(string name)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@name", name);
                    var result = (await SqlMapper.QueryAsync<bool>(con, "usp_IsClonableTemplateNameInUse", parameters,
                        commandType: CommandType.StoredProcedure)).FirstOrDefault();
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


    }
}
           
