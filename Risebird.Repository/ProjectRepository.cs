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

namespace Risebird.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ProjectRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public IEnumerable<Project> GetAllProjects()
        {
            IEnumerable<Project> projects = new List<Project>();
            try
            {
                //select * from Project where IsDeleted=0
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    projects = SqlMapper.Query<Project>(
                        con, "select * from Project", null, commandType: System.Data.CommandType.Text);
                }
                return projects;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<IEnumerable<Project>> GetProjects(string emailID = null, string profileName = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@EmailId", emailID);
                    parameters.Add("@Profile", profileName);
                    return await SqlMapper.QueryAsync<Project>(con, "usp_GetAllProjects", parameters,
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

        public Project GetProjectbyId(int id)
        {
            try
            {
                var query = "select * from Project where id=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Project>(query, dynamicParameters);
                    return results.FirstOrDefault();
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ProjectIRFMapping> GetProjectCluster(int projectId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ProjectId", projectId);
                    return SqlMapper.Query<ProjectIRFMapping>(con, "usp_GetProjectCluster", parameters,
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

        public IEnumerable<IRF> GetAllIRF()
        {
            try
            {
                //var query = "Select IRF.*, Client.[Name] as ClientCompName,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle from IRF inner join Client on IRF.ClientId = Client.id where IsDeleted = @IsDeleted";
                var query = "Select IRF.id,IRF.IRFId,IRF.SkillSetsevaluate,IRF.Clientid, Client.[Name] as ClientCompName,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle from IRF inner join Client on IRF.ClientId = Client.id where IsDeleted = @IsDeleted";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<ProjectIRFMapping> GetIRFByClient(int clientId, string irfIdList)
        {
            try
            {
                string query = string.Empty;
                if (!string.IsNullOrEmpty(irfIdList))
                {
                    query = "Select IRF.id as IRFId,IRF.IRFId as IRFName, Client.[Name] as Client, irf.JDTiltle, IRF.ClientId from IRF inner join Client on IRF.ClientId = Client.id where irf.ClientId=@ClientId and IsDeleted = @IsDeleted and irf.id not in (" + irfIdList + ")";
                }
                else
                {
                    query = "Select IRF.id as IRFId,IRF.IRFId as IRFName, Client.[Name] as Client, irf.JDTiltle, IRF.ClientId from IRF inner join Client on IRF.ClientId = Client.id where irf.ClientId=@ClientId and IsDeleted = @IsDeleted";
                }
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@ClientId", clientId);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ProjectIRFMapping>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }


        public async Task<bool> AddProject(Project projectDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var query = "select top (1)id from Project order by id desc";
                    var results = con.Query<int>(query, null);
                    Random rnd = new Random();
                    int nums = rnd.Next(10, 99);
                    projectDetails.ProjectUniqueid = "RB" + projectDetails.ClientId.ToString() + (results.FirstOrDefault() + 1).ToString("D5") + nums.ToString();
                    string sqlInsertReport = "INSERT INTO Project (Name,ClientId, EndClient, Location, ProjectOwnerId, IsDeleted, CreatedOn,ProjectUniqueid,SkillSetsevaluate ) Values (@Name,@ClientId,@EndClient, @Location,@ProjectOwnerId, @IsDeleted, @CreatedOn,@ProjectUniqueid,@SkillSetsevaluate);";
                    var affectedRows = con.Execute(sqlInsertReport, new
                    {
                        projectDetails.Name,
                        projectDetails.ClientId,
                        projectDetails.EndClient,
                        projectDetails.Location,
                        projectDetails.ProjectOwnerId,
                        projectDetails.CreatedOn,
                        IsDeleted = false,
                        projectDetails.ProjectUniqueid,
                        projectDetails.SkillSetsevaluate

                    });
                    var projectList = SqlMapper.QueryAsync<Project>(
                        con, "select * from Project", null, commandType: System.Data.CommandType.Text).Result.AsList();
                    var projectId = projectList.Where(x => x.Name == projectDetails.Name).Last().id;
                    if (projectDetails.ProjectIRFMappings != null)
                    {
                        //update techskillreport table
                        foreach (var item in projectDetails.ProjectIRFMappings)
                        {
                            string sqlTechReport = "INSERT INTO ProjectIRFMapping (ProjectId,IRFId,IsAdded) Values (@ProjectId, @IRFId, @IsAdded);";
                            var result1 = con.ExecuteAsync(sqlTechReport, new
                            {
                                ProjectId = projectId,
                                item.IRFId,
                                item.IsAdded

                            }).Result;
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

        public async Task<bool> DeleteProject(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_ArchiveProject", dynamicParameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> UnArchiveProject(int id)
        {
            try
            {
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_UnArchiveProject", dynamicParameters, commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }

        }



        public async Task<bool> UpdateProject(Project projectDetails)
        {
            try
            {

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", projectDetails.id);
                parameters.Add("@Name", projectDetails.Name);
                parameters.Add("@ClientId", projectDetails.ClientId);
                parameters.Add("@EndClient", projectDetails.EndClient);
                parameters.Add("@Location", projectDetails.Location);
                parameters.Add("@ProjectOwnerId", projectDetails.ProjectOwnerId);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateProject", parameters,
                       commandType: CommandType.StoredProcedure);



                    if (projectDetails.ProjectIRFMappings != null)
                    {
                        foreach (var item in projectDetails.ProjectIRFMappings)
                        {
                            string sqlQuery = "INSERT INTO ProjectIRFMapping (ProjectId,IRFId,IsAdded) Values (@ProjectId, @IRFId, @IsAdded);";
                            var affectedRows = con.ExecuteAsync(sqlQuery, new
                            {
                                ProjectId = projectDetails.id,
                                item.IRFId,
                                item.IsAdded
                            }).Result;
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

        public IEnumerable<Client> GetClients()
        {
            IEnumerable<Client> client = new List<Client>();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    client = SqlMapper.Query<Client>(
                        con, "select * from Client", null, commandType: CommandType.Text);
                }
                return client;
            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<int> GetIRFIdList(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@id", id);
                    return SqlMapper.Query<int>(con, "usp_GetIRFIdList", parameters,
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



    }
}
