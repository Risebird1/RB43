using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFRepository _irfRepository;

        public ProjectService(IProjectRepository projectRepository, IApplicationErrorLogger applicationErrorLogger, IIRFRepository irfRepository)
        {
            _projectRepository = projectRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfRepository = irfRepository;
        }

        public IEnumerable<Project> GetAllProjects()
        {
            try
            {
                var projects = _projectRepository.GetAllProjects();
                if (projects != null && projects.Count() != 0)
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

        public IEnumerable<Project> GetProjects(string emailId, string profile)
        {
            try
            {
                var projects = _projectRepository.GetProjects(emailId, profile).Result;
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
                var project = _projectRepository.GetAllProjects().FirstOrDefault(x => x.id == id);
                var projectCluster = _projectRepository.GetProjectCluster(id);
                var allIRFs = _projectRepository.GetAllIRF();
                if (projectCluster != null && projectCluster.Any())
                {
                    foreach (var item in projectCluster)
                    {
                        item.PrimarySkills = allIRFs.FirstOrDefault(x => x.id == item.IRFId).JDTiltle.Split("|")[0];
                        item.Client = allIRFs.FirstOrDefault(x => x.id == item.IRFId).ClientCompName;
                        allIRFs = allIRFs.Where(x => x.id != item.IRFId);
                    }
                }


                var objList = new List<ProjectIRFMapping>();

                if (projectCluster != null && projectCluster.Any())
                {
                    foreach (var item in projectCluster)
                    {
                        var obj = new ProjectIRFMapping
                        {
                            IRFId = item.IRFId,
                            IRFName = item.IRFName,
                            IsAdded = item.IsAdded,
                            //obj.id = 0;
                            PrimarySkills = item.PrimarySkills,
                            //obj.ProjectId = 0;
                            Client = item.Client
                        };
                        objList.Add(obj);
                    }

                }

                foreach (var item in allIRFs)
                {

                    var obj = new ProjectIRFMapping
                    {
                        IRFId = item.id,
                        IRFName = item.IRFId,
                        IsAdded = false,
                        //obj.id = 0;
                        PrimarySkills = item.JDTiltle.Split("|")[0],
                        //obj.ProjectId = 0;
                        Client = item.ClientCompName
                    };
                    objList.Add(obj);
                }

                if (project != null)
                {
                    project.ProjectIRFMappings = (IList<ProjectIRFMapping>)(objList);
                    return project;
                }
                return null;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public Project GetProjectEdit(int id)
        {
            try
            {
                string irfList = string.Empty;
                var project = _projectRepository.GetProjectbyId(id);
                var projectCluster = _projectRepository.GetProjectCluster(id);
                if (projectCluster != null && projectCluster.Count() != 0)
                {
                    foreach (var item in projectCluster)
                    {
                        item.PrimarySkills = item.JDTiltle.Split("|")[0];
                    }
                    irfList = string.Join(",", from item in projectCluster select item.IRFId);
                }
                var remainingIRF = _projectRepository.GetIRFByClient(project.ClientId, irfList);
                if (remainingIRF != null && remainingIRF.Count() != 0)
                {
                    foreach (var item in remainingIRF)
                    {
                        item.PrimarySkills = item.JDTiltle.Split("|")[0];
                    }
                }
                var objList = new List<ProjectIRFMapping>();
                objList.AddRange(projectCluster);
                objList.AddRange(remainingIRF);
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
                projectDetails.CreatedOn = DateTime.Now;
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
        public async Task<bool> UnArchiveProject(int id)
        {
            try
            {
                return _projectRepository.UnArchiveProject(id).Result;
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
                var clients = new List<Client>
                {
                    new Client { Name = "All", id = 0, Description = "All" }
                };
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

        public async Task<ProjectFunnel> GetFunnelData(int id)
        {
            try
            {
                //total number of candidates
                int waste = 0;
                var funnelData = new ProjectFunnel();
                var emailList = new List<string>();
                var irfList = _projectRepository.GetIRFIdList(id);
                funnelData.TotalIRFs = irfList.Count();
                foreach (var irfId in irfList)
                {
                    var irfName = _irfRepository.GetIRFNameBasedOnId(irfId);
                    var candidates = await _irfRepository.GetIRFCandidates(irfId);
                    if (!candidates.Any())
                    {
                        //get candidates from Meeting table by passing IRFId
                        emailList = _irfRepository.GetIRFCandidatesEmail(irfName).ToList();
                        funnelData.TotalCandidates = funnelData.TotalCandidates + emailList.Count;
                        funnelData.TotalInvites = funnelData.TotalInvites + emailList.Count;
                    }
                    else
                    {
                        emailList = candidates.Select(x => x.EmailId).ToList();
                        funnelData.TotalCandidates = funnelData.TotalCandidates + candidates.Count();
                        funnelData.TotalInvites = funnelData.TotalInvites + candidates.Count(x => x.TotalInvites > 0);
                    }

                    var interviewIdList = new List<int>();
                    foreach (var email in emailList)
                    {
                        var interviewId = _irfRepository.InterviewMeetings(irfName, email);
                        if (interviewId != 0)
                        {
                            interviewIdList.Add(interviewId);
                        }
                    }

                    funnelData.TotalConfirmations = funnelData.TotalConfirmations + interviewIdList.Count;
                    //funnelData.ConfirmationRate = funnelData.TotalInvites != 0 ? ((funnelData.TotalConfirmations * 100 / funnelData.TotalInvites)) : 0;

                    string ids = string.Join<int>(",", interviewIdList);
                    var meetingData = _irfRepository.GetInterviewComments(ids);
                    if (meetingData != null)
                    {
                        var completed = meetingData.Where(x => x.MeetingStatus == 4);
                        funnelData.TotalCompleted = funnelData.TotalCompleted + completed.Count();
                        var risebirdSelected = completed.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4));
                        funnelData.TotalRisebirdSelection = funnelData.TotalRisebirdSelection + risebirdSelected.Count();
                        var approved = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved == 1);
                        funnelData.TotalApproved = funnelData.TotalApproved + approved.Count();
                        var approvalFilter = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved > 2);
                        waste = waste + approvalFilter.Count(x => x.IsApproved > 3);
                        var finalSelected = approved.Where(x => x.IsSelected.HasValue && x.IsSelected == 1);
                        funnelData.TotalFinalSelections = funnelData.TotalFinalSelections + finalSelected.Count();
                        waste = waste + approved.Count(x => x.IsSelected.HasValue && x.IsSelected > 7);
                        var offered = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus == 1);
                        funnelData.TotalOffered = funnelData.TotalOffered + offered.Count();
                        var offerDeductions = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus > 3);
                        waste = waste + offerDeductions.Count(x => x.OfferStatus.HasValue && x.OfferStatus != 4);
                        var joined = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus == 1);
                        funnelData.TotalJoined = funnelData.TotalJoined + joined.Count();
                        var joinedDeductions = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus > 2);
                        waste += joinedDeductions.Count(x => x.JoiningStatus.HasValue && x.JoiningStatus >= 5);
                    }


                }

                return funnelData;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public IEnumerable<Client> GetClientsList()
        {
            try
            {
                return _projectRepository.GetClients()?.OrderBy(x => x.Name);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

    }
}
