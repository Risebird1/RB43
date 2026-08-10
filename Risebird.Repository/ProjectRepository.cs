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
                        con, "usp_GetAllProjectsList", null, commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<Project>("usp_GetProjectById", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<IRF>("usp_GetActiveIRFList", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@IsDeleted", 0);
                dynamicParameters.Add("@ClientId", clientId);
                dynamicParameters.Add("@ExcludeIRFIds", irfIdList);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<ProjectIRFMapping>("usp_GetIRFByClientId", dynamicParameters,
                        commandType: CommandType.StoredProcedure);
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
                    var results = await con.QueryAsync<int>("usp_GetMaxProjectId", null,
                        commandType: CommandType.StoredProcedure);
                    Random rnd = new Random();
                    int nums = rnd.Next(10, 99);
                    projectDetails.ProjectUniqueid = "RB" + projectDetails.ClientId.ToString() + (results.FirstOrDefault() + 1).ToString("D5") + nums.ToString();

                    var parameters = new DynamicParameters();
                    parameters.Add("@Name", projectDetails.Name);
                    parameters.Add("@ClientId", projectDetails.ClientId);
                    parameters.Add("@EndClient", projectDetails.EndClient);
                    parameters.Add("@Location", projectDetails.Location);
                    parameters.Add("@ProjectOwnerId", projectDetails.ProjectOwnerId);
                    parameters.Add("@CreatedOn", projectDetails.CreatedOn);
                    parameters.Add("@IsDeleted", false);
                    parameters.Add("@ProjectUniqueid", projectDetails.ProjectUniqueid);
                    parameters.Add("@SkillSetsevaluate", projectDetails.SkillSetsevaluate);
                    parameters.Add("@ProjectTitle", projectDetails.ProjectTitle);
                    parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await con.ExecuteAsync("usp_InsertProject", parameters, commandType: CommandType.StoredProcedure);
                    var projectId = parameters.Get<int>("@NewId");

                    if (projectDetails.ProjectIRFMappings != null)
                    {
                        foreach (var item in projectDetails.ProjectIRFMappings)
                        {
                            var mappingParameters = new DynamicParameters();
                            mappingParameters.Add("@ProjectId", projectId);
                            mappingParameters.Add("@IRFId", item.IRFId);
                            mappingParameters.Add("@IsAdded", item.IsAdded);
                            await con.ExecuteAsync("usp_InsertProjectIRFMapping", mappingParameters,
                                commandType: CommandType.StoredProcedure);
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
                parameters.Add("@ProjectTitle", projectDetails.ProjectTitle);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    await SqlMapper.QueryAsync<int>(con, "usp_UpdateProject", parameters,
                       commandType: CommandType.StoredProcedure);



                    if (projectDetails.ProjectIRFMappings != null)
                    {
                        foreach (var item in projectDetails.ProjectIRFMappings)
                        {
                            var mappingParameters = new DynamicParameters();
                            mappingParameters.Add("@ProjectId", projectDetails.id);
                            mappingParameters.Add("@IRFId", item.IRFId);
                            mappingParameters.Add("@IsAdded", item.IsAdded);
                            await con.ExecuteAsync("usp_InsertProjectIRFMapping", mappingParameters,
                                commandType: CommandType.StoredProcedure);
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
                        con, "usp_GetAllClients", null, commandType: CommandType.StoredProcedure);
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
