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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile)
        {
            try
            {
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

        public async Task<InterviewerProfileVM> GetInterviewerProfileByEmailId(string emailId, string profile)
        {
            try
            {
                var interviewerProfile = await _interviewerRepository.GetInterviewerProfileByEmailId(emailId, profile);
                if (interviewerProfile != null)
                {
                    InterviewerProfileVM profileModel = interviewerProfile.FirstOrDefault();
                    // Get all the List from DB
                    profileModel.InterviewerAvailabilityDetails = _interviewerRepository.GetAvailabilityList(profileModel.id).Result.ToList();
                    profileModel.InterviewerAccDetails = _interviewerRepository.GetAccDetailsList(profileModel.id).Result.ToList();

                    return profileModel;
                }
                else return new InterviewerProfileVM { EmailId = emailId };
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
        public async Task<InterviewReport> GetInterviewerinteviewReport(int interviewId, int irfId)
        {
            try
            {
                var reportModel = new InterviewReport();
                var irfDetails = _interviewerRepository.GetInterviewerSkillChannelDetails(irfId);

                //check whether an IRF exists for the given IRFId
                var check = _interviewerRepository.GetInterviewerSkillChannelbyId(irfId);// what is need of this method
                var interviewDetails = _interviewerRepository.GetInterviewerMeetingById(interviewId).Result;//Ask

                if (!check)
                {
                    return null;
                }
                var interviewReport = _interviewerRepository.GetInterviewerReportById(interviewId, irfId);//need to create table for report
                if (interviewReport != null && interviewReport.Count() > 0)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport.FirstOrDefault();
                    EditInterviewReport(reportModel, irfId);
                }
                else
                {
                    var model = interviewReport.Count() > 0 ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //get all values from database
                    var levelTech = _meetingRespository.GetLevelTech();// replacement
                    var skills = _meetingRespository.GetSkills();// use transskill table
                    //var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
                    var techSkillsDetails = _interviewerRepository.GetSkillsbyIRFid(irfId);//use
                    //var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);//use
                    var techSkillList = new List<TechSkillReport>();
                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillReport obj = new TechSkillReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                        obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                        obj.WeightageRequired = item.Weightage;

                        techSkillList.Add(obj);
                    }
                    model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;


                    model.InterviewId = interviewId;
                    model.CandidateName = interviewDetails.candidateName;
                    model.interviewDate = interviewDetails.interviewDate.ToString();
                    model.resumeLink = interviewDetails.resumeLink;
                    model.IRFId = irfId;

                    //model.JobLevelName = Enum.GetName(typeof(JobLevels), interviewDetails.JobLevel);
                    model.currentEmployerName = interviewDetails.currentEmployerName;
                    model.expectedPerHourRate = interviewDetails.expectedPerHourRate;
                    model.PreferredLocation = interviewDetails.preferredLocation;
                    model.IsEdited = false;
                    model.IsApproved = interviewDetails.IsApproved;
                    model.IsSelected = interviewDetails.IsSelected;
                    model.FinalComments = interviewDetails.FinalComments;
                    model.ApprovalComments = interviewDetails.ApprovalComments;
                    model.IsBiometricEnabled = interviewDetails.IsBiometricEnabled;
                    model.JoiningBiometryScore = interviewDetails.JoiningBiometryScore;
                    model.InterviewBiometryScore = interviewDetails.InterviewBiometryScore;
                    reportModel = model;
                }
                var techInterviewerRecommendation = _interviewerRepository.GetInterviewerRecommendationbyinterviewid(interviewId);
                foreach (var item in techInterviewerRecommendation)
                {
                    reportModel.CompanySize = item.CompanySize;
                    reportModel.JobFunction = item.JobFunction;
                    reportModel.Industry = item.Industry;
                    reportModel.CompanyHeadquarters = item.CompanyHeadquarters;
                    reportModel.CompanyType = item.CompanyType;
                    reportModel.CompanyStage = item.CompanyStage;
                    reportModel.SkillCategory = item.SkillCategory;
                    reportModel.Yearsofexperience = item.Yearsofexperience;
                    reportModel.Senioritylevel = item.Senioritylevel;
                    reportModel.SkillNature = item.SkillNature;
                    reportModel.Domain = item.Domain;
                    reportModel.SkillIP = item.SkillIP;
                    reportModel.PerHourRateIndia = item.PerHourRateIndia;
                }
                //get candidateId
                reportModel.CandidateId = _meetingRespository.GetCandidateId(interviewId, irfDetails.IRFId);
                reportModel.CandidateName = interviewDetails.candidateName;
                reportModel.CandidateEmail = interviewDetails.candidateEmail;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;

                reportModel.IRFName = irfDetails.IRFId;
                reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                return reportModel;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        private InterviewReport EditInterviewReport(InterviewReport model, int irfid)
        {
            var techReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));

            if (techReportCollection.Count() == 0)
            {
                var techSkillsDetails = _interviewerRepository.GetSkillsbyIRFid(irfid);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = _meetingRespository.GetLevelTech();
                var skills = _meetingRespository.GetSkills();

                //assign values to model
                foreach (var item in techSkillsDetails)
                {
                    TechSkillReport obj = new TechSkillReport();
                    obj.InterviewId = model.InterviewId;
                    obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                    obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                    obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                    obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                    obj.WeightageRequired = item.Weightage;

                    techSkillList.Add(obj);
                }
                model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;
            }
            else
            {
                model.TechSkillReportCollection = techReportCollection;
            }
            //Recommonded
            //model.QuestionsCollection = (IList<QuestionsDiscussed>)(_meetingRespository.GetQuestionsDiscussed(model.InterviewId));
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _interviewerRepository.GetInterviewerMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            return model;
        }
        public async Task<bool> AddInterviewerReport(InterviewReport reportModel)
        {
            try
            {
                var result = await _interviewerRepository.AddInterviewerReport(reportModel);

                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IEnumerable<SkillComment> GetTechSkillComments(int IRFid)
        {

            try
            {
                var comments = _interviewerRepository.GetTechSkillComments(IRFid);

                if (comments == null)
                    return null;
                else
                    return comments;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<AssignmentVM> GetInterviewerAssessment(string emailid)
        {
            try
            {
                var comments = _interviewerRepository.GetInterviewerAssessment(emailid);
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string AddAssessment(AssignmentVM _assessment)
        {
            try
            {
                var comments = _interviewerRepository.AddAssessment(_assessment);
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public IEnumerable<AssignmentVM> FetchAssessmentGroups()
        {
            try
            {
                var comments = _interviewerRepository.FetchAssessmentGroups();
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string ChangeStatusAssessment(int assessmentid, int status)
        {
            try
            {
                var comments = _interviewerRepository.ChangeStatusAssessment(assessmentid, status);
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public string UpdateAssessment(AssignmentVM _assessment)
        {
            try
            {
                var comments = _interviewerRepository.UpdateAssessment(_assessment);
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
    }
}
