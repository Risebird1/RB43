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
        private IIRFRepository _irfRepository;

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

        public ProjectFunnel GetFunnelData(int id)
        {
            try
            {
                //total number of candidates
                int waste = 0;
                var funnelData = new ProjectFunnel();
                var irfList = _projectRepository.GetIRFIdList(id);
                funnelData.TotalIRFs = irfList.Count();
                foreach (var irfId in irfList)
                {
                    var irfName = _irfRepository.GetIRFNameBasedOnId(irfId);
                    var candidates = _irfRepository.GetIRFCandidates(irfId);
                    var emailList = candidates.Select(x => x.EmailId);
                    funnelData.TotalCandidates = funnelData.TotalCandidates + candidates.Count();
                    funnelData.TotalInvites = funnelData.TotalInvites+ candidates.Where(x => x.TotalInvites > 0).Count();
                    //funnelData.InvitesRate = funnelData.TotalCandidates != 0 ? ((funnelData.TotalInvites * 100 / funnelData.TotalCandidates) ) : 0;

                    //get interviewMeetings where candidateEmail and irfName matches
                    var interviewIdList = new List<int>();
                    foreach (var email in emailList)
                    {
                        var interviewId = _irfRepository.GetInterviewMeetings(irfName, email);
                        if (interviewId != 0)
                        {
                            interviewIdList.Add(interviewId);
                        }
                    }

                    funnelData.TotalConfirmations = funnelData.TotalConfirmations + interviewIdList.Count();
                    //funnelData.ConfirmationRate = funnelData.TotalInvites != 0 ? ((funnelData.TotalConfirmations * 100 / funnelData.TotalInvites)) : 0;

                    string ids = string.Join<int>(",", interviewIdList);
                    var meetingData = _irfRepository.GetInterviewComments(ids);
                    if (meetingData != null)
                    {
                        var completed = meetingData.Where(x => x.MeetingStatus == 4);
                        //var totalCompleted = completed.Count();
                        funnelData.TotalCompleted = funnelData.TotalCompleted + completed.Count();
                        //funnelData.CompletionRate = funnelData.TotalConfirmations != 0 ? ((totalCompleted * 100 / funnelData.TotalConfirmations) ) : 0;
                        var risebirdSelected = completed.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4));
                        //var totalRisebirdSelected = risebirdSelected.Count();
                        funnelData.TotalRisebirdSelection = funnelData.TotalRisebirdSelection + risebirdSelected.Count();
                        //funnelData.RisebirdSelectionRate = totalCompleted!=0?(totalRisebirdSelected * 100 / totalCompleted) : 0;
                        var approved = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved == 1);
                        //var totalApproved = approved.Count();
                        funnelData.TotalApproved = funnelData.TotalApproved + approved.Count();
                        var approvalFilter = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved > 2);
                        //var approvalDenominator = totalRisebirdSelected - approvalFilter.Count();
                        //funnelData.ApprovalRate = approvalDenominator != 0 ? (totalApproved * 100 / approvalDenominator) : 0;
                        waste = waste + approvalFilter.Where(x => x.IsApproved > 3).Count();
                        var finalSelected = approved.Where(x => x.IsSelected.HasValue && x.IsSelected == 1);
                        //var totalFinalSelected = finalSelected.Count();
                        funnelData.TotalFinalSelections = funnelData.TotalFinalSelections + finalSelected.Count();
                        //var finalSelectedDeductions = approved.Where(x => x.IsSelected.HasValue && x.IsSelected != 1 && x.IsSelected != 2 && x.IsSelected != 6).Count();
                        //var finalSelectionDenominator = totalApproved - finalSelectedDeductions;
                        //funnelData.FinalSelectionRate = finalSelectionDenominator != 0 ? ((totalFinalSelected * 100 / finalSelectionDenominator) * 100) : 0;
                        waste = waste + approved.Where(x => x.IsSelected.HasValue && x.IsSelected > 7).Count();
                        var offered = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus == 1);
                        //var totalOffered = offered.Count();
                        funnelData.TotalOffered = funnelData.TotalOffered+ offered.Count();
                        var offerDeductions = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus > 3);
                        //var offerDenominator = totalFinalSelected - offerDeductions.Count();
                        //funnelData.OfferRate = offerDenominator != 0 ? ((totalOffered * 100 / offerDenominator) * 100) : 0;
                        waste = waste + offerDeductions.Where(x => x.OfferStatus.HasValue && x.OfferStatus != 4).Count();
                        var joined = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus == 1);
                        //var totalJoined = joined.Count();
                        funnelData.TotalJoined = funnelData.TotalJoined + joined.Count();
                        var joinedDeductions = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus > 2);
                        //var joinedDenominator = totalOffered - joinedDeductions.Count();
                        //funnelData.JoiningRate = joinedDenominator != 0 ? ((totalJoined * 100 / joinedDenominator) * 100) : 0;
                        waste = waste + joinedDeductions.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus >= 5).Count();
                        //funnelData.WasteRate = totalRisebirdSelected != 0 ? (waste * 100 / totalRisebirdSelected) : 0;
                    }


                }
                
                return funnelData;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

    }
}
