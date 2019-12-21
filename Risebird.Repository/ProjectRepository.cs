using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Repository
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;

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

        public IEnumerable<ProjectIRFMapping> GetProjectCluster(int projectId)
        {
            try
            {
                var query = "select ProjectIRFMapping.*, Project.[Name] as ProjectName, IRF.IRFId as IRFName, IRF.ClientId, IRF.id as IRFId from ProjectIRFMapping inner join IRF on ProjectIRFMapping.IRFId = IRF.id left join Project on ProjectIRFMapping.ProjectId = Project.id where ProjectIRFMapping.ProjectId = @ProjectId and ProjectIRFMapping.IsAdded = 1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@ProjectId", projectId);
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

        public IEnumerable<IRF> GetAllIRF()
        {
            try
            {
                //if (emailid != string.Empty && profileName == "Client")
                //{
                //    var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id inner join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id inner join[dbo].[UserList] ul on ul.id = tc.ClientUserId where IsDeleted = @IsDeleted and ul.EmailId = @emailid";
                //    var dynamicParameters = new DynamicParameters();
                //    dynamicParameters.Add("@IsDeleted", 0);
                //    dynamicParameters.Add("@emailid", emailid);
                //    using (SqlConnection con = new SqlConnection(connectionString))
                //    {
                //        var results = con.Query<IRF>(query, dynamicParameters);
                //        return results;
                //    }
                //}
                //else
                //{
                   
                //}
                var query = "Select IRF.*, Client.[Name] as ClientCompName from IRF inner join Client on IRF.ClientId = Client.id where IsDeleted = @IsDeleted";
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

        public async Task<bool> AddProject(Project projectDetails)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    
                    
                    string sqlInsertReport = "INSERT INTO Project (Name,Client, EndClient, Location, ProjectOwnerId, IsDeleted, CreatedOn ) Values (@Name,@Client,@EndClient, @Location,@ProjectOwnerId, @IsDeleted, @CreatedOn);";
                    var affectedRows = con.Execute(sqlInsertReport, new
                    {
                        Name = projectDetails.Name,
                        Client=projectDetails.Client,
                        EndClient = projectDetails.EndClient,
                        Location = projectDetails.Location,
                        ProjectOwnerId = projectDetails.ProjectOwnerId,
                        CreatedOn =projectDetails.CreatedOn,
                        IsDeleted = false

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
                                IRFId = item.IRFId,
                                IsAdded = item.IsAdded
                                
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
                var query = "update Project set IsDeleted=1 where id=@id";
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, parameters);
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
        public async Task<bool> UnArchiveProject(int id)
        {
            try
            {
                var query = "update Project set IsDeleted=0 where id = @id;";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.QueryAsync<int>(query, dynamicParameters);
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
                parameters.Add("@Client", projectDetails.Client);
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
                                IRFId = item.IRFId,
                                IsAdded = item.IsAdded
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
                //_errorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public IEnumerable<int> GetIRFIdList(int id)
        {
            try
            {
                
                var query = "select IRFId from ProjectIRFMapping where ProjectId=@id";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@id", id);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(query, dynamicParameters);
                    return results;
                }

            }
            catch (SqlException ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

    }
}
