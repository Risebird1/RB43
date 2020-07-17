using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.ViewModel;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewerService : IInterviewerService
    {
        private IInterviewerRepository _interviewerRepository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IUserService _userService;
        private IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;

        public InterviewerService(IInterviewerRepository interviewerRepository, IApplicationErrorLogger applicationErrorLogger, IUserService userService, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _interviewerRepository = interviewerRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _userService = userService;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }

        #region Interviewer Profile Service Functions

        public List<InterviewerProfileVM> GetInterviewers()
        {
            try
            {
                return _interviewerRepository.GetInterviewers().Result.ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public async Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                //Add Method
                return _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile).Result;


            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> EditInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                //Edit Method
                interviewerProfile.InterviewerEducationDetails = interviewerProfile.InterviewerEducationDetails.Where(x => x.CollegeType != null).ToList();
                interviewerProfile.InterviewerProjectsDetails = interviewerProfile.InterviewerProjectsDetails.Where(x => x.NatureOfProject != null).ToList();
                interviewerProfile.InterviewerAvailabilityDetails = interviewerProfile.InterviewerAvailabilityDetails.Where(x => x.Weekday != null).ToList();
                return _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteInterviewerProfile(int UserId)
        {
            try
            {
                return _interviewerRepository.DeleteInterviewerProfile(UserId).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> AddEmployerProfile(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                return _interviewerRepository.AddEmployerProfile(interviewerProfessional).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> EditEmployerProfile(InterviewerProfessional interviewerProfessional)
        {
            try
            {
                return _interviewerRepository.EditEmployerProfile(interviewerProfessional).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> DeleteEmployerProfile(int id)
        {
            try
            {
                return _interviewerRepository.DeleteEmployerProfile(id).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        // Added to retrieve the Nick names
        public IEnumerable<string> GetNickNames(int UserId)
        {
            try
            {
                return _interviewerRepository.GetNickNames(UserId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        // Added to retrieve the EmailIds
        public IEnumerable<string> GetEmailIds(int UserId)
        {
            try
            {
                return _interviewerRepository.GetEmailIds(UserId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public InterviewerProfileVM GetInterviewerProfileById(int userid)
        {
            try
            {
                var interviewerProfile = _interviewerRepository.GetInterviewerProfileById(userid).Result;
                InterviewerProfileVM ProfileModel = interviewerProfile.FirstOrDefault();

                int ProfileId = ProfileModel.id;

                // Get all the List from DB

                List<TransInterviewerSkill> SkillList = _interviewerRepository.GetSkills(ProfileId).Result.ToList();
                List<TransInterviewerCertificate> CertificateList = _interviewerRepository.GetCertificateList(ProfileId).Result.ToList();
                List<TransInterviewerEducation> EducationList = _interviewerRepository.GetEducationList(ProfileId).Result.ToList();
                //List<InterviewerProfessional> ProfessionalList = _interviewerRepository.GetProfessionalList(ProfileId).Result.ToList();
                List<TransInterviewerProjects> ProjectsList = _interviewerRepository.GetProjectsList(ProfileId).Result.ToList();
                List<TransInterviewerAvailability> AvailabilityList = _interviewerRepository.GetAvailabilityList(ProfileId).Result.ToList();
                List<TransInterviewerAccDetails> AccDetailsList = _interviewerRepository.GetAccDetailsList(ProfileId).Result.ToList();

                // Assign the Lists to model

                ProfileModel.InterviewerSkill = SkillList;
                ProfileModel.InterviewerCertificateDetails = CertificateList;
                ProfileModel.InterviewerEducationDetails = EducationList;
                //ProfileModel.InterviewerProfessionalDetails = ProfessionalList;
                ProfileModel.InterviewerProjectsDetails = ProjectsList;
                ProfileModel.InterviewerAvailabilityDetails = AvailabilityList;
                ProfileModel.InterviewerAccDetails = AccDetailsList;

                return ProfileModel;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public InterviewerProfileVM GetInterviewerMyProfileById(int userid)
        {
            try
            {
                var interviewerProfile = _interviewerRepository.GetInterviewerProfileById(userid).Result;
                InterviewerProfileVM ProfileModel = interviewerProfile.FirstOrDefault();

                int ProfileId = ProfileModel.id;

                // Get all the List from DB

                //List<TransInterviewerSkill> SkillList = _interviewerRepository.GetSkills(ProfileId).Result.ToList();
                //List<TransInterviewerCertificate> CertificateList = _interviewerRepository.GetCertificateList(ProfileId).Result.ToList();
                //List<TransInterviewerEducation> EducationList = _interviewerRepository.GetEducationList(ProfileId).Result.ToList();
                ////List<InterviewerProfessional> ProfessionalList = _interviewerRepository.GetProfessionalList(ProfileId).Result.ToList();
                //List<TransInterviewerProjects> ProjectsList = _interviewerRepository.GetProjectsList(ProfileId).Result.ToList();
                List<TransInterviewerAvailability> AvailabilityList = _interviewerRepository.GetAvailabilityList(ProfileId).Result.ToList();
                List<TransInterviewerAccDetails> AccDetailsList = _interviewerRepository.GetAccDetailsList(ProfileId).Result.ToList();

                // Assign the Lists to model

                //ProfileModel.InterviewerSkill = SkillList;
                //ProfileModel.InterviewerCertificateDetails = CertificateList;
                //ProfileModel.InterviewerEducationDetails = EducationList;
                ////ProfileModel.InterviewerProfessionalDetails = ProfessionalList;
                //ProfileModel.InterviewerProjectsDetails = ProjectsList;
                ProfileModel.InterviewerAvailabilityDetails = AvailabilityList;
                ProfileModel.InterviewerAccDetails = AccDetailsList;

                return ProfileModel;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public InterviewerProfileVM GetInterviewerProfileByEmailId(string emailId, string profile)
        {
            try
            {
                var interviewerProfile = _interviewerRepository.GetInterviewerProfileByEmailId(emailId, profile).Result;
                InterviewerProfileVM ProfileModel = interviewerProfile.FirstOrDefault();

                int ProfileId = ProfileModel.id;

                // Get all the List from DB

                List<TransInterviewerAvailability> AvailabilityList = _interviewerRepository.GetAvailabilityList(ProfileId).Result.ToList();
                List<TransInterviewerAccDetails> AccDetailsList = _interviewerRepository.GetAccDetailsList(ProfileId).Result.ToList();

                // Assign the Lists to model


                ProfileModel.InterviewerAvailabilityDetails = AvailabilityList;
                ProfileModel.InterviewerAccDetails = AccDetailsList;

                return ProfileModel;

            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public InterviewerProfessional GetEmployerById(int id)
        {
            try
            {
                var EmployerList = _interviewerRepository.GetEmployerById(id).Result;

                return EmployerList;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public List<LevelTech> GetLevelTech()
        {
            try
            {
                return _interviewerRepository.GetLevelTech().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<ExperienceRange> GetExperienceRange()
        {
            try
            {
                return _interviewerRepository.GetExperienceRange();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<JobFunction> GetJobFunction()
        {
            try
            {
                return _interviewerRepository.GetJobFunction();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<Industry> GetIndustry()
        {
            try
            {
                return _interviewerRepository.GetIndustry();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddInterviewerMyProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
                interviewerProfile.InterviewerAvailabilityDetails = interviewerProfile.InterviewerAvailabilityDetails.Where(x => x.Weekday != null).ToList();
                //Add Method
                return _interviewerRepository.AddInterviewerMyProfile(interviewerProfile).Result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion
        #region  unapproved interviewers
        public IEnumerable<InterviewerInvites> GetInterviewerInvites(string emailId)
        {
            try
            {
                return _interviewerRepository.GetInterviewerInvites(emailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }


        public List<Meeting> GetUnapprovedInterviewerScheduledInterviews(string emailID)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _interviewerRepository.GetUnapprovedInterviewerScheduledInterviews(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        #endregion

        #region  lead interviewers
        public List<Meeting> GetOpenInterviewerAssessment(string emailId)
        {
            try
            {
                var openMeeting = _interviewerRepository.GetOpenInterviewerAssessment(emailId).Result.OrderByDescending(x => x.interviewDate).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetAssignedInterviewerAssessment(string emailID)
        {
            try
            {
                var assignedMeeting = _interviewerRepository.GetAssignedInterviewerAssessment(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate)
        {
            try
            {
                var assignedMeeting = _interviewerRepository.GetAssignedInterviewerAssessment(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
                var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == interviewDate);
                if (checkDateMeeting.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
                }
                return _interviewerRepository.AssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }

        public bool UnassignYourself(int meetingid, string email)
        {
            try
            {
                var meeting = _interviewerRepository.GetMeetingById(meetingid);

                if (_interviewerRepository.UnassignYourself(meetingid).Result)
                {
                    var hrs = _userService.GetAllHrsEmailld()?.ToList();
                    _notificationHelper.SendAlertHRForUnassignedInterview(hrs, meeting.interviewDate.ToString(), meeting.candidateEmail, email);
                    return true;
                }
                else
                {
                    return false;

                }
                //interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(meeting.Result.IRFId);
                //interviwers.Remove(email);
                //_notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.Result.interviewDate.ToString());


            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }


        #endregion
        //public async Task<InterviewReport> GetAssignedInterviewerAssessment(int interviewId, int irfId)
        //{
        //    try
        //    {
        //        var reportModel = new InterviewReport();
        //        var irfDetails = _interviewerRepository.GetInterviewerSkillChannelDetails(irfId);
        //        var levels = _meetingRespository.GetLevels();
        //        //check whether an IRF exists for the given IRFId
        //        var check = _meetingRespository.GetIRFbyId(irfId);// what is need of this method
        //        var interviewDetails = _interviewerRepository.GetInterviewerMeetingById(interviewId).Result;

        //        if (!check)
        //        {
        //            return null;
        //        }
        //        var interviewReport = _meetingRespository.GetInterviewReportById(interviewId, irfId);//need to create table for report
        //        if (interviewReport != null && interviewReport.Count() > 0)
        //        {
        //            //call Edit Feedbackform method
        //            reportModel = interviewReport.FirstOrDefault();
        //            //EditInterviewReport(reportModel);
        //        }
        //        else
        //        {
        //            var model = interviewReport.Count() > 0 ? interviewReport.FirstOrDefault() : new InterviewReport();
        //            model.IsEdited = model.InterviewId > 0;
        //            //get all values from database
        //            var levelTech = _meetingRespository.GetLevelTech();// replacement
        //            var skills = _meetingRespository.GetSkills();// use transskill table
        //            //var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
        //            var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(irfId);//use
        //            var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);//use
        //            //var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
        //            //var selectabilityDetails = _meetingRespository.GetTransSelectability(irfId);
        //            var techSkillList = new List<TechSkillReport>();
        //            //assign values to model
        //            foreach (var item in techSkillsDetails)
        //            {
        //                TechSkillReport obj = new TechSkillReport();
        //                obj.InterviewId = interviewId;
        //                obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
        //                obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
        //                obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
        //                obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
        //                obj.WeightageRequired = item.Weightage;

        //                techSkillList.Add(obj);
        //            }
        //            model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;

        //            var genSkillList = new List<SkillGeneralReport>();

        //            foreach (var item in skillGeneralDetails)
        //            {
        //                SkillGeneralReport obj = new SkillGeneralReport();
        //                obj.InterviewId = interviewId;
        //                obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
        //                obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
        //                obj.Type = item.Type;
        //                obj.Name = item.Name;
        //                obj.Value = item.Value;
        //                obj.WeightageRequired = item.Weightage;
        //                genSkillList.Add(obj);
        //            }

        //            model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)genSkillList;

        //            model.InterviewId = interviewId;
        //            model.CandidateName = interviewDetails.candidateName;
        //            model.interviewDate = interviewDetails.interviewDate.ToString();
        //            model.resumeLink = interviewDetails.resumeLink;
        //            model.IRFId = irfId;

        //            model.JobLevelName = Enum.GetName(typeof(JobLevels), interviewDetails.JobLevel);
        //            model.currentEmployerName = interviewDetails.currentEmployerName;
        //            model.expectedPerHourRate = interviewDetails.expectedPerHourRate;
        //            model.PreferredLocation = interviewDetails.preferredLocation;
        //            model.IsEdited = false;
        //            model.IsApproved = interviewDetails.IsApproved;
        //            model.IsSelected = interviewDetails.IsSelected;
        //            model.FinalComments = interviewDetails.FinalComments;
        //            model.ApprovalComments = interviewDetails.ApprovalComments;
        //            model.IsBiometricEnabled = interviewDetails.IsBiometricEnabled;
        //            model.JoiningBiometryScore = interviewDetails.JoiningBiometryScore;
        //            model.InterviewBiometryScore = interviewDetails.InterviewBiometryScore;
        //            reportModel = model;
        //        }
        //        //get candidateId
        //        reportModel.CandidateId = _meetingRespository.GetCandidateId(interviewId, irfDetails.IRFId);//use
        //        reportModel.CandidateName = interviewDetails.candidateName;
        //        reportModel.SelectabilityComments = _evaluatorRespository.GetSelectabilityComments(irfId, interviewDetails.candidateEmail);
        //        reportModel.CandidateEmail = interviewDetails.candidateEmail;
        //        reportModel.CandidateVisa = interviewDetails.CandidateVisa;
        //        reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;
        //        var selectabilityReport = _evaluatorRespository.GetSelectabilityReport(reportModel.CandidateId, irfId);
        //        if (selectabilityReport != null && selectabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
        //        {
        //            reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

        //        }
        //        else
        //        {
        //            var selectabilityDetails = _evaluatorRespository.GetTransSelectability(irfId);
        //            var selectabilityList = new List<SelectabilityEvaluatorReport>();
        //            foreach (var item in selectabilityDetails)
        //            {
        //                SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport();
        //                obj.CandidateId = reportModel.CandidateId;
        //                obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
        //                obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
        //                obj.Type = item.Type;
        //                obj.Name = item.Name;
        //                obj.Value = item.Value;
        //                obj.WeightageRequired = item.Weightage;
        //                selectabilityList.Add(obj);
        //            }

        //            reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
        //        }

        //        reportModel.JoinabilityComments = _evaluatorRespository.GetJoinabilityComments(irfId, interviewDetails.candidateEmail);
        //        var joinabilityReport = _evaluatorRespository.GetJoinabilityReport(reportModel.CandidateId, irfId);
        //        if (joinabilityReport != null && joinabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
        //        {
        //            reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

        //        }
        //        else
        //        {
        //            var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);//replace
        //            var joinabilityList = new List<JoinabilityHRReport>();
        //            foreach (var item in joinabilityDetails)
        //            {
        //                JoinabilityHRReport obj = new JoinabilityHRReport();
        //                obj.CandidateId = reportModel.CandidateId;
        //                obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
        //                obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
        //                obj.Type = item.Type;
        //                obj.Name = item.Name;
        //                obj.Value = item.Value;
        //                obj.WeightageRequired = item.Weightage;
        //                joinabilityList.Add(obj);
        //            }

        //            reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
        //        }

        //        reportModel.IRFName = irfDetails.IRFId;
        //        reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
        //        return reportModel;
        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return null;
        //    }
        //}
        public async Task<bool> AddInterviewerReport(InterviewReport reportModel)
        {
            try
            {
                var result = await _interviewerRepository.AddInterviewerReport(reportModel);
                //if (reportModel.InterviewStatus.HasValue && !reportModel.IsEdited)
                //{
                //    if (reportModel.InterviewStatus.Value == 2 || reportModel.InterviewStatus.Value == 4)
                //    {

                //        var irfQualityControllers = _irfRepository.GetTransQualityController(reportModel.IRFId);
                //        var mailIds = new List<string>();
                //        foreach (var item in irfQualityControllers)
                //        {
                //            if (item.EmailId != null)
                //            {
                //                mailIds.Add(item.EmailId);
                //            }
                //        }
                //        _notificationHelper.SendQCInterviewerMail(mailIds, reportModel.interviewDate);

                //        if (reportModel.InterviewStatus.Value == 2)
                //        {
                //            var namemodel = String.Empty;

                //            if (reportModel.CandidateName != null && reportModel.CandidateName.Length >= 2)
                //                namemodel = namemodel + reportModel.CandidateName?.ToCharArray()[0] + reportModel.CandidateName?.ToCharArray()[1];
                //            else if (reportModel.CandidateName != null && reportModel.CandidateName.Length == 1)
                //                namemodel = namemodel + reportModel.CandidateName?.ToCharArray()[0];
                //            //get curated users
                //            var curatedMailIds = _irfRepository.GetCuratedUserMailId(reportModel.IRFId);
                //            _notificationHelper.SendCuratedUserMail(curatedMailIds.ToList(), reportModel.interviewDate, reportModel.IRFName, reportModel.PrimarySkill, namemodel);
                //        }

                //    }
                //}

                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        
    }
}
