using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ProjectService : IProjectService
    {
        private IProjectRepository _projectRepository;
        private IApplicationErrorLogger _applicationErrorLogger;

        public ProjectService(IProjectRepository projectRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _projectRepository = projectRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public IEnumerable<Project> GetAllProjects()
        {
            try
            {
                var projects = _projectRepository.GetAllProjects();
                if(projects != null && projects.Count() != 0)
                {
                    foreach (var item in projects)
                    {
                        //pass projectId to get total number of IRFs
                        var projectCluster = _projectRepository.GetProjectCluster(item.id).ToList();
                        item.TotalIRF = projectCluster != null ? projectCluster.Count() : 0;
                    }
                }
                
                return projects;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<IRF> GetAllIRF()
        {
            try
            {               
                return _projectRepository.GetAllIRF();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public Project GetProject(int id)
        {
            try
            {
                var project = _projectRepository.GetAllProjects().Where(x => x.id == id).FirstOrDefault();
                var projectCluster = _projectRepository.GetProjectCluster(id);
                var allIRFs= _projectRepository.GetAllIRF();
                if(projectCluster!=null && projectCluster.Count() != 0)
                {
                    foreach (var item in projectCluster)
                    {
                        item.PrimarySkills = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().JDTiltle.Split("|")[0];
                        item.Client = allIRFs.Where(x => x.id == item.IRFId).FirstOrDefault().ClientCompName;
                        allIRFs = allIRFs.Where(x => x.id != item.IRFId);
                    }
                }
                

                var objList = new List<ProjectIRFMapping>();

                if(projectCluster != null && projectCluster.Count() != 0)
                {
                    foreach (var item in projectCluster)
                    {
                        var obj = new ProjectIRFMapping();
                        obj.IRFId = item.IRFId;
                        obj.IRFName = item.IRFName;
                        obj.IsAdded = item.IsAdded;
                        //obj.id = 0;
                        obj.PrimarySkills = item.PrimarySkills;
                        //obj.ProjectId = 0;
                        obj.Client = item.Client;
                        objList.Add(obj);
                    }

                }
                
                foreach (var item in allIRFs)
                {

                    var obj = new ProjectIRFMapping();
                    obj.IRFId = item.id;
                    obj.IRFName = item.IRFId;
                    obj.IsAdded = false;
                    //obj.id = 0;
                    obj.PrimarySkills = item.JDTiltle.Split("|")[0];
                    //obj.ProjectId = 0;
                    obj.Client = item.ClientCompName;
                    objList.Add(obj);
                }
                project.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);
                return project;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<ProjectIRFMapping> GetProjectCluster(int id)
        {
            try
            {
                return _projectRepository.GetProjectCluster(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddProject(Project projectDetails)
        {
            try
            {
                projectDetails.ProjectIRFMappings = projectDetails.ProjectIRFMappings.Where(x => x.IsAdded == true).ToList();

                return _projectRepository.AddProject(projectDetails).Result;
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
                return _projectRepository.DeleteProject(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool UpdateProject(Project model)
        {
            try
            {
                model.ProjectIRFMappings = model.ProjectIRFMappings.Where(x => x.IsAdded == true).ToList();
                return _projectRepository.UpdateProject(model).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<Client> GetClients()
        {
            try
            {
                var clients = new List<Client>();
                clients.Add(new Client { Name = "All", id = 0, Description = "All" });
                foreach (var item in _projectRepository.GetClients())
                {
                    clients.Add(item);
                }
                
                return clients;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
