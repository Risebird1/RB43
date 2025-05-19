using Dapper;
using Microsoft.AspNetCore.Http;
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
using static Risebird.Entities.Data.RchilliResumeMapFields;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class IRFRepository : IIRFRepository
    {
        private readonly IConfiguration _configuration;
        private string connectionString;//readonly
        private readonly string connectionStringForTesting;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IRFRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            connectionStringForTesting = "Server=tcp:risebirdtest.database.windows.net,1433;Initial Catalog=productTestProdDB;Persist Security Info=False;User ID=risebirdtestadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            _httpContextAccessor = httpContextAccessor;

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

        public async Task<IEnumerable<Client>> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client = await SqlMapper.QueryAsync<Client>(
                        con, "select * from Client", null, commandType: Text);
                }
                return client;
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public async Task<IEnumerable<Level>> GetLevels()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Level>(
                        con, "select * from Level", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<LevelTech>> GetLevelTech()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<LevelTech>(
                        con, "select * from LevelTech", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                        con, "select intp.UserId, PrimarySkill = STUFF((SELECT distinct  ', ' + Name FROM (select tis.InterviewerProfileId, ski.Name from TransInterviewerSkill tis join Skill ski on tis.SkillId = ski.id ) b WHERE b.InterviewerProfileId = intp.Id FOR XML PATH('')), 1, 1, '') FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId  where intp.IsApproved=1", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public Interviewer GetInterviewerSkillsAndResume(int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return SqlMapper.Query<Interviewer>(
                        con, $"select intp.UserId, intp.ResumeLink, intp.CompanyName, PrimarySkill = STUFF((SELECT distinct  ', ' + Name FROM (select tis.InterviewerProfileId, ski.Name from TransInterviewerSkill tis join Skill ski on tis.SkillId = ski.id ) b WHERE b.InterviewerProfileId = intp.Id FOR XML PATH('')), 1, 1, '') FROM InterviewerProfile intp join UserList ul on ul.id = intp.UserId  where intp.IsApproved=1 and intp.UserId= {userId}", null, commandType: Text)?.FirstOrDefault();
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

        public async Task<int> GetRenewedInterviewerRate(string planType = null, int? countryType = null, int? irfId = null, int? companyType = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@planType", planType);
                    parameters.Add("@countryType", countryType);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@companyType", companyType);
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_GetRenewedInterviewerRate", parameters,
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

        public async Task<List<Interviewer>> GetIRFInterviewerSkills(string assessmentgroupid, int? irfId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@assessmentgroupid", assessmentgroupid);
                    if (irfId != null && irfId < 5805)
                    {
                        var result = await con.QueryAsync<Interviewer>(";With CTE AS(Select UserId, AssessmentGroupId, value,ResumeLink, NickName, (SELECT primarySkill = STRING_AGG(sk.[Name], ',') from TransInterviewerSkill tis join Skill sk on sk.id = tis.SkillId where tis.InterviewerProfileId = ipr.id ) as PrimarySkill from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',')) Select Distinct CTE.UserId,CTE.AssessmentGroupId, ul.EmailId,CTE.NickName, CTE.ResumeLink, CTE.PrimarySkill  From CTE join Userlist ul on CTE.Userid=ul.id  where CTE.value=@assessmentgroupid and ul.IsActive=1", dp);
                        return result.Distinct().ToList();

                    }
                    else
                    {
                        var result = await con.QueryAsync<Interviewer>(";With CTE AS(Select UserId, AssessmentGroupId, value,ResumeLink, NickName, Isavailable, AvailabilityResponseDate, (SELECT primarySkill = STRING_AGG(sk.[Name], ',') from TransInterviewerSkill tis join Skill sk on sk.id = tis.SkillId where tis.InterviewerProfileId = ipr.id ) as PrimarySkill from InterviewerProfile ipr cross Apply string_split(AssessmentGroupId,',')) Select Distinct CTE.UserId,CTE.AssessmentGroupId, ul.EmailId,CTE.NickName, CTE.ResumeLink, CTE.PrimarySkill  From CTE join Userlist ul on CTE.Userid=ul.id  where CTE.value=@assessmentgroupid and (CTE.Isavailable in (0, 1, 2, 3, 4) or CTE.Isavailable is null) and ul.IsActive=1", dp);
                        return result.Distinct().ToList();

                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public async Task<IEnumerable<Skill>> GetSkills()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<Skill>(
                        con, "select id, Trim([Name]) as Name, [Description] from Skill order by [Name]", null, commandType: Text);
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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


        public async Task<IEnumerable<IRF>> GetAllIRFs(string emailID, string profileName, int? pageNumber, string searchValue = null, IRFFilter filterModel = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@pageNumber", pageNumber.HasValue ? pageNumber.Value : 1);
                    parameters.Add("@pageSize", 9);
                    parameters.Add("@searchValue", searchValue);
                    parameters.Add("@isFilterApplied", filterModel != null ? true : false);
                    parameters.Add("@startDate", filterModel != null ? filterModel.StartDate : null);
                    parameters.Add("@endDate", filterModel != null ? filterModel.EndDate : null);
                    parameters.Add("@IRFIdList", filterModel != null && filterModel.RFID != null ? string.Join(",", filterModel.RFID) : null);
                    parameters.Add("@ClientCompanyList", filterModel != null && filterModel.ClientName != null ? string.Join(",", filterModel.ClientName) : null);
                    parameters.Add("@EndCLientNameList", filterModel != null && filterModel.EndClientName != null ? string.Join(",", filterModel.EndClientName) : null);
                    parameters.Add("@PrimarySkillList", filterModel != null && filterModel.PrimarySkill != null ? string.Join(",", filterModel.PrimarySkill) : null);
                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetAllIRF", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 300);
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

        public async Task<IEnumerable<IRFFilterViewModel>> GetIRFFilterData(string emailID, string profileName, IRFFilter filterModel = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    //parameters.Add("@timeNow", DateTime.Now);
                    //parameters.Add("@isFilterApplied", filterModel != null ? true : false);
                    parameters.Add("@startDate", filterModel != null ? filterModel.StartDate : null);
                    parameters.Add("@endDate", filterModel != null ? filterModel.EndDate : null);
                    parameters.Add("@IRFIdList", filterModel != null && filterModel.RFID != null ? string.Join(",", filterModel.RFID) : null);
                    parameters.Add("@ClientCompanyList", filterModel != null && filterModel.ClientName != null ? string.Join(",", filterModel.ClientName) : null);
                    parameters.Add("@EndCLientNameList", filterModel != null && filterModel.EndClientName != null ? string.Join(",", filterModel.EndClientName) : null);
                    parameters.Add("@PrimarySkillList", filterModel != null && filterModel.PrimarySkill != null ? string.Join(",", filterModel.PrimarySkill) : null);
                    return await SqlMapper.QueryAsync<IRFFilterViewModel>(con, "usp_GetAllIRFFilterData", parameters, commandType: CommandType.StoredProcedure);
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

        public async Task<IRFVm> GetSkillChannelDetailsForUpdate(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    IRFVm _resultVM = new IRFVm();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetSkillChannelDetailsForUpdate", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null)
                        {
                            _resultVM.IRFId = !string.IsNullOrEmpty(dt.Rows[0]["IRFId"].ToString()) ? dt.Rows[0]["IRFId"].ToString() : null;
                            _resultVM.ClientName = !string.IsNullOrEmpty(dt.Rows[0]["ClientName"].ToString()) ? dt.Rows[0]["ClientName"].ToString() : null;
                            _resultVM.AssessmentGroupName = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupName"].ToString()) ? dt.Rows[0]["AssessmentGroupName"].ToString() : null;
                            _resultVM.JDTiltle = !string.IsNullOrEmpty(dt.Rows[0]["JDTiltle"].ToString()) ? dt.Rows[0]["JDTiltle"].ToString() : null;
                            _resultVM.CompanyType = !string.IsNullOrEmpty(dt.Rows[0]["CompanyType"].ToString()) ? dt.Rows[0]["CompanyType"].ToString() : null;
                            _resultVM.ClientId = !string.IsNullOrEmpty(dt.Rows[0]["ClientId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["ClientId"]) : 0;
                            _resultVM.JobCountry = !string.IsNullOrEmpty(dt.Rows[0]["JobCountry"].ToString()) ? dt.Rows[0]["JobCountry"].ToString() : null;
                            _resultVM.StartDate = !string.IsNullOrEmpty(dt.Rows[0]["StartDate"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["StartDate"]) : new DateTime();
                            _resultVM.EndDate = !string.IsNullOrEmpty(dt.Rows[0]["EndDate"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["EndDate"]) : new DateTime();
                            _resultVM.JDLink = !string.IsNullOrEmpty(dt.Rows[0]["JDLink"].ToString()) ? dt.Rows[0]["JDLink"].ToString() : null;
                            _resultVM.NoOfPosition = !string.IsNullOrEmpty(dt.Rows[0]["NoOfPosition"].ToString()) ? Convert.ToInt32(dt.Rows[0]["NoOfPosition"]) : 0;
                            _resultVM.NoOfIntterview = !string.IsNullOrEmpty(dt.Rows[0]["NoOfIntterview"].ToString()) ? Convert.ToInt32(dt.Rows[0]["NoOfIntterview"]) : 0;
                            _resultVM.RisebirdHr = !string.IsNullOrEmpty(dt.Rows[0]["RisebirdHr"].ToString()) ? Convert.ToInt32(dt.Rows[0]["RisebirdHr"]) : 0;
                            _resultVM.PerMintuteRate = !string.IsNullOrEmpty(dt.Rows[0]["PerMintuteRate"].ToString()) ? Convert.ToDouble(dt.Rows[0]["PerMintuteRate"]) : 0;
                            _resultVM.IsDeleted = !string.IsNullOrEmpty(dt.Rows[0]["IsDeleted"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsDeleted"]) : false;
                            _resultVM.JobLevel = !string.IsNullOrEmpty(dt.Rows[0]["JobLevel"].ToString()) ? Convert.ToInt32(dt.Rows[0]["JobLevel"]) : null;
                            _resultVM.TotalInvites = !string.IsNullOrEmpty(dt.Rows[0]["TotalInvites"].ToString()) ? Convert.ToInt32(dt.Rows[0]["TotalInvites"]) : 0;
                            _resultVM.CalendlyLink = !string.IsNullOrEmpty(dt.Rows[0]["CalendlyLink"].ToString()) ? dt.Rows[0]["CalendlyLink"].ToString() : null;
                            _resultVM.CreatedOn = !string.IsNullOrEmpty(dt.Rows[0]["CreatedOn"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["CreatedOn"]) : new DateTime();
                            _resultVM.TotalBudget = !string.IsNullOrEmpty(dt.Rows[0]["TotalBudget"].ToString()) ? Convert.ToInt32(dt.Rows[0]["TotalBudget"]) : 0;
                            _resultVM.Discount = !string.IsNullOrEmpty(dt.Rows[0]["Discount"].ToString()) ? Convert.ToDouble(dt.Rows[0]["Discount"]) : 0;
                            _resultVM.IsBiometricEnabled = !string.IsNullOrEmpty(dt.Rows[0]["IsBiometricEnabled"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsBiometricEnabled"]) : false;
                            _resultVM.PriorityLevel = !string.IsNullOrEmpty(dt.Rows[0]["PriorityLevel"].ToString()) ? Convert.ToInt32(dt.Rows[0]["PriorityLevel"]) : null;
                            _resultVM.IsPaused = !string.IsNullOrEmpty(dt.Rows[0]["IsPaused"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsPaused"]) : false;
                            _resultVM.CompanyDetails = !string.IsNullOrEmpty(dt.Rows[0]["CompanyDetails"].ToString()) ? dt.Rows[0]["CompanyDetails"].ToString() : null;
                            _resultVM.Comment = !string.IsNullOrEmpty(dt.Rows[0]["Comment"].ToString()) ? dt.Rows[0]["Comment"].ToString() : null;
                            _resultVM.idProgress = !string.IsNullOrEmpty(dt.Rows[0]["idProgress"].ToString()) ? Convert.ToInt32(dt.Rows[0]["idProgress"]) : 0;
                            _resultVM.BussinessType = !string.IsNullOrEmpty(dt.Rows[0]["BussinessType"].ToString()) ? dt.Rows[0]["BussinessType"].ToString() : null;
                            _resultVM.Heuristicsscore = !string.IsNullOrEmpty(dt.Rows[0]["Heuristicsscore"].ToString()) ? Convert.ToInt32(dt.Rows[0]["Heuristicsscore"]) : 0;
                            _resultVM.PrimarySkillInvite = !string.IsNullOrEmpty(dt.Rows[0]["PrimarySkillInvite"].ToString()) ? dt.Rows[0]["PrimarySkillInvite"].ToString() : null;
                            _resultVM.BillingOwner = !string.IsNullOrEmpty(dt.Rows[0]["BillingOwner"].ToString()) ? Convert.ToInt32(dt.Rows[0]["BillingOwner"]) : 0;
                            _resultVM.AssessmentGroupId = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupId"].ToString()) ? dt.Rows[0]["AssessmentGroupId"].ToString() : null;
                            _resultVM.IsQCRequired = !string.IsNullOrEmpty(dt.Rows[0]["IsQCRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsQCRequired"]) : false;
                            _resultVM.IsJoinabilitySelectabilityRequired = !string.IsNullOrEmpty(dt.Rows[0]["IsJoinabilitySelectabilityRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsJoinabilitySelectabilityRequired"]) : false;
                            _resultVM.IsGeneralSkillRequired = !string.IsNullOrEmpty(dt.Rows[0]["IsGeneralSkillRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsGeneralSkillRequired"]) : false;
                            _resultVM.AudioComment = !string.IsNullOrEmpty(dt.Rows[0]["AudioComment"].ToString()) ? dt.Rows[0]["AudioComment"].ToString() : null;
                            _resultVM.CreatedFromTemplateId = !string.IsNullOrEmpty(dt.Rows[0]["CreatedFromTemplateId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["CreatedFromTemplateId"]) : 0;
                            _resultVM.ModifiedOn = !string.IsNullOrEmpty(dt.Rows[0]["ModifiedOn"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["ModifiedOn"]) : null;
                            _resultVM.MeetingPlatform = !string.IsNullOrEmpty(dt.Rows[0]["MeetingPlatform"].ToString()) ? dt.Rows[0]["MeetingPlatform"].ToString() : null;
                            _resultVM.IsAadhaarVerificationRequired = !string.IsNullOrEmpty(dt.Rows[0]["IsAadhaarVerificationRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsAadhaarVerificationRequired"]) : false;
                            _resultVM.IsCandidateavailability = !string.IsNullOrEmpty(dt.Rows[0]["IsCandidateavailability"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsCandidateavailability"]) : false;
                            _resultVM.IsMeetingNotificationSend = !string.IsNullOrEmpty(dt.Rows[0]["IsMeetingNotificationSend"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsMeetingNotificationSend"]) : false;
                            _resultVM.IsMeetingAutoCancelled = !string.IsNullOrEmpty(dt.Rows[0]["IsMeetingAutoCancelled"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsMeetingAutoCancelled"]) : false;
                            _resultVM.IsLocked = !string.IsNullOrEmpty(dt.Rows[0]["IsLocked"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsLocked"]) : false;
                            _resultVM.flagcount = !string.IsNullOrEmpty(dt.Rows[0]["flagcount"].ToString()) ? Convert.ToInt32(dt.Rows[0]["flagcount"]) : 0;
                            _resultVM.InterviewDuration = !string.IsNullOrEmpty(dt.Rows[0]["InterviewDuration"].ToString()) ? Convert.ToInt32(dt.Rows[0]["InterviewDuration"]) : 0;
                            _resultVM.IsFixedInterviewduration = !string.IsNullOrEmpty(dt.Rows[0]["IsFixedInterviewduration"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsFixedInterviewduration"]) : false;
                            _resultVM.FixedAmount = !string.IsNullOrEmpty(dt.Rows[0]["FixedAmount"].ToString()) ? Convert.ToInt32(dt.Rows[0]["FixedAmount"]) : 0;
                            _resultVM.IsNewSkillChannel = !string.IsNullOrEmpty(dt.Rows[0]["IsNewSkillChannel"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsNewSkillChannel"]) : false;
                            _resultVM.ClientCompName = !string.IsNullOrEmpty(dt.Rows[0]["ClientCompName"].ToString()) ? dt.Rows[0]["ClientCompName"].ToString() : null;
                            _resultVM.IsClientCandidateAvailablily = !string.IsNullOrEmpty(dt.Rows[0]["IsClientCandidateAvailablily"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsClientCandidateAvailablily"]) : false;
                            _resultVM.IsClientMeetingNotificationSend = !string.IsNullOrEmpty(dt.Rows[0]["IsClientMeetingNotificationSend"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsClientMeetingNotificationSend"]) : false;
                            _resultVM.IsClientMeetingAutoCancelled = !string.IsNullOrEmpty(dt.Rows[0]["IsClientMeetingAutoCancelled"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsClientMeetingAutoCancelled"]) : false;
                            _resultVM.ClientInterviewDuration = !string.IsNullOrEmpty(dt.Rows[0]["ClientInterviewDuration"].ToString()) ? Convert.ToInt32(dt.Rows[0]["ClientInterviewDuration"]) : 0;
                            _resultVM.DMApproved = !string.IsNullOrEmpty(dt.Rows[0]["DMApproved"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["DMApproved"]) : false;
                            _resultVM.SkillSetsevaluate = !string.IsNullOrEmpty(dt.Rows[0]["SkillSetsevaluate"].ToString()) ? Convert.ToInt32(dt.Rows[0]["SkillSetsevaluate"]) : 1;
                            _resultVM.JDTitleTwo = !string.IsNullOrEmpty(dt.Rows[0]["JDTitleTwo"].ToString()) ? dt.Rows[0]["JDTitleTwo"].ToString() : null;
                            _resultVM.IsVideoRequired = !string.IsNullOrEmpty(dt.Rows[0]["IsVideoRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsVideoRequired"]) : false;
                            _resultVM.TechRubricType = !string.IsNullOrEmpty(dt.Rows[0]["TechRubricType"].ToString()) ? dt.Rows[0]["TechRubricType"].ToString() : null;
                            _resultVM.CandidateIdverification = !string.IsNullOrEmpty(dt.Rows[0]["CandidateIdverification"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["CandidateIdverification"]) : false;
                            _resultVM.CodeSnippetRequired = !string.IsNullOrEmpty(dt.Rows[0]["CodeSnippetRequired"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["CodeSnippetRequired"]) : false;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransSkill> transTechSkills = new List<TransSkill>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transTechSkills.Add(new TransSkill
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    SkillId = !string.IsNullOrEmpty(dt.Rows[i]["SkillId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillId"]) : 0,
                                    LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0,
                                    Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0,
                                    Concepts = !string.IsNullOrEmpty(dt.Rows[i]["Concepts"].ToString()) ? dt.Rows[i]["Concepts"].ToString() : null,
                                    Natureofquestion = !string.IsNullOrEmpty(dt.Rows[i]["Natureofquestion"].ToString()) ? dt.Rows[i]["Natureofquestion"].ToString() : null,
                                    Timelimit = !string.IsNullOrEmpty(dt.Rows[i]["Timelimit"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Timelimit"]) : 0,
                                    SkillName = !string.IsNullOrEmpty(dt.Rows[i]["SkillName"].ToString()) ? dt.Rows[i]["SkillName"].ToString() : null,
                                    isEliminationskill = !string.IsNullOrEmpty(dt.Rows[i]["isEliminationskill"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["isEliminationskill"]) : false,
                                    SkillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1,
                                });
                            }
                            _resultVM.SkillCollection = (IList<TransSkill>)(transTechSkills);
                        }
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransSkillGeneral> transGeneralSkills = new List<TransSkillGeneral>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transGeneralSkills.Add(new TransSkillGeneral
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    Type = !string.IsNullOrEmpty(dt.Rows[i]["Type"].ToString()) ? dt.Rows[i]["Type"].ToString() : null,
                                    Value = !string.IsNullOrEmpty(dt.Rows[i]["Value"].ToString()) ? dt.Rows[i]["Value"].ToString() : null,
                                    LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0,
                                    Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0,

                                });
                            }
                            _resultVM.SkillGeneral = (IList<TransSkillGeneral>)(transGeneralSkills);
                        }
                    }
                    if (dataset.Tables.Count > 3)
                    {
                        dt = dataset.Tables[3];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransJoinability> transJoinability = new List<TransJoinability>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transJoinability.Add(new TransJoinability
                                {
                                    //id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    Type = !string.IsNullOrEmpty(dt.Rows[i]["Type"].ToString()) ? dt.Rows[i]["Type"].ToString() : null,
                                    Value = !string.IsNullOrEmpty(dt.Rows[i]["Value"].ToString()) ? dt.Rows[i]["Value"].ToString() : null,
                                    LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0,
                                    Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0,

                                });
                            }
                            _resultVM.Joinability = (IList<TransJoinability>)(transJoinability);
                        }
                    }
                    if (dataset.Tables.Count > 4)
                    {
                        dt = dataset.Tables[4];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransInterviewer> transInterviewer = new List<TransInterviewer>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transInterviewer.Add(new TransInterviewer
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    InterviewerId = !string.IsNullOrEmpty(dt.Rows[i]["InterviewerId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewerId"]) : 0,
                                    UserId = !string.IsNullOrEmpty(dt.Rows[i]["UserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["UserId"]) : 0,
                                    PerSuccessRate = !string.IsNullOrEmpty(dt.Rows[i]["PerSuccessRate"].ToString()) ? Convert.ToInt32(dt.Rows[i]["PerSuccessRate"]) : 0,
                                    IsActive = !string.IsNullOrEmpty(dt.Rows[i]["IsActive"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsActive"]) : false,
                                    IsAvailable = !string.IsNullOrEmpty(dt.Rows[i]["IsAvailable"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsAvailable"]) : false,
                                    IsSMEVerified = !string.IsNullOrEmpty(dt.Rows[i]["IsSMEVerified"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsSMEVerified"]) : false,
                                    Surcharge = !string.IsNullOrEmpty(dt.Rows[i]["Surcharge"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Surcharge"]) : 0,
                                    PrimarySkill = !string.IsNullOrEmpty(dt.Rows[i]["PrimarySkill"].ToString()) ? dt.Rows[i]["PrimarySkill"].ToString() : null,
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    NickName = !string.IsNullOrEmpty(dt.Rows[i]["NickName"].ToString()) ? dt.Rows[i]["NickName"].ToString() : null,
                                    ResumeLink = !string.IsNullOrEmpty(dt.Rows[i]["ResumeLink"].ToString()) ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    CodedResumeLink = !string.IsNullOrEmpty(dt.Rows[i]["CodedResumeLink"].ToString()) ? dt.Rows[i]["CodedResumeLink"].ToString() : null,
                                    Source = !string.IsNullOrEmpty(dt.Rows[i]["Source"].ToString()) ? dt.Rows[i]["Source"].ToString() : null,
                                    CompanyName = !string.IsNullOrEmpty(dt.Rows[i]["CompanyName"].ToString()) ? dt.Rows[i]["CompanyName"].ToString() : null,
                                    ClientAction = !string.IsNullOrEmpty(dt.Rows[i]["ClientAction"].ToString()) ? dt.Rows[i]["ClientAction"].ToString() : null,
                                    IsResumeRedactAttempted = !string.IsNullOrEmpty(dt.Rows[i]["IsResumeRedactAttempted"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsResumeRedactAttempted"]) : false,
                                    IsCampaignActivity = !string.IsNullOrEmpty(dt.Rows[i]["IsCampaignActivity"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsCampaignActivity"]) : false,
                                    CampaignName = !string.IsNullOrEmpty(dt.Rows[i]["CampaignName"].ToString()) ? dt.Rows[i]["CampaignName"].ToString() : null,
                                    AIVerdict = !string.IsNullOrEmpty(dt.Rows[i]["AIVerdict"].ToString()) ? dt.Rows[i]["AIVerdict"].ToString() : null

                                });
                            }
                            _resultVM.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                        }
                    }
                    if (dataset.Tables.Count > 5)
                    {
                        dt = dataset.Tables[5];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransSelectability> transSelectability = new List<TransSelectability>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transSelectability.Add(new TransSelectability
                                {
                                    //id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    Type = !string.IsNullOrEmpty(dt.Rows[i]["Type"].ToString()) ? dt.Rows[i]["Type"].ToString() : null,
                                    Value = !string.IsNullOrEmpty(dt.Rows[i]["Value"].ToString()) ? dt.Rows[i]["Value"].ToString() : null,
                                    LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0,
                                    Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0,

                                });
                            }
                            _resultVM.Selectability = (IList<TransSelectability>)(transSelectability);
                        }
                    }
                    if (dataset.Tables.Count > 6)
                    {
                        dt = dataset.Tables[6];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransHR> transHR = new List<TransHR>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transHR.Add(new TransHR
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    HRId = !string.IsNullOrEmpty(dt.Rows[i]["HRId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["HRId"]) : 0,
                                    UserId = !string.IsNullOrEmpty(dt.Rows[i]["UserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["UserId"]) : 0

                                });
                            }
                            _resultVM.HRCollection = (IList<TransHR>)(transHR);
                        }
                    }
                    if (dataset.Tables.Count > 7)
                    {
                        dt = dataset.Tables[7];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransClientUser> transClientUser = new List<TransClientUser>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transClientUser.Add(new TransClientUser
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    ClientUserId = !string.IsNullOrEmpty(dt.Rows[i]["ClientUserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientUserId"]) : 0,
                                    ClientRoleId = !string.IsNullOrEmpty(dt.Rows[i]["ClientRoleId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["ClientRoleId"]) : 0,
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    uniqueid = !string.IsNullOrEmpty(dt.Rows[i]["uniqueid"].ToString()) ? dt.Rows[i]["uniqueid"].ToString() : null,
                                    IRFName = !string.IsNullOrEmpty(dt.Rows[i]["IRFName"].ToString()) ? dt.Rows[i]["IRFName"].ToString() : null,
                                    FirstName = !string.IsNullOrEmpty(dt.Rows[i]["FirstName"].ToString()) ? dt.Rows[i]["FirstName"].ToString() : null,
                                    Role = !string.IsNullOrEmpty(dt.Rows[i]["Role"].ToString()) ? dt.Rows[i]["Role"].ToString() : null,

                                });
                            }
                            _resultVM.ClientCollection = (IList<TransClientUser>)(transClientUser);
                        }
                    }
                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IRFVm> GetSkillChannelInterviewersForEdit(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    IRFVm _resultVM = new IRFVm();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetSkillChannelInterviewersForEdit", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@irfId", irfId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null)
                        {
                            _resultVM.IRFId = !string.IsNullOrEmpty(dt.Rows[0]["IRFId"].ToString()) ? dt.Rows[0]["IRFId"].ToString() : null;
                            _resultVM.ClientName = !string.IsNullOrEmpty(dt.Rows[0]["ClientName"].ToString()) ? dt.Rows[0]["ClientName"].ToString() : null;
                            _resultVM.AssessmentGroupName = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupName"].ToString()) ? dt.Rows[0]["AssessmentGroupName"].ToString() : null;
                            _resultVM.JDTiltle = !string.IsNullOrEmpty(dt.Rows[0]["JDTiltle"].ToString()) ? dt.Rows[0]["JDTiltle"].ToString() : null;
                            _resultVM.CreatedOn = !string.IsNullOrEmpty(dt.Rows[0]["CreatedOn"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["CreatedOn"]) : new DateTime();
                            _resultVM.AssessmentGroupId = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupId"].ToString()) ? dt.Rows[0]["AssessmentGroupId"].ToString() : null;
                            _resultVM.Heuristicsscore = !string.IsNullOrEmpty(dt.Rows[0]["Heuristicsscore"].ToString()) ? Convert.ToInt32(dt.Rows[0]["Heuristicsscore"]) : 0;
                            _resultVM.JobCountry = !string.IsNullOrEmpty(dt.Rows[0]["JobCountry"].ToString()) ? dt.Rows[0]["JobCountry"].ToString() : null;
                            _resultVM.CompanyType = !string.IsNullOrEmpty(dt.Rows[0]["CompanyType"].ToString()) ? dt.Rows[0]["CompanyType"].ToString() : null;
                            _resultVM.IsNewSkillChannel = !string.IsNullOrEmpty(dt.Rows[0]["IsNewSkillChannel"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsNewSkillChannel"]) : false;
                            _resultVM.FlatRate = !string.IsNullOrEmpty(dt.Rows[0]["FlatRate"].ToString()) ? Convert.ToInt32(dt.Rows[0]["FlatRate"]) : 0;

                        }
                    }

                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<TransInterviewer> transInterviewer = new List<TransInterviewer>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                transInterviewer.Add(new TransInterviewer
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                                    InterviewerId = !string.IsNullOrEmpty(dt.Rows[i]["InterviewerId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["InterviewerId"]) : 0,
                                    UserId = !string.IsNullOrEmpty(dt.Rows[i]["UserId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["UserId"]) : 0,
                                    PerSuccessRate = !string.IsNullOrEmpty(dt.Rows[i]["PerSuccessRate"].ToString()) ? Convert.ToInt32(dt.Rows[i]["PerSuccessRate"]) : 0,
                                    IsActive = !string.IsNullOrEmpty(dt.Rows[i]["IsActive"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsActive"]) : false,
                                    IsAvailable = !string.IsNullOrEmpty(dt.Rows[i]["IsAvailable"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsAvailable"]) : false,
                                    IsSMEVerified = !string.IsNullOrEmpty(dt.Rows[i]["IsSMEVerified"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsSMEVerified"]) : false,
                                    Surcharge = !string.IsNullOrEmpty(dt.Rows[i]["Surcharge"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Surcharge"]) : 0,
                                    PrimarySkill = !string.IsNullOrEmpty(dt.Rows[i]["PrimarySkill"].ToString()) ? dt.Rows[i]["PrimarySkill"].ToString() : null,
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    NickName = !string.IsNullOrEmpty(dt.Rows[i]["NickName"].ToString()) ? dt.Rows[i]["NickName"].ToString() : null,
                                    ResumeLink = !string.IsNullOrEmpty(dt.Rows[i]["ResumeLink"].ToString()) ? dt.Rows[i]["ResumeLink"].ToString() : null,
                                    CodedResumeLink = !string.IsNullOrEmpty(dt.Rows[i]["CodedResumeLink"].ToString()) ? dt.Rows[i]["CodedResumeLink"].ToString() : null,
                                    Source = !string.IsNullOrEmpty(dt.Rows[i]["Source"].ToString()) ? dt.Rows[i]["Source"].ToString() : null,
                                    CompanyName = !string.IsNullOrEmpty(dt.Rows[i]["CompanyName"].ToString()) ? dt.Rows[i]["CompanyName"].ToString() : null,
                                    ClientAction = !string.IsNullOrEmpty(dt.Rows[i]["ClientAction"].ToString()) ? dt.Rows[i]["ClientAction"].ToString() : null,
                                    IsResumeRedactAttempted = !string.IsNullOrEmpty(dt.Rows[i]["IsResumeRedactAttempted"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsResumeRedactAttempted"]) : false,
                                    IsCampaignActivity = !string.IsNullOrEmpty(dt.Rows[i]["IsCampaignActivity"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["IsCampaignActivity"]) : false,
                                    CampaignName = !string.IsNullOrEmpty(dt.Rows[i]["CampaignName"].ToString()) ? dt.Rows[i]["CampaignName"].ToString() : null,
                                    AIVerdict = !string.IsNullOrEmpty(dt.Rows[i]["AIVerdict"].ToString()) ? dt.Rows[i]["AIVerdict"].ToString() : null,
                                    AIScreeningScore = !string.IsNullOrEmpty(dt.Rows[i]["AIScreeningScore"].ToString()) ? Convert.ToInt32(dt.Rows[i]["AIScreeningScore"]) : 0,

                                });
                            }
                            _resultVM.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                        }
                    }

                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
                    var result = await SqlMapper.QueryFirstOrDefaultAsync<string>(con, "usp_GetTechSkillofSkillChannelByQuestion", parameters,
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
                    if (!isCloneTemplate)
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
        public IRF GetIRFbyId(int id, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
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

        public IRF GetIRFbyIRFName(string irfName)
        {
            try
            {
                var query = "select IRF.*, Client.[Name] as ClientCompName, Client.GST as ClientGST, Client.OfficeAddress as ClientAddress from IRF join Client on IRF.ClientId=client.id where IRF.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", irfName);
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

                    if (irfDetails.SkillCollection != null && irfDetails.SkillCollection.Count() > 0)
                    {
                        techSkillDt.Columns.Add("IRFId", typeof(int));
                        techSkillDt.Columns.Add("LevelId", typeof(int));
                        techSkillDt.Columns.Add("SkillId", typeof(int));
                        techSkillDt.Columns.Add("Weightage", typeof(int));
                        techSkillDt.Columns.Add("Concepts", typeof(string));
                        techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                        techSkillDt.Columns.Add("Timelimit", typeof(int));
                        techSkillDt.Columns.Add("isEliminationskill", typeof(bool));
                        techSkillDt.Columns.Add("SkillSet", typeof(int));
                        techSkillDt.Columns.Add("SkillName", typeof(string));

                        foreach (var item in irfDetails.SkillCollection)
                        {
                            if (string.IsNullOrEmpty(item.Concepts))//|| irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Concepts = "";
                            }
                            if (string.IsNullOrEmpty(item.Natureofquestion))// || irfDetails.TechRubricType == TechRubricTypes.Basic
                            {
                                item.Natureofquestion = "";
                            }
                            if (irfDetails.TechRubricType == TechRubricTypes.Basic)
                            {
                                item.Timelimit = 0;
                                item.isEliminationskill = false;
                            }
                            techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill, item.SkillSet, item.SkillName);
                        }
                    }



                    DataTable interviewerDt = new DataTable();

                    if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        //irfDetails.InterviewerCollection = (IList<TransInterviewer>)irfDetails.InterviewerCollection.Distinct();
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("Surcharge", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in irfDetails.InterviewerCollection.Distinct())
                        {
                            interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.Surcharge, item.IsActive, item.IsAvailable);
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

                    DataTable hRDt = new DataTable();
                    if (irfDetails.HRCollection != null && irfDetails.HRCollection.Count() > 0)
                    {
                        hRDt.Columns.Add("IRFId", typeof(int));
                        hRDt.Columns.Add("UserId", typeof(int));

                        foreach (var item in irfDetails.HRCollection)
                        {
                            hRDt.Rows.Add(irfDetails.id, item.UserId);
                        }
                    }

                    SqlCommand cmdCreateIRF = new SqlCommand("usp_AddIRFDetails", con);
                    cmdCreateIRF.CommandType = CommandType.StoredProcedure;


                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.NVarChar));
                    cmdCreateIRF.Parameters["@IRFId"].Value = irfDetails.IRFId;

                    //cmdCreateIRF.Parameters.Add(new SqlParameter("@ClientName", SqlDbType.VarChar));
                    //cmdCreateIRF.Parameters["@ClientName"].Value = irfDetails.ClientName;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@JDTiltle", SqlDbType.NVarChar));
                    cmdCreateIRF.Parameters["@JDTiltle"].Value = irfDetails.JDTiltle;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@CompanyType", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@CompanyType"].Value = irfDetails.CompanyType;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@ClientId"].Value = irfDetails.ClientId;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@JobCountry", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@JobCountry"].Value = irfDetails.JobCountry;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime));
                    cmdCreateIRF.Parameters["@StartDate"].Value = irfDetails.StartDate;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime));
                    cmdCreateIRF.Parameters["@EndDate"].Value = irfDetails.EndDate;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@JDLink", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@JDLink"].Value = irfDetails.JDLink ?? string.Empty;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@NoOfPosition", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@NoOfPosition"].Value = irfDetails.NoOfPosition;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@NoOfIntterview", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@NoOfIntterview"].Value = irfDetails.NoOfIntterview;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@RisebirdHr", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@RisebirdHr"].Value = irfDetails.RisebirdHr;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@PerMintuteRate", SqlDbType.Real));
                    cmdCreateIRF.Parameters["@PerMintuteRate"].Value = irfDetails.PerMintuteRate;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@JobLevel", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@JobLevel"].Value = irfDetails.JobLevel;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@CalendlyLink", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@CalendlyLink"].Value = irfDetails.CalendlyLink;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@TotalBudget", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@TotalBudget"].Value = irfDetails.TotalBudget;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsBiometricEnabled", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsBiometricEnabled"].Value = irfDetails.IsBiometricEnabled;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@PriorityLevel", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@PriorityLevel"].Value = irfDetails.PriorityLevel;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@CompanyDetails", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@CompanyDetails"].Value = irfDetails.CompanyDetails ?? string.Empty;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@Type"].Value = irfDetails.Type;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@Heuristicsscore", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@Heuristicsscore"].Value = irfDetails.Heuristicsscore;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@PrimarySkillInvite", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@PrimarySkillInvite"].Value = irfDetails.PrimarySkillInvite;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@BillingOwner", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@BillingOwner"].Value = irfDetails.BillingOwner;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@AssessmentGroupId"].Value = irfDetails.AssessmentGroupId;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@SkillSetsevaluate", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@SkillSetsevaluate"].Value = irfDetails.SkillSetsevaluate;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@JDTitleTwo", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@JDTitleTwo"].Value = irfDetails.JDTitleTwo;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsQCRequired", SqlDbType.Bit));
                    //cmdUpdateIRF.Parameters["@IsQCRequired"].Value = irfDetails.IsQCRequired;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsJoinabilitySelectabilityRequired", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsJoinabilitySelectabilityRequired"].Value = irfDetails.IsJoinabilitySelectabilityRequired;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsGeneralSkillRequired", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsGeneralSkillRequired"].Value = irfDetails.IsGeneralSkillRequired;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = irfDetails.IsAadhaarVerificationRequired;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsCandidateavailability", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsCandidateavailability"].Value = irfDetails.IsCandidateavailability;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsMeetingAutoCancelled", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsMeetingAutoCancelled"].Value = irfDetails.IsMeetingAutoCancelled;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsMeetingNotificationSend", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsMeetingNotificationSend"].Value = irfDetails.IsMeetingNotificationSend;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@CreatedBy"].Value = irfDetails.CreatedBy;


                    cmdCreateIRF.Parameters.Add(new SqlParameter("@SkillChannelSME", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@SkillChannelSME"].Value = irfDetails.SkillChannelSMEs?.Where(x => x.IsSelectedForIRF)?.FirstOrDefault()?.InterviewerProfileId;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                    cmdCreateIRF.Parameters["@CreatedOn"].Value = irfDetails.CreatedOn;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsNewSkillChannel", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsNewSkillChannel"].Value = true;//this is set true for all the skill channels created from May23 when changes to the financial model were instituted

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@FlatRate", SqlDbType.Int));
                    cmdCreateIRF.Parameters["@FlatRate"].Value = irfDetails.FlatRate;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@IsVideoRequired", SqlDbType.Bit));
                    cmdCreateIRF.Parameters["@IsVideoRequired"].Value = irfDetails.IsVideoRequired;

                    cmdCreateIRF.Parameters.Add(new SqlParameter("@TechRubricType", SqlDbType.VarChar));
                    cmdCreateIRF.Parameters["@TechRubricType"].Value = irfDetails.TechRubricType;
                    //cmdCreateIRF.Parameters.Add(new SqlParameter("@Surcharge", SqlDbType.Int));
                    //cmdCreateIRF.Parameters["@Surcharge"].Value = irfDetails.Surcharge;

                    //cmdCreateIRF.Parameters.Add(new SqlParameter("@InterviewDuration", SqlDbType.Int));
                    //cmdCreateIRF.Parameters["@InterviewDuration"].Value = irfDetails.InterviewDuration;

                    //cmdCreateIRF.Parameters.Add(new SqlParameter("@IsFixedInterviewduration", SqlDbType.Bit));
                    //cmdCreateIRF.Parameters["@IsFixedInterviewduration"].Value = irfDetails.IsFixedInterviewduration;

                    //cmdCreateIRF.Parameters.Add(new SqlParameter("@FixedAmount", SqlDbType.Int));
                    //cmdCreateIRF.Parameters["@FixedAmount"].Value = irfDetails.IsFixedInterviewduration ? irfDetails.FixedAmount : null;


                    SqlParameter idIRFParam = new SqlParameter();
                    idIRFParam.ParameterName = "@idIrf";
                    idIRFParam.DbType = DbType.Int32;
                    idIRFParam.Direction = ParameterDirection.Output;
                    cmdCreateIRF.Parameters.Add(idIRFParam);


                    if (joinabilityDt != null && joinabilityDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@JoinabilityBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@JoinabilityBulkData"].Value = joinabilityDt;
                    }
                    if (selectabilityDt != null && selectabilityDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@SelectabilityBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@SelectabilityBulkData"].Value = selectabilityDt;
                    }
                    if (skillGeneralDt != null && skillGeneralDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@SkillGeneralBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@SkillGeneralBulkData"].Value = skillGeneralDt;
                    }
                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    }
                    if (emergencyInterviewerDt != null && emergencyInterviewerDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@EmergencyInterviewerBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@EmergencyInterviewerBulkData"].Value = emergencyInterviewerDt;
                    }
                    if (clientUserDt != null && clientUserDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    }
                    //if (qualityControllerDt != null && qualityControllerDt.Rows.Count > 0)
                    //{
                    //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@QCBulkData", SqlDbType.Structured));
                    //    cmdUpdateIRF.Parameters["@QCBulkData"].Value = qualityControllerDt;
                    //}
                    if (hRDt != null && hRDt.Rows.Count > 0)
                    {
                        cmdCreateIRF.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                        cmdCreateIRF.Parameters["@HRBulkData"].Value = hRDt;
                    }

                    con.Open();

                    if (cmdCreateIRF.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    irfID = Convert.ToInt32(cmdCreateIRF?.Parameters["@idIrf"]?.Value);

                    return true;

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

        public async Task<Tuple<bool, bool>> UpdateIRFDetails(IRFVm irfDetails, List<InterviewerSuccessRateChangeData> interviewerRateChangesList = null, List<RemovedInterviewersFromSkillsChannelData> removedInterviewers = null, List<AddedNewInterviewersInSkillsChannelData> addedNewInterviewers = null, List<RemovedTechSkillsFromIRF> removedTechSkills = null)
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
                    techSkillDt.Columns.Add("isEliminationskill", typeof(bool));
                    techSkillDt.Columns.Add("SkillSet", typeof(int));
                    techSkillDt.Columns.Add("SkillName", typeof(string));

                    foreach (var item in irfDetails.SkillCollection?.Where(x => x.LevelId > 0))
                    {
                        //if (item.SkillId != 0)
                        //{

                        //}
                        if (string.IsNullOrEmpty(item.Concepts) || irfDetails.TechRubricType == TechRubricTypes.Basic)
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion) || irfDetails.TechRubricType == TechRubricTypes.Basic)
                        {
                            item.Natureofquestion = "";
                        }
                        if (irfDetails.TechRubricType == TechRubricTypes.Basic)
                        {
                            item.Timelimit = 0;
                            item.isEliminationskill = false;
                        }
                        techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill, item.SkillSet, item.SkillName);

                    }

                    DataTable interviewerDt = new DataTable();

                    if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        if (irfDetails.IsNewSkillChannel)
                        {
                            interviewerDt.Columns.Add("Surcharge", typeof(int));

                        }
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));
                        interviewerDt.Columns.Add("Source", typeof(string));
                        interviewerDt.Columns.Add("IsCampaignActivity", typeof(bool));
                        interviewerDt.Columns.Add("CampaignId", typeof(int));
                        interviewerDt.Columns.Add("OpenAIScreeningResponse", typeof(string));
                        interviewerDt.Columns.Add("AIVerdict", typeof(string));
                        interviewerDt.Columns.Add("IsSurchargeApprove", typeof(bool));

                        foreach (var item in irfDetails.InterviewerCollection)
                        {
                            if (irfDetails.IsNewSkillChannel)
                            {
                                interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.Surcharge,
                                    item.IsActive, item.IsAvailable, item.Source, item.IsCampaignActivity, item.CampaignId,
                                    item.OpenAIScreeningResponse, item.AIVerdict, item.IsSurchargeApprove);
                            }
                            else
                            {
                                interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive,
                                    item.IsAvailable, item.Source, item.IsCampaignActivity, item.CampaignId,
                                    item.OpenAIScreeningResponse, item.AIVerdict, item.IsSurchargeApprove);
                            }
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

                    //DataTable qualityControllerDt = new DataTable();

                    //if (irfDetails.QualityController != null && irfDetails.QualityController.Count() != 0 && !(irfDetails.QualityController.Where(x => x.UserId == 0).Any()) && irfDetails.IsQCRequired)
                    //{
                    //    qualityControllerDt.Columns.Add("IRFId", typeof(int));
                    //    qualityControllerDt.Columns.Add("UserId", typeof(int));
                    //    qualityControllerDt.Columns.Add("PerSuccessRate", typeof(int));
                    //    qualityControllerDt.Columns.Add("IsActive", typeof(bool));

                    //    foreach (var item in irfDetails.QualityController)
                    //    {
                    //        qualityControllerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive);
                    //    }
                    //}

                    DataTable hRDt = new DataTable();
                    hRDt.Columns.Add("IRFId", typeof(int));
                    hRDt.Columns.Add("UserId", typeof(int));

                    foreach (var item in irfDetails.HRCollection)
                    {
                        hRDt.Rows.Add(irfDetails.id, item.UserId);
                    }

                    DataTable smeDt = new DataTable();

                    if (irfDetails.SkillChannelSMEs != null && irfDetails.SkillChannelSMEs.Where(x => x.IsSelectedForIRF) != null)
                    {
                        smeDt.Columns.Add("id", typeof(int));
                        //irfDetails.SkillChannelSMEs?.Where(x => x.IsSelectedForIRF)?.FirstOrDefault()?.InterviewerProfileId
                        foreach (var item in irfDetails.SkillChannelSMEs.Where(x => x.IsSelectedForIRF))
                        {
                            smeDt.Rows.Add(item.InterviewerProfileId);
                        }
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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@SkillSetsevaluate", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@SkillSetsevaluate"].Value = irfDetails.SkillSetsevaluate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDTitleTwo", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JDTitleTwo"].Value = irfDetails.JDTitleTwo;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@MeetingPlatform"].Value = irfDetails.MeetingPlatform;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsQCRequired", SqlDbType.Bit));
                    //cmdUpdateIRF.Parameters["@IsQCRequired"].Value = irfDetails.IsQCRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsJoinabilitySelectabilityRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsJoinabilitySelectabilityRequired"].Value = irfDetails.IsJoinabilitySelectabilityRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsGeneralSkillRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsGeneralSkillRequired"].Value = irfDetails.IsGeneralSkillRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = irfDetails.IsAadhaarVerificationRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsCandidateavailability", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsCandidateavailability"].Value = irfDetails.IsCandidateavailability;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingAutoCancelled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingAutoCancelled"].Value = irfDetails.IsMeetingAutoCancelled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingNotificationSend", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingNotificationSend"].Value = irfDetails.IsMeetingNotificationSend;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@SkillChannelSME", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@SkillChannelSME"].Value = irfDetails.SkillChannelSMEs?.Where(x => x.IsSelectedForIRF)?.FirstOrDefault()?.InterviewerProfileId;


                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@timeNow"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ModifiedBy", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@ModifiedBy"].Value = irfDetails.ModifiedBy;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsLocked", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsLocked"].Value = irfDetails.IsLocked;


                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsNewSkillChannel", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsNewSkillChannel"].Value = irfDetails.IsNewSkillChannel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsVideoRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsVideoRequired"].Value = irfDetails.IsVideoRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechRubricType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@TechRubricType"].Value = irfDetails.TechRubricType;
                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@Surcharge", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@Surcharge"].Value = irfDetails.Surcharge;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewDuration", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@InterviewDuration"].Value = irfDetails.InterviewDuration;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsFixedInterviewduration", SqlDbType.Bit));
                    //cmdUpdateIRF.Parameters["@IsFixedInterviewduration"].Value = irfDetails.IsFixedInterviewduration;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@FixedAmount", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@FixedAmount"].Value = irfDetails.IsFixedInterviewduration ? irfDetails.FixedAmount : null;

                    bool isSMEChanged = false;


                    //SqlParameter isSMEChangedParam = new SqlParameter();
                    //isSMEChangedParam.ParameterName = "@isSMEChanged
                    //isSMEChangedParam.DbType = DbType.Boolean;
                    //isSMEChangedParam.Direction = ParameterDirection.Output;
                    //cmdUpdateIRF.Parameters.Add(isSMEChangedParam);



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


                    if (joinabilityDt != null && joinabilityDt.Rows.Count > 0)
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
                        if (irfDetails.IsNewSkillChannel)
                        {
                            cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerSurchargeBulkData", SqlDbType.Structured));
                            cmdUpdateIRF.Parameters["@InterviewerSurchargeBulkData"].Value = interviewerDt;

                        }
                        else
                        {
                            cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                            cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;

                        }
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
                    //if (qualityControllerDt != null && qualityControllerDt.Rows.Count > 0)
                    //{
                    //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@QCBulkData", SqlDbType.Structured));
                    //    cmdUpdateIRF.Parameters["@QCBulkData"].Value = qualityControllerDt;
                    //}
                    if (hRDt != null && hRDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@HRBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@HRBulkData"].Value = hRDt;
                    }
                    if (smeDt != null && smeDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@SMEBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@SMEBulkData"].Value = smeDt;
                    }
                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }
                    //isSMEChanged = Convert.ToBoolean(cmdUpdateIRF?.Parameters["@isSMEChanged"]?.Value);
                    //if (removedTechSkills != null)
                    //{
                    //    foreach (var removedTechSkill in removedTechSkills)
                    //    {
                    //        try
                    //        {
                    //            UpdateTechSkillQuestionsForRemovedSkills(removedTechSkill);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    //            continue;
                    //        }
                    //    }
                    //}

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
                    return new Tuple<bool, bool>(true, isSMEChanged);
                    //return true;
                    //return Tuple(<true, isSMEChanged>);
                }

            }

            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new Tuple<bool, bool>(false, false);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return new Tuple<bool, bool>(false, false);
            }
        }

        //public async Task<bool> AddIRFInterviewers(IRFVm irfDetails)
        //{
        //    try
        //    {

        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {

        //            DataTable interviewerDt = new DataTable();

        //            if (irfDetails.InterviewerCollectionPopup != null && irfDetails.InterviewerCollectionPopup.Count() != 0 && !(irfDetails.InterviewerCollectionPopup.Where(x => x.UserId == 0).Any()))
        //            {
        //                interviewerDt.Columns.Add("IRFId", typeof(int));
        //                interviewerDt.Columns.Add("UserId", typeof(int));
        //                interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
        //                if (irfDetails.IsNewSkillChannel)
        //                {
        //                    interviewerDt.Columns.Add("Surcharge", typeof(int));

        //                }
        //                interviewerDt.Columns.Add("IsActive", typeof(bool));
        //                interviewerDt.Columns.Add("IsAvailable", typeof(bool));
        //                interviewerDt.Columns.Add("Source", typeof(string));
        //                interviewerDt.Columns.Add("IsCampaignActivity", typeof(bool));
        //                interviewerDt.Columns.Add("CampaignId", typeof(int));
        //                interviewerDt.Columns.Add("OpenAIScreeningResponse", typeof(string));
        //                interviewerDt.Columns.Add("AIVerdict", typeof(string));
        //                interviewerDt.Columns.Add("IsSurchargeApprove", typeof(bool));

        //                foreach (var item in irfDetails.InterviewerCollectionPopup)
        //                {
        //                    if (irfDetails.IsNewSkillChannel)
        //                    {
        //                        interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.Surcharge,
        //                            false, true, irfDetails.LoggedEmail, item.IsCampaignActivity, item.CampaignId,
        //                            item.OpenAIScreeningResponse, item.AIVerdict, item.IsSurchargeApprove);
        //                    }
        //                    else
        //                    {
        //                        interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, false,
        //                            true, irfDetails.LoggedEmail, item.IsCampaignActivity, item.CampaignId,
        //                            item.OpenAIScreeningResponse, item.AIVerdict, item.IsSurchargeApprove);
        //                    }
        //                }
        //            }

        //            SqlCommand cmdUpdateIRF = new SqlCommand("usp_AddIRFInterviewers", con);
        //            cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

        //            cmdUpdateIRF.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
        //            cmdUpdateIRF.Parameters["@id"].Value = irfDetails.id;

        //            cmdUpdateIRF.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
        //            cmdUpdateIRF.Parameters["@timeNow"].Value = DateTime.Now;

        //            cmdUpdateIRF.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
        //            cmdUpdateIRF.Parameters["@PerformedBy"].Value = irfDetails.LoggedEmail;

        //            cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsNewSkillChannel", SqlDbType.Bit));
        //            cmdUpdateIRF.Parameters["@IsNewSkillChannel"].Value = irfDetails.IsNewSkillChannel;

        //            if (interviewerDt != null && interviewerDt.Rows.Count > 0)
        //            {
        //                if (irfDetails.IsNewSkillChannel)
        //                {
        //                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerSurchargeBulkData", SqlDbType.Structured));
        //                    cmdUpdateIRF.Parameters["@InterviewerSurchargeBulkData"].Value = interviewerDt;

        //                }
        //                else
        //                {
        //                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
        //                    cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;

        //                }
        //            }

        //            con.Open();

        //            if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
        //            {
        //                con.Close();
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
        public async Task<bool> AddIRFInterviewersV2(List<TransInterviewer> interviewers, string loggedInUser)//IList<IRFInterviewerMembers>
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {

                    DataTable interviewerDt = new DataTable();

                    if (interviewers != null && interviewers.Count() != 0 && !(interviewers.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("Surcharge", typeof(int));

                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));
                        interviewerDt.Columns.Add("Source", typeof(string));
                        interviewerDt.Columns.Add("IsCampaignActivity", typeof(bool));
                        interviewerDt.Columns.Add("CampaignId", typeof(int));
                        interviewerDt.Columns.Add("OpenAIScreeningResponse", typeof(string));
                        interviewerDt.Columns.Add("AIVerdict", typeof(string));
                        interviewerDt.Columns.Add("IsSurchargeApprove", typeof(bool));

                        foreach (var item in interviewers?.Distinct())
                        {

                            interviewerDt.Rows.Add(item.IRFId, item.UserId, item.PerSuccessRate,
                                item.Surcharge, false, true, item.Source, false, 0,
                                null, null, false);

                        }
                    }

                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_AddIRFInterviewersV2", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@id"].Value = irfDetails.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@timeNow"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@PerformedBy"].Value = loggedInUser;


                    if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerSurchargeBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@InterviewerSurchargeBulkData"].Value = interviewerDt;
                        //if (irfDetails.IsNewSkillChannel)
                        //{
                        //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerSurchargeBulkData", SqlDbType.Structured));
                        //    cmdUpdateIRF.Parameters["@InterviewerSurchargeBulkData"].Value = interviewerDt;

                        //}
                        //else
                        //{
                        //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                        //    cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;

                        //}
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
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
            try
            {
                DynamicParameters dynamicParam = new DynamicParameters();
                dynamicParam.Add("@IRFId", removedTechSkill.IRFId);
                //dynamicParam.Add("@SkillId", removedTechSkill.SkillId);
                dynamicParam.Add("@SkillName", removedTechSkill.SkillName);
                using (SqlConnection conChange = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(conChange, "usp_UpdateTechSkillQuestionsForRemovedTechSkill", dynamicParam,
                       commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return;
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
        public async Task<IEnumerable<TransSkill>> GetSkillsbyIRFid(int id)
        {
            try
            {
                var query = "Select tts.*, IRF.TechRubricType from TransTechSkill tts join IRF on irf.id = tts.IRFId where tts.IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<TransSkill>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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

        public async Task<IEnumerable<TransInterviewer>> GetTransInterviewer(int id)
        {
            try
            {
                DynamicParameters param = new DynamicParameters();
                param.Add("@irfId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<TransInterviewer>(con, "usp_GetTransInterviewer", param,
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


        public IEnumerable<TransClientUser> GetTransClientUser(int id, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
                //"select TransClientUser.*, UserList.EmailId from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id where IRFId = @IRFId";
                var query = "select a.*, ClientRole.[Name] as [Role] from (select TransClientUser.*, UserList.EmailId,UserList.uniqueid,IRF.IRFId as IRFName,UserList.FirstName from TransClientUser inner join UserList on TransClientUser.ClientUserId = UserList.id join IRF on IRF.id=TransClientUser.IRFId) as a inner join ClientRole on ClientRole.id =a.ClientRoleId where IRFId =  @IRFId";
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
                    techSkillCommentsDt.Columns.Add("SkillName", typeof(string));

                    foreach (var item in obj.SkillCollection?.Where(x => !string.IsNullOrEmpty(x.SkillName)))
                    {
                        techSkillCommentsDt.Rows.Add(obj.IRFId, 0, item.Comment?.Replace("\r", "")
                                        .Replace("\n", "")
                                        .Replace("\t", "")
                                        .Trim(), item.UserId, item.SkillName);
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
                    var result = SqlMapper.Query<SkillComment>(con, "usp_GetTechSkillComments", dynamicParameters,
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
            //parameters.Add("@IsQCRequired", entity.IsQCRequired);
            parameters.Add("@IsJoinabilitySelectabilityRequired", entity.IsJoinabilitySelectabilityRequired);
            parameters.Add("@IsGeneralSkillRequired", entity.IsGeneralSkillRequired);
            parameters.Add("@IsAadhaarVerificationRequired", entity.IsAadhaarVerificationRequired);
            parameters.Add("@IsCandidateavailability", entity.IsCandidateavailability);
            parameters.Add("@IsMeetingAutoCancelled", entity.IsMeetingAutoCancelled);
            parameters.Add("@IsMeetingNotificationSend", entity.IsMeetingNotificationSend);
            parameters.Add("@CreatedBy", entity.CreatedBy);
            parameters.Add("@SkillChannelSME", entity.SkillChannelSMEs.Where(x => x.IsSelectedForIRF)?.FirstOrDefault()?.InterviewerProfileId);
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
                var query = "select * from ClientUser where ClientId = @ClientId and RoleId!=9";
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
                    var results = await SqlMapper.QueryAsync<int>(con, "usp_AddUpdateInterviewPlanner", dynamicParameters,
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
                    await SqlMapper.QueryAsync<string>(
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

                            await SqlMapper.QueryAsync<string>(con, "usp_AddCandidate", parameters, commandType: CommandType.StoredProcedure);

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
                dynamicParameters.Add("@PreReportLink", obj.PreReportLink);
                dynamicParameters.Add("@PreReportUsername", obj.PreReportUsername);
                dynamicParameters.Add("@PreReportPassword", obj.PreReportPassword);
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
                    await SqlMapper.QueryAsync<string>(
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

        public int GetUserId(string emailId, string profile = null, bool isStaging = false)
        {
            int result = 0;
            if (isStaging)
            {
                connectionString = connectionStringForTesting;
            }
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


        public async Task<IEnumerable<MeetingVM>> UpdateMeetingStatus(string IRFId, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", IRFId);
                    dynamicParameters.Add("@date", DateTime.Now);
                    dynamicParameters.Add("@emailid", emailid);
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
                var query = "select id, EmailId from UserList where idProfile in (31, 32) and isactive=1 and EmailId != 'shivesh@risebird.io'";
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
                        var obj = await SqlMapper.QueryAsync<int>(con, "usp_AddCuratedGroup", parameters,
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
                        if (await cmdCuratedIRF.ExecuteNonQueryAsync() != 0)
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
                        if (await cmdCuratedIRF.ExecuteNonQueryAsync() != 0)
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
                        if (await cmdCuratedUsers.ExecuteNonQueryAsync() != 0)
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
                var resultset = string.Empty;
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
                            if (string.IsNullOrEmpty(resultset))
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
                var Skillset = string.Empty;
                for (int i = 0; i < Skills.Count(); i++)
                {
                    var query = "Select Id from skill where Name=@Skill";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@Skill", Skills[i].Trim());

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var results = con.Query<string>(query, dynamicParameters);
                        var result = results.FirstOrDefault();
                        if (string.IsNullOrEmpty(Skillset))
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
                    parameters.Add("@SeniorityLevel", _skilldetails.SeniorityLevel);
                    parameters.Add("@EmploymentType", _skilldetails.EmploymentType);
                    parameters.Add("@AssessmentVerificationType", _skilldetails.AssessmentVerificationType);
                    parameters.Add("@CodeLanguage", _skilldetails.CodeLanguage);
                    parameters.Add("@MCQSetsValue", _skilldetails.MCQSetsValue);
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


        public async Task<bool> AddCandidateStatus(CandidateStatus model, bool isStaging = false)
        {
            try
            {
                if (isStaging)
                {
                    connectionString = connectionStringForTesting;
                }
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
                parameters.Add("@candidateEmailId", model.CandidateEmailId);
                parameters.Add("@IRFName", model.IRFName);

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
                    var result = await SqlMapper.QueryAsync<int>(con, "usp_DeleteCandidateStatus", parameters,// this procedure has been temporarily dropped from production on 15th April, 2024
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
                    var result = await con.QueryAsync<string>(query, dynamicParameters);
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
        public async Task<DataTable> AddQuickMultipleCandidates(List<CandidateQuickAddModel> candidateList, int? userId = null, bool isResumeParser = false, bool isApi = false, string ApiRequestedBy = null, string jobId = null, bool isStaging = false)
        {
            if (isStaging)
            {
                connectionString = connectionStringForTesting;
            }
            DataTable dt = new DataTable();
            if (isResumeParser)
            {
                dt.Columns.AddRange(new DataColumn[3] { new DataColumn("EmailId", typeof(string)),
                    new DataColumn("Import Status", typeof(string)), new DataColumn("CandidateResumeId", typeof(int)) });
            }
            else if (isApi)
            {
                dt.Columns.AddRange(new DataColumn[3] { new DataColumn("EmailId", typeof(string)),
                    new DataColumn("Import Status", typeof(string)), new DataColumn("IRFId", typeof(int)) });
            }
            else
            {
                dt.Columns.AddRange(new DataColumn[2] { new DataColumn("EmailId", typeof(string)),
                    new DataColumn("Import Status", typeof(string)) });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    if (candidateList != null && candidateList.Count() > 0)
                    {
                        foreach (var obj in candidateList)
                        {
                            DynamicParameters parameters = new DynamicParameters();
                            if (!isApi)
                            {
                                parameters.Add("@IRFId", obj.IRFId);

                            }
                            else
                            {
                                parameters.Add("@jobId", jobId);
                            }

                            parameters.Add("@countryCode", obj.countryCode);

                            parameters.Add("@Name", obj.Name);
                            parameters.Add("@EmailId", obj.EmailId);
                            parameters.Add("@ResumeLink", obj.ResumeLink);
                            parameters.Add("@Phone", obj.Phone);
                            if (userId.HasValue)
                            {
                                parameters.Add("@AddedByUserId", userId);

                            }
                            else
                            {
                                parameters.Add("@AddedByUserId", obj.AddedByUserId);

                            }
                            parameters.Add("@date", DateTime.Now);
                            if (isApi && !string.IsNullOrEmpty(ApiRequestedBy))
                            {
                                parameters.Add("@ClientHREmail", ApiRequestedBy);
                                parameters.Add("@ProfileSource", ApiRequestedBy);
                                parameters.Add("@WhatsappNumber", obj.Phone);
                            }
                            else
                            {
                                parameters.Add("@ClientHREmail", obj.ClientHREmail);
                                parameters.Add("@ProfileSource", obj.ProfileSource);
                                parameters.Add("@WhatsappNumber", obj.WhatsappNumber);
                            }
                            parameters.Add("@AffindaResumeIdentifier", obj.AffindaResumeIdentifier);
                            parameters.Add("@operationStatus", dbType: DbType.String, direction: ParameterDirection.Output, size: 5215585);


                            await SqlMapper.QueryAsync(con, "usp_AddQuickCandidate", parameters, commandType: CommandType.StoredProcedure);

                            var result = parameters.Get<string>("@operationStatus");
                            if (isResumeParser)
                            {
                                dt.Rows.Add(obj.EmailId, result, obj.CandidateResumeId);

                            }
                            else if (isApi)
                            {
                                var resultArr = result.Split("|");
                                var irfId = Convert.ToInt32(resultArr?.Last());
                                dt.Rows.Add(obj.EmailId, resultArr[0], irfId);

                            }
                            else
                            {
                                dt.Rows.Add(obj.EmailId, result);

                            }

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
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFCandidateStats", parameters, commandTimeout: 600, //usp_GetAllIRFDetails
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
                var query = "Select IRF.id,IRF.IRFId, irf.IsNewSkillChannel, ag.AssessmentGroup, ag.id as AssessmentGroupId, irf.CreatedFromTemplateId from IRF left join AssessmentGroup ag on ag.id=IRF.AssessmentGroupId order by IRF.id desc";
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
        public async Task<bool> UpdateCancellationSource(int interviewid, string emailId, string cancellationreason)
        {
            try
            {
                var query = "Update InterviewMeetings set Cancellationsource=@Cancellationsource,cancellationreason=@Cancellationreason where id=@meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", interviewid);
                dynamicParameters.Add("@Cancellationsource", emailId);
                dynamicParameters.Add("@Cancellationreason", cancellationreason);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await con.QueryAsync<SkillCahnnelDetailsVM>(query, dynamicParameters);
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
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAssessmentGroupInterviewerMembers(int Assessmentid, bool isMinimalData = false, bool? isTestDb = false)
        {
            try
            {
                if (isTestDb.HasValue && isTestDb.Value)
                {
                    string conStringTest = "Server=tcp:risebirdtest.database.windows.net,1433;Initial Catalog=productTestProdDB;Persist Security Info=False;User ID=risebirdtestadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                    connectionString = conStringTest;
                }
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    if (isMinimalData)
                    {
                        return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_GetAssessmentGroupInterviewerMembersForIRFDropDownMinimalData", parameters,
                         commandType: CommandType.StoredProcedure);
                    }
                    else
                    {
                        return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_GetAssessmentGroupInterviewerMembers", parameters,
                         commandType: CommandType.StoredProcedure);
                    }

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

        public IEnumerable<TransSkill> GetClonableTechSkillsbyId(int id)
        {
            try
            {
                var query = "Select tcits.*, lt.[Name] as LevelName from TransClonableIRFTechSkill tcits join LevelTech lt on lt.id = tcits.LevelId where tcits.ClonableIRFId = @id";
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
                var query = "select ClonableIRFId, Skill as SkillId, Question, UserId from ClonableTemplateQuestions where ClonableIRFId = @id";
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

        public async Task<Tuple<bool, int>> CreateSkillChannelFromTemplate(IRFVm irfDetails, string loggedInUser = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_CreateSkillChannelFromTemplate", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

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
                    cmdUpdateIRF.Parameters["@JDLink"].Value = irfDetails.JDLink;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfPosition", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfPosition"].Value = irfDetails.NoOfPosition;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfIntterview", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfIntterview"].Value = irfDetails.NoOfIntterview;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RisebirdHr", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RisebirdHr"].Value = irfDetails.RisebirdHr;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PerMintuteRate", SqlDbType.Real));
                    cmdUpdateIRF.Parameters["@PerMintuteRate"].Value = irfDetails.PerMintuteRate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JobLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@JobLevel"].Value = irfDetails.JobLevel.HasValue ? irfDetails.JobLevel.Value : (int)JobLevels.SE;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CalendlyLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CalendlyLink"].Value = irfDetails.CalendlyLink ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TotalBudget", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@TotalBudget"].Value = irfDetails.TotalBudget;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsBiometricEnabled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsBiometricEnabled"].Value = irfDetails.IsBiometricEnabled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PriorityLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@PriorityLevel"].Value = irfDetails.PriorityLevel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyDetails", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyDetails"].Value = irfDetails.CompanyDetails ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Type"].Value = irfDetails.Type ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Heuristicsscore", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@Heuristicsscore"].Value = irfDetails.Heuristicsscore;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PrimarySkillInvite", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@PrimarySkillInvite"].Value = irfDetails.PrimarySkillInvite ?? string.Empty;

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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedFromTemplateId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@CreatedFromTemplateId"].Value = irfDetails.CreatedFromTemplateId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@CreatedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@MeetingPlatform"].Value = irfDetails.MeetingPlatform ?? "Zoom";

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = irfDetails.IsAadhaarVerificationRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsCandidateavailability", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsCandidateavailability"].Value = irfDetails.IsCandidateavailability;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingAutoCancelled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingAutoCancelled"].Value = irfDetails.IsMeetingAutoCancelled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingNotificationSend", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingNotificationSend"].Value = irfDetails.IsMeetingNotificationSend;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsNewSkillChannel", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsNewSkillChannel"].Value = true;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RateComputedForClient", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RateComputedForClient"].Value = irfDetails.RateComputedForClient;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMarketPlaceChannel", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMarketPlaceChannel"].Value = irfDetails.RateComputedForClient > 0 ? irfDetails.IsMarketPlaceChannel : null;

                    SqlParameter flatRateParam = new SqlParameter();
                    flatRateParam.ParameterName = "@flatRate";
                    flatRateParam.DbType = DbType.Int16;
                    flatRateParam.Direction = ParameterDirection.Output;
                    cmdUpdateIRF.Parameters.Add(flatRateParam);

                    int flatRate = 0;

                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));
                    techSkillDt.Columns.Add("isEliminationskill", typeof(bool));

                    foreach (var item in irfDetails.SkillCollection)
                    {
                        if (string.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, "", "", 0, item.isEliminationskill);
                    }

                    DataTable clientUserDt = new DataTable();
                    if (irfDetails.ClientCollection != null && irfDetails.ClientCollection.Count() > 0)
                    {
                        clientUserDt.Columns.Add("IRFId", typeof(int));
                        clientUserDt.Columns.Add("ClientUserId", typeof(int));

                        foreach (var item in irfDetails.ClientCollection)
                        {
                            clientUserDt.Rows.Add(irfDetails.id, item.ClientUserId);
                        }
                    }


                    //    SqlCommand cmdClientUser = new SqlCommand("usp_ClientUserBulkInsert", con);
                    //    cmdClientUser.CommandType = CommandType.StoredProcedure;
                    //    cmdClientUser.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                    //    cmdClientUser.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    //    con.Open();
                    //    if (cmdClientUser.ExecuteNonQuery() != 0)
                    //    {
                    //        con.Close();
                    //    }


                    //DataTable interviewerDt = new DataTable();

                    //if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    //{
                    //    interviewerDt.Columns.Add("IRFId", typeof(int));
                    //    interviewerDt.Columns.Add("UserId", typeof(int));
                    //    interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                    //    interviewerDt.Columns.Add("Surcharge", typeof(int));
                    //    interviewerDt.Columns.Add("IsActive", typeof(bool));
                    //    interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                    //    foreach (var item in irfDetails.InterviewerCollection)
                    //    {
                    //        //if (irfDetails.JobCountry == "2")
                    //        //{
                    //        //    interviewerDt.Rows.Add(irfDetails.id, item.UserId, 1500, true, false);
                    //        //}
                    //        //else
                    //        //{
                    //        //    interviewerDt.Rows.Add(irfDetails.id, item.UserId, 1000, true, false);
                    //        //}
                    //        interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.Surcharge, item.IsActive, item.IsAvailable);

                    //    }
                    //}

                    if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    }
                    //if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    //{
                    //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                    //    cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    //}
                    if (clientUserDt != null && clientUserDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    flatRate = Convert.ToInt16(cmdUpdateIRF?.Parameters["@flatRate"]?.Value);

                    return new Tuple<bool, int>(true, flatRate);
                }
            }

            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new Tuple<bool, int>(false, 0);
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                //return false;
                return new Tuple<bool, int>(false, 0);

            }
        }


        public async Task<Tuple<bool, int>> CreateSkillChannelFromTemplateV1(IRFVm irfDetails, string loggedInUser = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_CreateSkillChannelFromTemplateV1", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

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
                    cmdUpdateIRF.Parameters["@JobLevel"].Value = irfDetails.JobLevel.HasValue ? irfDetails.JobLevel.Value : (int)JobLevels.SE;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CalendlyLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CalendlyLink"].Value = irfDetails.CalendlyLink ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TotalBudget", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@TotalBudget"].Value = irfDetails.TotalBudget;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsBiometricEnabled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsBiometricEnabled"].Value = irfDetails.IsBiometricEnabled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PriorityLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@PriorityLevel"].Value = irfDetails.PriorityLevel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyDetails", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyDetails"].Value = irfDetails.CompanyDetails ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Type"].Value = irfDetails.Type ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Heuristicsscore", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@Heuristicsscore"].Value = irfDetails.Heuristicsscore;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PrimarySkillInvite", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@PrimarySkillInvite"].Value = irfDetails.PrimarySkillInvite ?? string.Empty;

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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedFromTemplateId", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@CreatedFromTemplateId"].Value = irfDetails.CreatedFromTemplateId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@CreatedOn"].Value = DateTime.Now;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@MeetingPlatform", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@MeetingPlatform"].Value = irfDetails.MeetingPlatform ?? "Zoom";

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = irfDetails.IsAadhaarVerificationRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsCandidateavailability", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsCandidateavailability"].Value = irfDetails.IsCandidateavailability;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingAutoCancelled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingAutoCancelled"].Value = irfDetails.IsMeetingAutoCancelled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingNotificationSend", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingNotificationSend"].Value = true;//irfDetails.IsMeetingNotificationSend;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsNewSkillChannel", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsNewSkillChannel"].Value = true;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsVideoRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsVideoRequired"].Value = irfDetails.IsVideoRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechRubricType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@TechRubricType"].Value = TechRubricTypes.Advanced;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RequestId", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@RequestId"].Value = irfDetails.RequestId;

                    SqlParameter flatRateParam = new SqlParameter();
                    flatRateParam.ParameterName = "@flatRate";
                    flatRateParam.DbType = DbType.Int16;
                    flatRateParam.Direction = ParameterDirection.Output;
                    cmdUpdateIRF.Parameters.Add(flatRateParam);

                    int flatRate = 0;

                    //DataTable techSkillDt = new DataTable();
                    //techSkillDt.Columns.Add("IRFId", typeof(int));
                    //techSkillDt.Columns.Add("LevelId", typeof(int));
                    //techSkillDt.Columns.Add("SkillName", typeof(string));
                    //techSkillDt.Columns.Add("Weightage", typeof(int));
                    //techSkillDt.Columns.Add("Concepts", typeof(string));
                    //techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    //techSkillDt.Columns.Add("Timelimit", typeof(int));
                    //techSkillDt.Columns.Add("isEliminationskill", typeof(bool));

                    //foreach (var item in irfDetails.SkillCollection)
                    //{
                    //    if (string.IsNullOrEmpty(item.Concepts))
                    //    {
                    //        item.Concepts = "";
                    //    }
                    //    if (string.IsNullOrEmpty(item.Natureofquestion))
                    //    {
                    //        item.Natureofquestion = "";
                    //    }
                    //    techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillName, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill);
                    //}

                    DataTable clientUserDt = new DataTable();
                    if (irfDetails.ClientCollection != null && irfDetails.ClientCollection.Count() > 0)
                    {
                        clientUserDt.Columns.Add("IRFId", typeof(int));
                        clientUserDt.Columns.Add("ClientUserId", typeof(int));

                        foreach (var item in irfDetails.ClientCollection)
                        {
                            clientUserDt.Rows.Add(irfDetails.id, item.ClientUserId);
                        }
                    }


                    //DataTable interviewerDt = new DataTable();

                    //if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    //{
                    //    interviewerDt.Columns.Add("IRFId", typeof(int));
                    //    interviewerDt.Columns.Add("UserId", typeof(int));
                    //    interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                    //    interviewerDt.Columns.Add("Surcharge", typeof(int));
                    //    interviewerDt.Columns.Add("IsActive", typeof(bool));
                    //    interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                    //    foreach (var item in irfDetails.InterviewerCollection)
                    //    {
                    //        //if (irfDetails.JobCountry == "2")
                    //        //{
                    //        //    interviewerDt.Rows.Add(irfDetails.id, item.UserId, 1500, true, false);
                    //        //}
                    //        //else
                    //        //{
                    //        //    interviewerDt.Rows.Add(irfDetails.id, item.UserId, 1000, true, false);
                    //        //}
                    //        interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.Surcharge, item.IsActive, item.IsAvailable);

                    //    }
                    //}

                    //if (techSkillDt != null && techSkillDt.Rows.Count > 0)
                    //{
                    //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                    //    cmdUpdateIRF.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                    //}
                    //if (interviewerDt != null && interviewerDt.Rows.Count > 0)
                    //{
                    //    cmdUpdateIRF.Parameters.Add(new SqlParameter("@InterviewerBulkData", SqlDbType.Structured));
                    //    cmdUpdateIRF.Parameters["@InterviewerBulkData"].Value = interviewerDt;
                    //}
                    if (clientUserDt != null && clientUserDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    flatRate = Convert.ToInt16(cmdUpdateIRF?.Parameters["@flatRate"]?.Value);

                    return new Tuple<bool, int>(true, flatRate);
                }
            }

            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new Tuple<bool, int>(false, 0);
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; TemplateName : {irfDetails.TemplateName}; userEmailId : {loggedInUser}");

                //await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                //return false;
                return new Tuple<bool, int>(false, 0);

            }
        }

        public async Task<AssignmentVM> GetAssessmentGroupDetails(int assessmentGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@assessmentGroupId", assessmentGroupId);
                    var result = await SqlMapper.QueryAsync<AssignmentVM>(con, "usp_GetAssessmentGroupDetails", parameters,
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
            return null;
        }

        public async Task<string> GetAssessmentGroupFromIRFId(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetAssessmentGroupFromIRFId", parameters,
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
            return null;
        }

        public async Task<string> GetAssessmentGroupById(int assessmentGroupId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@assessmentGroupId", assessmentGroupId);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetAssessmentGroupById", parameters,
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
            return null;
        }

        public async Task<bool> EditSkillChannelCreatedFromTemplate(IRFVm irfDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlCommand cmdUpdateIRF = new SqlCommand("usp_EditSkillChannelCreatedFromTemplate", con);
                    cmdUpdateIRF.CommandType = CommandType.StoredProcedure;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.NVarChar));
                    //cmdUpdateIRF.Parameters["@IRFId"].Value = irfDetails.IRFId;
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@id"].Value = irfDetails.id;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientName", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@ClientName"].Value = irfDetails.ClientName;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDTiltle", SqlDbType.NVarChar));
                    cmdUpdateIRF.Parameters["@JDTiltle"].Value = irfDetails.JDTiltle;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyType", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyType"].Value = irfDetails.CompanyType;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@ClientId"].Value = irfDetails.ClientId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JobCountry", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JobCountry"].Value = irfDetails.JobCountry;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime));
                    //cmdUpdateIRF.Parameters["@StartDate"].Value = irfDetails.StartDate;

                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime));
                    //cmdUpdateIRF.Parameters["@EndDate"].Value = irfDetails.EndDate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JDLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@JDLink"].Value = irfDetails.JDLink;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfPosition", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfPosition"].Value = irfDetails.NoOfPosition;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@NoOfIntterview", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@NoOfIntterview"].Value = irfDetails.NoOfIntterview;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@RisebirdHr", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@RisebirdHr"].Value = irfDetails.RisebirdHr;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PerMintuteRate", SqlDbType.Real));
                    cmdUpdateIRF.Parameters["@PerMintuteRate"].Value = irfDetails.PerMintuteRate;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@JobLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@JobLevel"].Value = irfDetails.JobLevel.HasValue ? irfDetails.JobLevel.Value : (int)JobLevels.SE;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CalendlyLink", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CalendlyLink"].Value = irfDetails.CalendlyLink ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@TotalBudget", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@TotalBudget"].Value = irfDetails.TotalBudget;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsBiometricEnabled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsBiometricEnabled"].Value = irfDetails.IsBiometricEnabled;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PriorityLevel", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@PriorityLevel"].Value = irfDetails.PriorityLevel;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@CompanyDetails", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@CompanyDetails"].Value = irfDetails.CompanyDetails ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Type", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@Type"].Value = irfDetails.Type ?? string.Empty;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@Heuristicsscore", SqlDbType.Int));
                    cmdUpdateIRF.Parameters["@Heuristicsscore"].Value = irfDetails.Heuristicsscore;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@PrimarySkillInvite", SqlDbType.VarChar));
                    cmdUpdateIRF.Parameters["@PrimarySkillInvite"].Value = irfDetails.PrimarySkillInvite ?? string.Empty;

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

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsAadhaarVerificationRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsAadhaarVerificationRequired"].Value = irfDetails.IsAadhaarVerificationRequired;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsCandidateavailability", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsCandidateavailability"].Value = irfDetails.IsCandidateavailability;
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingAutoCancelled", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingAutoCancelled"].Value = irfDetails.IsMeetingAutoCancelled;
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsMeetingNotificationSend", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsMeetingNotificationSend"].Value = irfDetails.IsMeetingNotificationSend;
                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@IsVideoRequired", SqlDbType.Bit));
                    cmdUpdateIRF.Parameters["@IsVideoRequired"].Value = irfDetails.IsVideoRequired;
                    //cmdUpdateIRF.Parameters.Add(new SqlParameter("@CreatedFromTemplateId", SqlDbType.Int));
                    //cmdUpdateIRF.Parameters["@CreatedFromTemplateId"].Value = irfDetails.CreatedFromTemplateId;

                    cmdUpdateIRF.Parameters.Add(new SqlParameter("@ModifiedOn", SqlDbType.DateTime));
                    cmdUpdateIRF.Parameters["@ModifiedOn"].Value = DateTime.Now;

                    DataTable techSkillDt = new DataTable();
                    techSkillDt.Columns.Add("IRFId", typeof(int));
                    techSkillDt.Columns.Add("LevelId", typeof(int));
                    techSkillDt.Columns.Add("SkillId", typeof(int));
                    techSkillDt.Columns.Add("Weightage", typeof(int));
                    techSkillDt.Columns.Add("Concepts", typeof(string));
                    techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                    techSkillDt.Columns.Add("Timelimit", typeof(int));
                    techSkillDt.Columns.Add("isEliminationskill", typeof(bool));

                    foreach (var item in irfDetails.SkillCollection)
                    {
                        if (string.IsNullOrEmpty(item.Concepts))
                        {
                            item.Concepts = "";
                        }
                        if (string.IsNullOrEmpty(item.Natureofquestion))
                        {
                            item.Natureofquestion = "";
                        }
                        techSkillDt.Rows.Add(irfDetails.id, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill);
                    }

                    DataTable clientUserDt = new DataTable();
                    if (irfDetails.ClientCollection != null && irfDetails.ClientCollection.Count() > 0)
                    {
                        clientUserDt.Columns.Add("IRFId", typeof(int));
                        clientUserDt.Columns.Add("ClientUserId", typeof(int));

                        foreach (var item in irfDetails.ClientCollection)
                        {
                            clientUserDt.Rows.Add(irfDetails.id, item.ClientUserId);
                        }
                    }


                    //    SqlCommand cmdClientUser = new SqlCommand("usp_ClientUserBulkInsert", con);
                    //    cmdClientUser.CommandType = CommandType.StoredProcedure;
                    //    cmdClientUser.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                    //    cmdClientUser.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    //    con.Open();
                    //    if (cmdClientUser.ExecuteNonQuery() != 0)
                    //    {
                    //        con.Close();
                    //    }


                    DataTable interviewerDt = new DataTable();

                    if (irfDetails.InterviewerCollection != null && irfDetails.InterviewerCollection.Count() != 0 && !(irfDetails.InterviewerCollection.Where(x => x.UserId == 0).Any()))
                    {
                        interviewerDt.Columns.Add("IRFId", typeof(int));
                        interviewerDt.Columns.Add("UserId", typeof(int));
                        interviewerDt.Columns.Add("PerSuccessRate", typeof(int));
                        interviewerDt.Columns.Add("IsActive", typeof(bool));
                        interviewerDt.Columns.Add("IsAvailable", typeof(bool));

                        foreach (var item in irfDetails.InterviewerCollection?.Distinct())
                        {
                            interviewerDt.Rows.Add(irfDetails.id, item.UserId, item.PerSuccessRate, item.IsActive, true);
                        }
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
                    if (clientUserDt != null && clientUserDt.Rows.Count > 0)
                    {
                        cmdUpdateIRF.Parameters.Add(new SqlParameter("@ClientUserBulkData", SqlDbType.Structured));
                        cmdUpdateIRF.Parameters["@ClientUserBulkData"].Value = clientUserDt;
                    }

                    con.Open();

                    if (await cmdUpdateIRF.ExecuteNonQueryAsync() != 0)
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
        public int GetIRFIdByIRFName(string IRFId)
        {
            try
            {
                var query = "Select id from IRF where IRFId=@IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public IEnumerable<TransInterviewer> GetIRFInterviewerSkillsByAssessmentGroup(string assessmentgroupid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DynamicParameters dp = new DynamicParameters();
                    dp.Add("@assessmentgroupid", assessmentgroupid);
                    return con.Query<TransInterviewer>(";With CTE AS(Select * from InterviewerProfile cross Apply string_split(AssessmentGroupId,',')) Select Distinct CTE.UserId,CTE.AssessmentGroupId, ul.EmailId,CTE.NickName  From CTE join Userlist ul on CTE.Userid=ul.id  where CTE.value=@assessmentgroupid", dp);
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<string>> GetIRFInterviewersAndEmergencyInterviewersEmail(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    //parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<string>(con, "usp_GetIRFInterviewersAndEmergencyInterviewersEmail", parameters,
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
        public async Task<IEnumerable<IRF>> GetAllIRFDetailsIRF(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@profile", profile);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFCandidateStatsIRFCount", parameters, commandTimeout: 600,
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
        public async Task<bool> UpdateIntervieweravailbilityDetails(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    parameters.Add("@timeNow", DateTime.Now);
                    await SqlMapper.QueryAsync(con, "usp_UpdateIntervieweravailbilityDetails", parameters,
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
        public IEnumerable<ClonableSkillChannelVM> GetClonableTempStatsbyId(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", id);
                    var result = SqlMapper.Query<ClonableSkillChannelVM>(con, "usp_FetchClonableTempStatsDetailsbyId", parameters,
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
        public async Task<IEnumerable<InterviewDetailsVM>> FetchIRFCompletedInterviewDetails(int id, int type, DateTime startDate, DateTime endDate)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@startDate", startDate);
                parameters.Add("@endDate", endDate);
                parameters.Add("@timeNow", DateTime.Now);
                parameters.Add("@id", id);
                parameters.Add("@type", type);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewDetailsVM>(con, "usp_GetIRFCompletedInterviewDetails", parameters,
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
        public async Task<InterviewDetailsVM> FetchIRFInterviewerDetailsById(int id)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<InterviewDetailsVM>(
                        con, "Select IRF.IRFId,cl.Name as ClientName,ul.EmailId as ClientHRAttemptContact From IRF join Client cl on IRF.ClientId=cl.id join UserList ul on ul.id=IRF.RisebirdHr where IRF.id=@id", parameters, commandType: Text);
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
        public async Task<IEnumerable<string>> FetchSupportAdminEmailid()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(
                        con, "Select EmailId From UserList where idProfile=32", null, commandType: Text);
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
        public async Task<IEnumerable<ClientInterviewMapped>> GetCompletedInterviewCountMappedClient(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    return await SqlMapper.QueryAsync<ClientInterviewMapped>(con, "dbo.usp_GetInterviewCountClientMapped", parameters,
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
        public bool EditIRFConfiguration(int irfId)
        {
            try
            {
                var query = "Select Case When IsCandidateavailability is null and IsMeetingNotificationSend is null then 0 Else 1 END from IRF where IRF.id =@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", irfId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<bool>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetInterviewerAssessmentGroups(string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_FetchInterviewerAssessmentGroupDetails", parameters,
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

        public async Task<IEnumerable<AssessmentGroupInterviewers>> GetAssessmentGroupInterviewerMembersForSME(int Assessmentid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    return await SqlMapper.QueryAsync<AssessmentGroupInterviewers>(con, "usp_GetAssessmentGroupInterviewerMembersForSME", parameters,
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

        public async Task<IEnumerable<SkillChannelSME>> GetEligibleSMEForSkillChannel(int Assessmentid, int? irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Assessmentid", Assessmentid);
                    parameters.Add("@irfId", irfId);
                    return await SqlMapper.QueryAsync<SkillChannelSME>(con, "usp_GetAssessmentGroupInterviewerMembersForSME", parameters,
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

        public async Task<bool> AddUpdateAssessmentGroupSMEInterviewer(IList<AssessmentGroupInterviewers> interviewers)
        {
            string currentUser = string.Empty;
            try
            {
                if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null)
                {
                    currentUser = _httpContextAccessor.HttpContext.Session.GetString("email")?.Trim();
                    currentUser = currentUser?.Replace("\"", "");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {


                    SqlCommand cmd = new SqlCommand("usp_AddUpdateAssessmentGroupSMEInterviewer", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmd.Parameters["@AssessmentGroupId"].Value = interviewers?.FirstOrDefault().AssessmentGroupId;

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("AssessmentGroupId", typeof(int));
                    dataTable.Columns.Add("InterviewerProfileId", typeof(int));
                    dataTable.Columns.Add("IsSME", typeof(bool));
                    dataTable.Columns.Add("CreatedBy", typeof(string));
                    dataTable.Columns.Add("CreatedOn", typeof(string));
                    dataTable.Columns.Add("ModifiedBy", typeof(string));
                    dataTable.Columns.Add("ModifiedOn", typeof(string));


                    foreach (var item in interviewers)
                    {
                        dataTable.Rows.Add(item.AssessmentGroupId, item.InterviewerProfileId, item.IsSME, currentUser, DateTime.Now, null, null);
                    }

                    if (dataTable != null && dataTable.Rows.Count > 0)
                    {
                        cmd.Parameters.Add(new SqlParameter("@SMEBulkData", SqlDbType.Structured));
                        cmd.Parameters["@SMEBulkData"].Value = dataTable;
                    }

                    con.Open();

                    if (await cmd.ExecuteNonQueryAsync() != 0)
                    {
                        con.Close();
                    }

                    return true;
                }
            }

            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; userEmailId : {currentUser}");

                return false;
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}; userEmailId : {currentUser}");

                return false;
            }
        }
        public async Task<IEnumerable<IRF>> GetAllNewCreatedIRFs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRF>(con, "usp_GetAllNewCreatedIRFs", parameters,
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
        public async Task<IEnumerable<AssessmentGroupInterviewerDetails>> GetAvailableAssessmentGroupInterviewerMembers(int assessmentGroupId, int? irfId = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@assessmentGroupId", assessmentGroupId);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<AssessmentGroupInterviewerDetails>(con, "usp_GetAvailableAssessmentGroupInterviewerMembers", parameters,
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

        public async Task<IEnumerable<IRFInterviewerMembers>> GetAllIRFAssessmentInterviewerMembers(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    //parameters.Add("@timeNow", DateTime.Now);
                    return await SqlMapper.QueryAsync<IRFInterviewerMembers>(con, "usp_GetAllIRFAssessmentInterviewerMembers", parameters,
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
        public async Task<IEnumerable<IRFClientReviewVM>> GetIRFClientReviews(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    return await SqlMapper.QueryAsync<IRFClientReviewVM>(con, "usp_GetIRFClientReviews", parameters,
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
        public async Task<CommonInterviewDetailsVM> GetIRFBillingOwnerById(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IRFId", irfId);
                    var result = await SqlMapper.QueryAsync<CommonInterviewDetailsVM>(con, "dbo.usp_GetSkillChannelBillingOwnerById", parameters, commandTimeout: 600,
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
            return null;
        }
        public async Task<string> UpdateIRFRedFlags()
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync(con, "usp_UpdateIRFRedFlags", parameters, commandTimeout: 9000, commandType: CommandType.StoredProcedure);
                    return "successfully";
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return "Fail";
        }
        public async Task<IEnumerable<InterviewDetailsVM>> GetSkillChannelStatsDetails(int irfid, string type)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfid", irfid);
                    parameters.Add("@type", type);
                    return await SqlMapper.QueryAsync<InterviewDetailsVM>(con, "dbo.usp_GetIRFStatsDetails", parameters, commandTimeout: 600,
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

        public async Task<IRF> GetSkillChannelDetailsByName(string irfName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfName", irfName);
                    var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetSkillChannelDetailsByIRFName", parameters, commandTimeout: 600,
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
            return null;
        }
        public async Task<bool> UpdateTechSkills(IList<TransSkill> techskills)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        DataTable techSkillDt = new DataTable();
                        techSkillDt.Columns.Add("IRFId", typeof(int));
                        techSkillDt.Columns.Add("LevelId", typeof(int));
                        techSkillDt.Columns.Add("SkillId", typeof(int));
                        techSkillDt.Columns.Add("Weightage", typeof(int));
                        techSkillDt.Columns.Add("Concepts", typeof(string));
                        techSkillDt.Columns.Add("Natureofquestion", typeof(string));
                        techSkillDt.Columns.Add("Timelimit", typeof(int));
                        techSkillDt.Columns.Add("isEliminationskill", typeof(bool));
                        techSkillDt.Columns.Add("SkillSet", typeof(int));
                        techSkillDt.Columns.Add("SkillName", typeof(string));

                        foreach (var item in techskills)
                        {
                            techSkillDt.Rows.Add(item.IRFId, item.LevelId, item.SkillId, item.Weightage, item.Concepts, item.Natureofquestion, item.Timelimit, item.isEliminationskill, item.SkillSet);
                        }

                        SqlCommand cmdTechSkill = new SqlCommand("usp_TechSkillBulkInsert", con);
                        cmdTechSkill.CommandType = CommandType.StoredProcedure;
                        cmdTechSkill.Parameters.Add(new SqlParameter("@TechSkillBulkData", SqlDbType.Structured));
                        cmdTechSkill.Parameters["@TechSkillBulkData"].Value = techSkillDt;
                        con.Open();
                        if (cmdTechSkill.ExecuteNonQuery() != 0)
                        {
                            con.Close();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> UpdateJobLevel(int irf, int joblevel)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irf);
                        parameters.Add("@joblevel", joblevel);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_updateJobLevel", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> UpdateDeficultyLevel(int irf, int difficultylevel)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irf);
                        parameters.Add("@difficultylevel", difficultylevel);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_updatedifficultylevel", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> UpdateFormat(int irf, string format)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irf);
                        parameters.Add("@format", format);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_updateformat", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<IRFInstructionDetailsVM> GetFormatanddifficultylevel(int irf)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irf);
                        var result = await SqlMapper.QueryAsync<IRFInstructionDetailsVM>(con, "dbo.usp_GetFormatanddifficultylevel", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return result.FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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

        public async Task<IEnumerable<BroadcastModel>> GetTextInstruction(int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    var result = await SqlMapper.QueryAsync<BroadcastModel>(con, "usp_GetTextInstruction", parameters,
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
        public async Task<bool> AddIRFSampleQuestions(TechSkillComments obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable techSkillCommentsDt = new DataTable();
                    techSkillCommentsDt.Columns.Add("IRFId", typeof(int));
                    techSkillCommentsDt.Columns.Add("Skill", typeof(int));
                    techSkillCommentsDt.Columns.Add("Comment", typeof(string));
                    techSkillCommentsDt.Columns.Add("CreatedBy", typeof(string));
                    techSkillCommentsDt.Columns.Add("SkillName", typeof(string));
                    foreach (var item in obj.SkillCollection?.Where(x => !string.IsNullOrEmpty(x.SkillName)))
                    {
                        techSkillCommentsDt.Rows.Add(obj.IRFId, 0, item.Comment, item.CreatedBy, item.SkillName);
                    }
                    SqlCommand cmdAddSkillComments = new SqlCommand("usp_AddIRFSampleQuestions", con);
                    cmdAddSkillComments.CommandType = CommandType.StoredProcedure;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFSampleQuestionsBulkData", SqlDbType.Structured));
                    cmdAddSkillComments.Parameters["@IRFSampleQuestionsBulkData"].Value = techSkillCommentsDt;
                    cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFId", SqlDbType.Int));
                    cmdAddSkillComments.Parameters["@IRFId"].Value = obj.IRFId;
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
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return false;
        }
        public async Task<IEnumerable<SkillComment>> GetSkillSampleQuestions(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SkillComment>(con, "usp_GetSkillSampleQuestions", dynamicParameters,
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
        public async Task<bool> DeleteSkillChannelSampleQuestions(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@questionid", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<SkillComment>(con, "usp_DeleteSkillSampleQuestions", dynamicParameters,
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

        public async Task<bool> UpdateSkillChannelEmbeddableUrl(string irfId, string embedUrl)//embed url by Affinda
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfId ", irfId);
                        parameters.Add("@embedUrl", embedUrl);
                        parameters.Add("@timeNow", DateTime.Now);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_UpdateSkillChannelEmbeddableUrl", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> ApproveDM(int id, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfId ", id);
                        parameters.Add("@emailid", emailid);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_ApproveDMByClient", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> UpdateAffindaIndexFlag(string irfId, bool isIndexCreated)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfId ", irfId);
                        parameters.Add("@isIndexCreated ", isIndexCreated);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_UpdateAffindaIndexFlag", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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

        /// <summary>
        /// the functionality of this function was later changed to get both similar and dissimilar assessment groups, differentiated from each other by a flag
        /// </summary>
        /// <param name="searchValue"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AssignmentVM>> GetSimilarAssessmentGroups(string searchValue, bool isAlreadyMasterAG, bool? forEditMAG = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@searchValue", searchValue.Trim());
                        parameters.Add("@isAlreadyMasterAG", isAlreadyMasterAG);
                        parameters.Add("@forEditMAG", forEditMAG);
                        var result = await SqlMapper.QueryAsync<AssignmentVM>(con, "dbo.usp_GetSimilarAssessmentGroups", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<IEnumerable<IRFVm>> GetSkillChannelsForAssessmentGroups(List<int> assessmentGroups)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@AssessmentGroupsList", assessmentGroups != null ? string.Join(",", assessmentGroups) : null);
                    return await SqlMapper.QueryAsync<IRFVm>(con, "usp_GetSkillChannelsForAssessmentGroups", parameters, commandType: CommandType.StoredProcedure, commandTimeout: 300);
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
        public bool CreateCampaign(AGCampaignModel model, string loggedInUser, out int campaignId)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable agrpsDt = new DataTable();
                    agrpsDt.Columns.Add("AssessmentGroupId", typeof(int));

                    if (model.AssessmentGroups != null && model.AssessmentGroups.Count() > 0 && model.AssessmentGroups.Any(x => x.IsSelectedForCampaign))
                    {
                        foreach (var item in model.AssessmentGroups.Where(x => x.IsSelectedForCampaign))
                        {
                            agrpsDt.Rows.Add(item.id);
                        }

                    }

                    if (model.SelectedRemainingAssessmentGroups != null && model.SelectedRemainingAssessmentGroups.Count() > 0)
                    {
                        foreach (var id in model.SelectedRemainingAssessmentGroups)
                        {
                            agrpsDt.Rows.Add(id);
                        }

                    }

                    SqlCommand cmd = new SqlCommand("usp_CreateOrUpdateCampaign", con);
                    cmd.CommandType = CommandType.StoredProcedure;


                    cmd.Parameters.Add(new SqlParameter("@campaignId", SqlDbType.Int));
                    cmd.Parameters["@campaignId"].Value = model.id;

                    cmd.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmd.Parameters["@AssessmentGroupId"].Value = model.AssessmentGroupId;

                    cmd.Parameters.Add(new SqlParameter("@CampaignName", SqlDbType.VarChar));
                    cmd.Parameters["@CampaignName"].Value = model.CampaignName;

                    cmd.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
                    cmd.Parameters["@PerformedBy"].Value = loggedInUser;

                    cmd.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmd.Parameters["@timeNow"].Value = DateTime.Now;

                    cmd.Parameters.Add(new SqlParameter("@CreateMasterAG", SqlDbType.Bit));
                    cmd.Parameters["@CreateMasterAG"].Value = model.CreateMasterAG;

                    SqlParameter resultCampaignId = new SqlParameter();
                    resultCampaignId.ParameterName = "@resultCampaignId";
                    resultCampaignId.DbType = DbType.Int32;
                    resultCampaignId.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(resultCampaignId);


                    if (agrpsDt != null && agrpsDt.Rows.Count > 0)
                    {
                        cmd.Parameters.Add(new SqlParameter("@CampaignAssessmentGroupsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@CampaignAssessmentGroupsBulkData"].Value = agrpsDt;
                    }

                    con.Open();

                    if (cmd.ExecuteNonQuery() != 0)
                    {
                        con.Close();
                    }

                    campaignId = Convert.ToInt32(cmd?.Parameters["@resultCampaignId"]?.Value);
                    model.id = campaignId;
                    return true;

                }

            }

            catch (SqlException e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                campaignId = 0;
                return false;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                campaignId = 0;
                return false;
            }
        }

        /// <summary>
        /// creates mag 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="loggedInUser"></param>
        /// <returns></returns>
        public async Task<bool> CreateMAG(AGCampaignModel model, string loggedInUser)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable agrpsDt = new DataTable();
                    agrpsDt.Columns.Add("AssessmentGroupId", typeof(int));

                    if (model.AssessmentGroups != null && model.AssessmentGroups.Count() > 0 && model.AssessmentGroups.Any(x => x.IsSelectedForCampaign))
                    {
                        foreach (var item in model.AssessmentGroups.Where(x => x.IsSelectedForCampaign))
                        {
                            agrpsDt.Rows.Add(item.id);
                        }

                    }

                    if (model.SelectedRemainingAssessmentGroups != null && model.SelectedRemainingAssessmentGroups.Count() > 0)
                    {
                        foreach (var id in model.SelectedRemainingAssessmentGroups)
                        {
                            agrpsDt.Rows.Add(id);
                        }

                    }

                    SqlCommand cmd = new SqlCommand("usp_CreateMAG", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmd.Parameters["@AssessmentGroupId"].Value = model.AssessmentGroupId;

                    cmd.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
                    cmd.Parameters["@PerformedBy"].Value = loggedInUser;

                    cmd.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmd.Parameters["@timeNow"].Value = DateTime.Now;



                    if (agrpsDt != null && agrpsDt.Rows.Count > 0)
                    {
                        cmd.Parameters.Add(new SqlParameter("@CampaignAssessmentGroupsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@CampaignAssessmentGroupsBulkData"].Value = agrpsDt;
                    }

                    con.Open();

                    if (cmd.ExecuteNonQuery() != 0)
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

        public async Task<bool> EditMAG(AGCampaignModel model, string loggedInUser)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable agrpsDt = new DataTable();
                    agrpsDt.Columns.Add("AssessmentGroupId", typeof(int));

                    if (model.AssessmentGroups != null && model.AssessmentGroups.Count() > 0 && model.AssessmentGroups.Any(x => x.IsSelectedForCampaign))
                    {
                        foreach (var item in model.AssessmentGroups.Where(x => x.IsSelectedForCampaign))
                        {
                            agrpsDt.Rows.Add(item.id);
                        }

                    }

                    if (model.SelectedRemainingAssessmentGroups != null && model.SelectedRemainingAssessmentGroups.Count() > 0)
                    {
                        foreach (var id in model.SelectedRemainingAssessmentGroups)
                        {
                            agrpsDt.Rows.Add(id);
                        }

                    }

                    SqlCommand cmd = new SqlCommand("usp_EditMAG", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@AssessmentGroupId", SqlDbType.Int));
                    cmd.Parameters["@AssessmentGroupId"].Value = model.AssessmentGroupId;

                    cmd.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
                    cmd.Parameters["@PerformedBy"].Value = loggedInUser;

                    cmd.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmd.Parameters["@timeNow"].Value = DateTime.Now;



                    if (agrpsDt != null && agrpsDt.Rows.Count > 0)
                    {
                        cmd.Parameters.Add(new SqlParameter("@CampaignAssessmentGroupsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@CampaignAssessmentGroupsBulkData"].Value = agrpsDt;
                    }

                    con.Open();

                    if (cmd.ExecuteNonQuery() != 0)
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

        public bool AddSkillChannelsForCampaign(AGCampaignModel model, string loggedInUser)
        {
            try
            {

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    DataTable skillChannelsDt = new DataTable();
                    if (model.SkillChannels != null && model.SkillChannels.Count() > 0 && model.SkillChannels.Any(x => x.IsSelectedForCampaign))
                    {
                        skillChannelsDt.Columns.Add("IRFId", typeof(int));

                        foreach (var item in model.SkillChannels.Where(x => x.IsSelectedForCampaign))
                        {
                            skillChannelsDt.Rows.Add(item.id);
                        }

                    }

                    SqlCommand cmd = new SqlCommand("usp_AddUpdateCampaignSkillChanels", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new SqlParameter("@campaignId", SqlDbType.Int));
                    cmd.Parameters["@campaignId"].Value = model.id;

                    cmd.Parameters.Add(new SqlParameter("@PerformedBy", SqlDbType.VarChar));
                    cmd.Parameters["@PerformedBy"].Value = loggedInUser;

                    cmd.Parameters.Add(new SqlParameter("@timeNow", SqlDbType.DateTime));
                    cmd.Parameters["@timeNow"].Value = DateTime.Now;

                    if (skillChannelsDt != null && skillChannelsDt.Rows.Count > 0)
                    {
                        cmd.Parameters.Add(new SqlParameter("@CampaignSkillChannelsBulkData", SqlDbType.Structured));
                        cmd.Parameters["@CampaignSkillChannelsBulkData"].Value = skillChannelsDt;
                    }

                    con.Open();

                    if (cmd.ExecuteNonQuery() != 0)
                    {
                        con.Close();
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

        public async Task<IEnumerable<AssessmentGroupInterviewerDetails>> GetCampaignInterviewingMembers(int campaignId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@campaignId", campaignId);
                    return await SqlMapper.QueryAsync<AssessmentGroupInterviewerDetails>(con, "usp_GetCampaignInterviewingMembers", parameters,
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

        public async Task<IEnumerable<AGCampaignModel>> GetCampaignList()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    //var parameters = new DynamicParameters();
                    //parameters.Add("@campaignId", campaignId);
                    return await SqlMapper.QueryAsync<AGCampaignModel>(con, "usp_GetCampaignList", null,
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

        public async Task<AGCampaignModel> GetCampaignDetailsById(int campaignId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    AGCampaignModel _resultVM = new AGCampaignModel();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetCampaignDetailsById", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@campaignId", campaignId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null)
                        {

                            _resultVM.id = !string.IsNullOrEmpty(dt.Rows[0]["id"].ToString()) ? Convert.ToInt32(dt.Rows[0]["id"]) : 0;
                            _resultVM.CampaignName = !string.IsNullOrEmpty(dt.Rows[0]["CampaignName"].ToString()) ? dt.Rows[0]["CampaignName"].ToString() : null;
                            _resultVM.AssessmentGroupName = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupName"].ToString()) ? dt.Rows[0]["AssessmentGroupName"].ToString() : null;
                            _resultVM.CreatedBy = !string.IsNullOrEmpty(dt.Rows[0]["CreatedBy"].ToString()) ? dt.Rows[0]["CreatedBy"].ToString() : null;
                            _resultVM.CreatedAt = !string.IsNullOrEmpty(dt.Rows[0]["CreatedAt"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["CreatedAt"]) : new DateTime();
                            _resultVM.ModifiedBy = !string.IsNullOrEmpty(dt.Rows[0]["ModifiedBy"].ToString()) ? dt.Rows[0]["ModifiedBy"].ToString() : null;
                            _resultVM.ModifiedAt = !string.IsNullOrEmpty(dt.Rows[0]["ModifiedAt"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["ModifiedAt"]) : new DateTime();
                            _resultVM.AssessmentGroupId = !string.IsNullOrEmpty(dt.Rows[0]["AssessmentGroupId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["AssessmentGroupId"]) : 0;
                            _resultVM.IsAlreadyMasterAG = !string.IsNullOrEmpty(dt.Rows[0]["IsAlreadyMasterAG"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsAlreadyMasterAG"]) : false;


                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<AssignmentVM> assessmentGroups = new List<AssignmentVM>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                assessmentGroups.Add(new AssignmentVM
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    AssessmentGroup = !string.IsNullOrEmpty(dt.Rows[i]["AssessmentGroup"].ToString()) ? dt.Rows[i]["AssessmentGroup"].ToString() : null

                                });
                            }
                            _resultVM.AssessmentGroups = (IList<AssignmentVM>)(assessmentGroups);
                        }
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<IRFVm> irfList = new List<IRFVm>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                irfList.Add(new IRFVm
                                {
                                    id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                                    IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? dt.Rows[i]["IRFId"].ToString() : null,
                                    AssessmentGroupName = !string.IsNullOrEmpty(dt.Rows[i]["AssessmentGroupName"].ToString()) ? dt.Rows[i]["AssessmentGroupName"].ToString() : null

                                });
                            }
                            _resultVM.SkillChannels = (IList<IRFVm>)(irfList);
                        }
                    }
                    if (dataset.Tables.Count > 3)
                    {
                        dt = dataset.Tables[3];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            List<AssessmentGroupInterviewerDetails> interviewers = new List<AssessmentGroupInterviewerDetails>();
                            //var listStaffingAgencyMeetings = new List<TransSkill>();
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                interviewers.Add(new AssessmentGroupInterviewerDetails
                                {
                                    EmailId = !string.IsNullOrEmpty(dt.Rows[i]["EmailId"].ToString()) ? dt.Rows[i]["EmailId"].ToString() : null,
                                    uniqueid = !string.IsNullOrEmpty(dt.Rows[i]["uniqueid"].ToString()) ? dt.Rows[i]["uniqueid"].ToString() : null

                                });
                            }
                            _resultVM.Interviewers = interviewers;
                        }
                    }

                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> CheckCampaignUniqueness(string value)
        {
            try
            {
                //var query = "Select * from IRF where IRFId = @IRFId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@value", value);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_CheckCampaignNameUniqueness", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    if (result.Count() > 0)
                        return false;
                    else
                        return true;

                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }

        public async Task<bool> CheckAssessmentGroupAlraedyExist(string AssessmentGroup)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@AssessmentGroup", AssessmentGroup);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_CheckAssessmentGroupAlraedyExist", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    if (result.Count() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }
        public async Task<IEnumerable<IRFVm>> GetSkillChannelsCreatedUsingTemplates(string EmailId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@EmailId", EmailId);
                    return await SqlMapper.QueryAsync<IRFVm>(con, "usp_GetSkillChannelsCreatedUsingTemplates", dynamicParameters,
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

        public async Task<Candidate> GetCandidateIdAndIrfIdByMeetingId(int interviewId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewId", interviewId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetCandidateIdAndIrfIdByMeetingId", dynamicParameters,
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
        public async Task<IEnumerable<CallibrationCallVM>> GetCalibrationCallDetails(int irfid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CallibrationCallVM>(con, "usp_GetCalibrationCallDetails", dynamicParameters,
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
        public async Task<IEnumerable<CallibrationCallVM>> GetAllCalibrationMeetingsById(string emailid, string type)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Emailid", emailid);
                dynamicParameters.Add("@type", type);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<CallibrationCallVM>(con, "usp_GetAllCalibrationMeetingsdetailsById", dynamicParameters,
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

        public async Task<IEnumerable<SecondaryAssessmentGroups>> GetMAGSecondaryAssessmentGroupsById(int assessmentGroupId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@assessmentGroupId", assessmentGroupId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<SecondaryAssessmentGroups>(con, "usp_GetMAGSecondaryAssessmentGroupsById", dynamicParameters,
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
        public async Task<bool> UpdateConsentFile(int irfid, string uploadedFileURL)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                dynamicParameters.Add("@uploadedFileURL", uploadedFileURL);
                dynamicParameters.Add("@CreatedOn", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateConsentForm", dynamicParameters,
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
        public async Task<string> GetConsentFile(int irfid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetConsentForm", dynamicParameters,
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
        public async Task<IEnumerable<IRF>> GetDayStatsByClientUser(DateTime startDate, DateTime endDate, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetDayStatsByClientUser", parameters, commandTimeout: 600,
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
        public async Task<string> GetCodeSnippetFile(int interviewid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetCodeSnippetFile", dynamicParameters,
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
        public async Task<bool> UpdateCodeSnippetFile(int interviewid, string uploadedFileURL, List<SkillComment> model)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@interviewid", interviewid);
                dynamicParameters.Add("@uploadedFileURL", uploadedFileURL);
                dynamicParameters.Add("@CreatedOn", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateCodeSnippetFile", dynamicParameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                //    DataTable techSkillCommentsDt = new DataTable();
                //    techSkillCommentsDt.Columns.Add("IRFId", typeof(int));
                //    techSkillCommentsDt.Columns.Add("Skill", typeof(int));
                //    techSkillCommentsDt.Columns.Add("Comment", typeof(string));
                //    techSkillCommentsDt.Columns.Add("CreatedBy", typeof(string));
                //    foreach (var item in model)
                //    {
                //        techSkillCommentsDt.Rows.Add(interviewid, Convert.ToInt32(item.Skill), item.Comment, item.CreatedBy);
                //    }
                //    SqlCommand cmdAddSkillComments = new SqlCommand("usp_AddIRFSampleQuestions", con);
                //    cmdAddSkillComments.CommandType = CommandType.StoredProcedure;
                //    cmdAddSkillComments.Parameters.Add(new SqlParameter("@IRFSampleQuestionsBulkData", SqlDbType.Structured));
                //    cmdAddSkillComments.Parameters["@IRFSampleQuestionsBulkData"].Value = techSkillCommentsDt;
                //    cmdAddSkillComments.Parameters.Add(new SqlParameter("@interviewid", SqlDbType.Int));
                //    cmdAddSkillComments.Parameters["@interviewid"].Value = interviewid;
                //    cmdAddSkillComments.Parameters.Add(new SqlParameter("@uploadedFileURL", SqlDbType.VarChar));
                //    cmdAddSkillComments.Parameters["@uploadedFileURL"].Value = uploadedFileURL;
                //    cmdAddSkillComments.Parameters.Add(new SqlParameter("@CreatedOn", SqlDbType.DateTime));
                //    cmdAddSkillComments.Parameters["@CreatedOn"].Value = DateTime.Now;
                //    con.Open();

                //    if (await cmdAddSkillComments.ExecuteNonQueryAsync() != 0)
                //    {
                //        con.Close();
                //    }
                //    return true;
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
            return false;
        }

        public async Task<TransInterviewer> GetInterviewerOpenAIScreeningResults(int irfId, int userId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@userId", userId);
                    var result = await SqlMapper.QueryAsync<TransInterviewer>(con, "usp_GetInterviewerOpenAIScreeningResult", parameters, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveInterviewerOpenAIScreeningResults(int irfId, int userId, string screeningResponseJson)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@userId", userId);
                    parameters.Add("@screeningResponseJson", screeningResponseJson.Replace("'", ""));
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveInterviewerOpenAIScreeningResults", parameters, commandTimeout: 600,
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

        public async Task<bool> SaveInterviewerOpenAIScreeningResultsV2(int irfId, int userId, InterviewerResumeFitmentAnalysisSchema model)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@userId", userId);
                    parameters.Add("@completeFitmentAnalysis", model.completeFitmentAnalysis);// screeningResponseJson.Replace("'", "")
                    parameters.Add("@fitmentScore", model.fitmentScore);
                    parameters.Add("@verdict", model.verdict);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveInterviewerOpenAIScreeningResultsV2", parameters, commandTimeout: 600,
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

        public async Task<IEnumerable<TransInterviewer>> GetTransInterviewersForOpenAIScreening()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<TransInterviewer>(con, "usp_GetTransInterviewersForOpenAIScreening", null, commandTimeout: 600,
                        commandType: CommandType.StoredProcedure);
                    //var result = await SqlMapper.QueryAsync<TransInterviewer>(con, "usp_GetTransInterviewersForOpenAIScreeningTestDataTempSP", null, commandTimeout: 600,
                    //    commandType: CommandType.StoredProcedure);
                    return result;
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveTransInterviewerOpenAIScreeningResults(int transInterviewerId, string screeningResponseJson)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@transInterviewerId", transInterviewerId);
                    parameters.Add("@screeningResponseJson", screeningResponseJson);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveTransInterviewerOpenAIScreeningResults", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<IRF>> GetAllIRFMultiStackDetailsIRF(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@profile", profile);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFCandidateStatsIRFMultiStackCount", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<CandidateExcelData>> IRFMutliStackInterviewStatusDetailedView(string irfname, string type, DateTime startDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfname", irfname);
                    parameters.Add("@type", type);
                    parameters.Add("@startdate", startDate);
                    parameters.Add("@timenow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "dbo.usp_GetIRFMutliStackInterviewStatusDetailedView", parameters, commandTimeout: 600,
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
        public async Task<bool> SaveInterviewerConvertedFromDocResumeLink(int transInterviewerId, string convertedResumeLink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@transInterviewerId", transInterviewerId);
                    parameters.Add("@convertedResumeLink", convertedResumeLink);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_SaveInterviewerConvertedFromDocResumeLink", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<ProjectStatsVM>> GetProjectSkillChannelStatsCount(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<ProjectStatsVM>(con, "dbo.usp_GetProjectSkillChannelStatsCount", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<CandidateExcelData>> GetProjectSkillChannelStatsDetailedView(int projectid, string type, DateTime startDate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Projectid", projectid);
                    parameters.Add("@type", type);
                    parameters.Add("@startdate", startDate);
                    parameters.Add("@timenow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "dbo.usp_GetProjectSkillChannelStatsDetailedView", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<CandidateExcelData>> GetGetDayStatsByClientUserDetailedView(int irfid, string type, DateTime startDate, DateTime enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfid", irfid);
                    parameters.Add("@type", type);
                    parameters.Add("@startdate", startDate);
                    parameters.Add("@enddate", enddate);
                    parameters.Add("@timenow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "dbo.usp_GetDayStatsByClientUserStatsDetailedView", parameters, commandTimeout: 600,
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

        public async Task<IRF> GetIRFBasedOnTuringsJobId(int countryType, string jobId, string planType = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@countryType", countryType);
                    parameters.Add("@jobId", jobId);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@planType", planType);
                    var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetIRFBasedOnTuringsJobId", parameters, commandTimeout: 600,
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
            return null;
        }
        public async Task<IEnumerable<MeetingVM>> GetProjectInterviewMeetings(int id, string emailId = null)
        {
            try
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                parameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetProjectInterviewMeetings", parameters,
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
        public async Task<IEnumerable<ProjectStatsVM>> GetPartnerDetailsStatsCount(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<ProjectStatsVM>(con, "dbo.usp_GetPartnerDetailsStatsCount", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<CandidateExcelData>> GetPartnerDetailsStatsDetailedView(int userid, string type, DateTime startDate, DateTime enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@userid", userid);
                    parameters.Add("@type", type);
                    parameters.Add("@startdate", startDate);
                    parameters.Add("@endDate", enddate);
                    parameters.Add("@timenow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "dbo.usp_GetPartnerDetailsStatsDetailedView", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<IRF>> GetUserDetailsStatsCount(DateTime startDate, DateTime endDate, string profile, string emailid)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@startDate", startDate);
                    parameters.Add("@endDate", endDate);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@emailid", emailid);
                    return await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_GetUserDetailsStatsCount", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<CandidateExcelData>> GetUserStatsDetailedView(int irfid, string emailid, string type, DateTime startDate, DateTime enddate)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@irfid", irfid);
                    parameters.Add("@type", type);
                    parameters.Add("@emailid", emailid);
                    parameters.Add("@startdate", startDate);
                    parameters.Add("@endDate", enddate);
                    parameters.Add("@timenow", DateTime.Now);
                    return await SqlMapper.QueryAsync<CandidateExcelData>(con, "dbo.usp_GetUserStatsDetailedView", parameters, commandTimeout: 600,
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
        public async Task<IEnumerable<Candidate>> GetProjectCandidates(int id, string emailId = null)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Projectid", id);
                dynamicParameters.Add("@emailId", emailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync<Candidate>(con, "usp_GetProjectCandidates", dynamicParameters,
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

        public async Task<bool> AddAGInterviewerToSkillChannel(int irfId, int userId, string loggedInEmailId)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@userId", userId);
                dynamicParameters.Add("@irfId", irfId);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                dynamicParameters.Add("@AddedBy", loggedInEmailId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "usp_AddAGInterviewerToSkillChannel", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<int>> GetIRFList(int ClientId)
        {
            try
            {
                var query = "Select id from IRF where ClientId = @clientId";
                var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@clientId", ClientId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> SaveInterviewerBillingRate(int billingrate, int Userid, int IRFid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFid", IRFid);
                dynamicParameters.Add("@Userid", Userid);
                dynamicParameters.Add("@CreatedOn", DateTime.Now);
                dynamicParameters.Add("@billingrate", billingrate);
                dynamicParameters.Add("@billingtype", "USD");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = await SqlMapper.QueryAsync(con, "Usp_SaveInterviewerBillingRate", dynamicParameters,
                       commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<AssignmentVM>> GetAllMAGs()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var dynamicParameters = new DynamicParameters();
                    return await SqlMapper.QueryAsync<AssignmentVM>(con, "usp_GetAllMAGs", dynamicParameters,
                         commandType: CommandType.StoredProcedure);
                }

            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<InterviewersBillingRateVM>> GetInterviewersWithOutBillingRate()
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@Clientid", 0);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    return await SqlMapper.QueryAsync<InterviewersBillingRateVM>(con, "usp_GetInterviewersWithOutBillingRate", dynamicParameters,
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
        public async Task<bool> UpdateUSTJobTitle(int agId, string ustJobTitle, string loggedInUser)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@agId", agId);
                    parameters.Add("@ustJobTitle", ustJobTitle);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@PerformedBy", loggedInUser);
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateUSTJobTitle", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UpdateInterviewerSurcharge(int userId, int surcharge, int irfId, string loggedInUser)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@userId", userId);
                    parameters.Add("@surcharge", surcharge);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@irfId", irfId);
                    parameters.Add("@PerformedBy", loggedInUser);
                    var results = await SqlMapper.QueryAsync(con, "usp_UpdateInterviewerSurcharge", parameters,
                           commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public int GetIRFIdByIRFName(string IRFId, string tenantId)
        {
            try
            {
                var query = "Select IRF.id from IRF join Client cl on cl.id=IRF.ClientId where IRFId=@IRFId and cl.tenantId=@tenantId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);
                dynamicParameters.Add("@tenantId", tenantId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<SkillChannelnAGDetails> CheckExistenceofSkillChannelAgainstRequestId(string requestId, string jobTitle)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var returnObj = new SkillChannelnAGDetails();

                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_CheckExistenceofSkillChannelAgainstRequestId", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@requestId", requestId);
                    adapter.SelectCommand.Parameters.AddWithValue("@jobTitle", jobTitle);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        returnObj.irfId = dt.Rows[0]["IRFId"] != null ? Convert.ToInt16(dt.Rows[0]["IRFId"].ToString()) : 0;
                        returnObj.assessmentGroupId = dt.Rows[0]["AssessmentGroupId"] != null ? Convert.ToInt16(dt.Rows[0]["AssessmentGroupId"].ToString()) : 0;
                        returnObj.AssessmentGroupName = dt.Rows[0]["AssessmentGroupName"] != null ? dt.Rows[0]["AssessmentGroupName"].ToString() : null;
                        returnObj.TemplateId = dt.Rows[0]["TemplateId"] != null ? Convert.ToInt16(dt.Rows[0]["TemplateId"].ToString()) : 0;
                        returnObj.TemplateName = dt.Rows[0]["TemplateName"] != null ? dt.Rows[0]["TemplateName"].ToString() : null;
                        returnObj.IRFName = dt.Rows[0]["IRFName"] != null ? dt.Rows[0]["IRFName"].ToString() : null;

                    }
                    //return new Tuple<int, int>(irfId, assessmentGroupId);
                    return returnObj;
                }
            }

            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}");
                return null;
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, $"Message : {ex.Message}; StackTrace : {ex.StackTrace};  Source : {ex.Source}");
                return null;

            }
        }
        public int GetIRFIdByIRFNametenantId(string IRFId, string tenantId)
        {
            try
            {
                var query = "Select IRF.id from IRF join Client cl on cl.id=IRF.ClientId where IRFId=@IRFId and cl.tenantId=@tenantId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFId);
                dynamicParameters.Add("@tenantId", tenantId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }
        public async Task<bool> UpdateVideoConsentFile(int irfid, string uploadedFileURL)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                dynamicParameters.Add("@uploadedFileURL", uploadedFileURL);
                dynamicParameters.Add("@CreatedOn", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateVideoConsentForm", dynamicParameters,
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
        public async Task<string> GetVideoConsentForm(int irfid)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@irfid", irfid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetVideoConsentForm", dynamicParameters,
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
        public async Task<bool> UpdateCandidateChecks(int irfid, bool CandidateCheckvalue, string CandidateCheckName)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irfid);
                        parameters.Add("@CandidateCheckvalue", CandidateCheckvalue);
                        parameters.Add("@CandidateCheckName", CandidateCheckName);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_UpdateCandidateChecks", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<bool> UpdateNewPriority(int irfid, int PriorityLevel)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@irfid", irfid);
                        parameters.Add("@PriorityLevel", PriorityLevel);
                        var result = await SqlMapper.QueryAsync<IRF>(con, "dbo.usp_UpdateIRFPriority", parameters, null,
                            commandType: CommandType.StoredProcedure);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
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
        public async Task<IEnumerable<AssignmentVM>> GetUnassignedInterviewsMAG()
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeNow", DateTime.Now);
                //string conStringTest = "Server=tcp:risebirdtest.database.windows.net,1433;Initial Catalog=productTestProdDB;Persist Security Info=False;User ID=risebirdtestadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                //connectionString = conStringTest;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<AssignmentVM>(con, "usp_GetUnassignedInterviewsMAG", dynamicParameters,
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
    }
}

