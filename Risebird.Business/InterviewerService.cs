using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class InterviewerService : IInterviewerService
    {
        private readonly IInterviewerRepository _interviewerRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IUserService _userService;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IActivityTrackerRepository _activityTrackerRepository;

        public InterviewerService(IInterviewerRepository interviewerRepository, IApplicationErrorLogger applicationErrorLogger, IUserService userService, IMeetingRespository meetingRespository, INotificationHelper notificationHelper, IPasswordHelper passwordHelper, IIRFRepository irfRepository, IActivityTrackerRepository activityTrackerRepository)
        {
            _interviewerRepository = interviewerRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _userService = userService;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
            _passwordHelper = passwordHelper;
            _irfRepository = irfRepository;
            _activityTrackerRepository = activityTrackerRepository;
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
        //public async Task<bool> AddInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList)
        //{
        //    try
        //    {
        //        UserDetails userDetails = new UserDetails
        //        {
        //            FirstName = interviewerProfile.FirstName.Replace(" ", ""),
        //            LastName = interviewerProfile.LastName.Replace(" ", ""),
        //            EmailId = interviewerProfile.EmailId.Replace(" ", "")
        //        };
        //        interviewerProfile.UserPassword = await _passwordHelper.GenerateUserPassword(userDetails);
        //        var NickNamesList = GetNickNames(0);
        //        if (string.IsNullOrEmpty(interviewerProfile.NickName))
        //        {
        //            var lastnamelength = !string.IsNullOrEmpty(interviewerProfile.LastName) ? interviewerProfile.LastName.Length : 0;
        //            var NickName = (!string.IsNullOrEmpty(interviewerProfile.FirstName) ? interviewerProfile.FirstName.Substring(0, 2).Trim() : "Ri") + "" + (!string.IsNullOrEmpty(interviewerProfile.LastName) ? lastnamelength >= 2 ? interviewerProfile.LastName.Substring(0, 2).Trim().Replace(".", "") : interviewerProfile.LastName.Substring(0, 1).Trim().Replace(".", "") : "se") + "" + (!string.IsNullOrEmpty(interviewerProfile.MobileNumber) ? interviewerProfile.MobileNumber.Substring(6, 4) : "1145");
        //            int i = 0;
        //            while (i <= 4)
        //            {
        //                if (NickNamesList.Contains(NickName))
        //                {
        //                    NickName = NickName + (!string.IsNullOrEmpty(interviewerProfile.MobileNumber) ? interviewerProfile.MobileNumber[interviewerProfile.MobileNumber.Length - 1].ToString() : "5");
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //                i++;
        //            }
        //            interviewerProfile.NickName = NickName;
        //        }
        //        interviewerProfile.IndiaProductRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate * 1.5);
        //        interviewerProfile.OutsideIndiaProductRate = Convert.ToInt32(interviewerProfile.OutsideIndiaServiceRate * 1.5);
        //        return _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile, loggedInUser).Result;
        //    }
        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}

        public bool AddInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList)
        {
            assessmentGroupList = string.Empty;
            try
            {
                if (interviewerProfile.AssessmentGroupArray != null)
                {
                    interviewerProfile.AssessmentGroupDifference = string.Join(",", interviewerProfile.AssessmentGroupArray);
                }
                UserDetails userDetails = new UserDetails
                {
                    FirstName = interviewerProfile.FirstName.Replace(" ", ""),
                    LastName = interviewerProfile.LastName.Replace(" ", ""),
                    EmailId = interviewerProfile.EmailId.Replace(" ", "")
                };
                interviewerProfile.UserPassword = _passwordHelper.GenerateUserPassword(userDetails).Result;
                var NickNamesList = GetNickNames(0);
                if (string.IsNullOrEmpty(interviewerProfile.NickName))
                {
                    var lastnamelength = !string.IsNullOrEmpty(interviewerProfile.LastName) ? interviewerProfile.LastName.Length : 0;
                    var NickName = (!string.IsNullOrEmpty(interviewerProfile.FirstName) ? interviewerProfile.FirstName.Substring(0, 2).Trim() : "Ri") + "" + (!string.IsNullOrEmpty(interviewerProfile.LastName) ? lastnamelength >= 2 ? interviewerProfile.LastName.Substring(0, 2).Trim().Replace(".", "") : interviewerProfile.LastName.Substring(0, 1).Trim().Replace(".", "") : "se") + "" + (!string.IsNullOrEmpty(interviewerProfile.MobileNumber) ? interviewerProfile.MobileNumber.Substring(6, 4) : "1145");
                    int i = 0;
                    while (i <= 4)
                    {
                        if (NickNamesList.Contains(NickName))
                        {
                            NickName = NickName + (!string.IsNullOrEmpty(interviewerProfile.MobileNumber) ? interviewerProfile.MobileNumber[interviewerProfile.MobileNumber.Length - 1].ToString() : "5");
                        }
                        else
                        {
                            break;
                        }
                        i++;
                    }
                    interviewerProfile.NickName = NickName;
                }
                //interviewerProfile.IndiaProductRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate * 1.5);
                //interviewerProfile.OutsideIndiaProductRate = Convert.ToInt32(interviewerProfile.OutsideIndiaServiceRate * 1.5);
                interviewerProfile.IndiaProductRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate * 1.5);
                interviewerProfile.OutsideIndiaServiceRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate + 150);
                interviewerProfile.OutsideIndiaProductRate = Convert.ToInt32(interviewerProfile.OutsideIndiaServiceRate * 1.5);

                var result = _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile, loggedInUser, out assessmentGroupList);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateInterviewer;
                    activityTrackerModel.Particulars = "Interviewer: " + interviewerProfile.EmailId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Add Interviewer";
                    activityTrackerModel.ResourceUrl = interviewerProfile.ResourceUrl;
                    _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        //public async Task<bool> EditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser)
        //{
        //    try
        //    {
        //        //Edit Method
        //        interviewerProfile.InterviewerEducationDetails = interviewerProfile.InterviewerEducationDetails.Where(x => x.CollegeType != null).ToList();
        //        interviewerProfile.InterviewerProjectsDetails = interviewerProfile.InterviewerProjectsDetails.Where(x => x.NatureOfProject != null).ToList();
        //        interviewerProfile.InterviewerAvailabilityDetails = interviewerProfile.InterviewerAvailabilityDetails.Where(x => x.Weekday != null).ToList();
        //        interviewerProfile.IndiaProductRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate * 1.5);
        //        interviewerProfile.OutsideIndiaProductRate = Convert.ToInt32(interviewerProfile.OutsideIndiaServiceRate * 1.5);
        //        return _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile, loggedInUser).Result;
        //    }
        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}
        public bool EditInterviewerProfile(InterviewerProfileVM interviewerProfile, string loggedInUser, out string assessmentGroupList)
        {
            assessmentGroupList = string.Empty;
            try
            {
                List<string> assessmentGroupDiffArray = new List<string>();
                List<string> removedAssessmentGroupArray = new List<string>();
                foreach (var item in interviewerProfile.AssessmentGroupArray)
                {
                    if (interviewerProfile.AssessmentGroupArrayOld == null
                        || !interviewerProfile.AssessmentGroupArrayOld.Contains(item))
                    {
                        assessmentGroupDiffArray.Add(item);
                    }
                }
                if (assessmentGroupDiffArray != null && assessmentGroupDiffArray.Count() > 0)
                {
                    interviewerProfile.AssessmentGroupDifference = string.Join(",", assessmentGroupDiffArray);
                }
                if (interviewerProfile.AssessmentGroupArrayOld != null)
                {
                    foreach (var item in interviewerProfile.AssessmentGroupArrayOld)
                    {
                        if (!interviewerProfile.AssessmentGroupArray.Contains(item))
                        {
                            removedAssessmentGroupArray.Add(item);

                        }
                    }
                }
                if (removedAssessmentGroupArray != null && removedAssessmentGroupArray.Count() > 0)
                {
                    interviewerProfile.RemovedAssessmentGroups = string.Join(",", removedAssessmentGroupArray);
                }
                //Edit Method
                interviewerProfile.InterviewerEducationDetails = interviewerProfile.InterviewerEducationDetails.Where(x => x.CollegeType != null).ToList();
                interviewerProfile.InterviewerProjectsDetails = interviewerProfile.InterviewerProjectsDetails.Where(x => x.NatureOfProject != null).ToList();
                interviewerProfile.InterviewerAvailabilityDetails = interviewerProfile.InterviewerAvailabilityDetails.Where(x => x.Weekday != null).ToList();
                interviewerProfile.IndiaProductRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate * 1.5);
                interviewerProfile.OutsideIndiaServiceRate = Convert.ToInt32(interviewerProfile.IndiaServiceRate + 150);
                interviewerProfile.OutsideIndiaProductRate = Convert.ToInt32(interviewerProfile.OutsideIndiaServiceRate * 1.5);
                var result = _interviewerRepository.AddOrEditInterviewerProfile(interviewerProfile, loggedInUser, out assessmentGroupList);
                if (result)
                {
                    var activityTrackerModel = new ActivityTrackerVM();
                    activityTrackerModel.ActivityType = (int)ActivityTypes.UpdateInterviewer;
                    activityTrackerModel.Particulars = "Interviewer: " + interviewerProfile.EmailId;
                    activityTrackerModel.PerformedBy = loggedInUser;
                    activityTrackerModel.Summary = "Update Interviewer";
                    activityTrackerModel.ResourceUrl = interviewerProfile.ResourceUrl;
                    _activityTrackerRepository.AddActivity(activityTrackerModel);
                }
                return result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
                TransInterviewerAccDetails AccDetailsList = _interviewerRepository.GetAccDetailsList(ProfileId).Result;

                // Assign the Lists to model

                ProfileModel.InterviewerSkill = SkillList;
                ProfileModel.InterviewerCertificateDetails = CertificateList;
                ProfileModel.InterviewerEducationDetails = EducationList;
                //ProfileModel.InterviewerProfessionalDetails = ProfessionalList;
                ProfileModel.InterviewerProjectsDetails = ProjectsList;
                ProfileModel.InterviewerAvailabilityDetails = AvailabilityList;
                ProfileModel.InterviewerAccDetails = AccDetailsList;
                ProfileModel.IsEdit = true;
                if (!string.IsNullOrEmpty(ProfileModel.AssessmentGroupId))
                {
                    ProfileModel.AssessmentGroupArray = ProfileModel.AssessmentGroupId.Split(',');
                    ProfileModel.AssessmentGroupArrayOld = ProfileModel.AssessmentGroupArray;
                }
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
                TransInterviewerAccDetails AccDetailsList = _interviewerRepository.GetAccDetailsList(ProfileId).Result;

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
                    profileModel.InterviewerAccDetails = _interviewerRepository.GetAccDetailsList(profileModel.id).Result;
                    profileModel.InterviewerAccDetails.AccountNumber = string.IsNullOrEmpty(profileModel.InterviewerAccDetails.AccountNumber) ? "" : profileModel.InterviewerAccDetails.AccountNumber.Trim();
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
                if (checkDateMeeting.Any())
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
                    var hrs = _userService.GetAllHrsEmailld();
                    var Skillchannelid = meeting.IRFId;
                    var client = _interviewerRepository.FetchClientName(meeting.IRFId);
                    //_notificationHelper.SendAlertHRForUnassignedInterview(hrs, meeting.interviewDate.ToString(), meeting.candidateEmail, email, Skillchannelid, client);
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
                //var check = _interviewerRepository.GetInterviewerSkillChannelbyId(irfId);// what is need of this method
                var interviewDetails = _interviewerRepository.GetInterviewerMeetingById(interviewId).Result;//Ask

                if (irfDetails == null)
                {
                    return null;
                }
                var interviewReport = await _interviewerRepository.GetInterviewerReportById(interviewId, irfId);//need to create table for report
                if (interviewReport != null)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport;
                    await EditInterviewReport(reportModel, irfId);
                }
                else
                {
                    var model = interviewReport != null ? interviewReport : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //get all values from database
                    var levelTech = await _meetingRespository.GetLevelTech();// replacement
                    var skills = await _meetingRespository.GetSkills();// use transskill table
                    //var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
                    var techSkillsDetails = _interviewerRepository.GetSkillsbyIRFid(irfId);//use
                    //var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);//use
                    var techSkillList = new List<TechSkillReport>();
                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillReport obj = new TechSkillReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name;
                        obj.RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id;
                        obj.SkillName = skills.FirstOrDefault(x => x.id == item.SkillId)?.Name;
                        obj.SkillId = skills.FirstOrDefault(x => x.id == item.SkillId).id;
                        obj.WeightageRequired = item.Weightage;

                        techSkillList.Add(obj);
                    }
                    model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;


                    model.InterviewId = interviewId;
                    model.CandidateName = interviewDetails.candidateName;
                    model.interviewDate = interviewDetails.interviewDate.ToString();
                    model.resumeLink = interviewDetails.resumeLink;
                    model.encodedresumeLink = interviewDetails.encodedresumeLink;
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
                var techInterviewerRecommendation = await _interviewerRepository.GetInterviewerRecommendationbyinterviewid(interviewId);

                if (techInterviewerRecommendation != null)
                {
                    reportModel.CompanySize = techInterviewerRecommendation.CompanySize;
                    reportModel.JobFunction = techInterviewerRecommendation.JobFunction;
                    reportModel.Industry = techInterviewerRecommendation.Industry;
                    reportModel.Country = techInterviewerRecommendation.Country;
                    reportModel.CompanyType = techInterviewerRecommendation.CompanyType;
                    reportModel.CompanyStage = techInterviewerRecommendation.CompanyStage;
                    reportModel.SkillCategory = techInterviewerRecommendation.SkillCategory;
                    reportModel.Yearsofexperience = techInterviewerRecommendation.Yearsofexperience;
                    reportModel.Senioritylevel = techInterviewerRecommendation.Senioritylevel;
                    reportModel.SkillNature = techInterviewerRecommendation.SkillNature;
                    reportModel.Domain = techInterviewerRecommendation.Domain;
                    reportModel.DomainName = techInterviewerRecommendation.DomainName;
                    reportModel.SkillIP = techInterviewerRecommendation.SkillIP;
                    reportModel.PerHourRateOutside = techInterviewerRecommendation.PerHourRateOutside;
                    reportModel.PerHourRateIndia = techInterviewerRecommendation.PerHourRateIndia;
                    reportModel.ProfileType = techInterviewerRecommendation.ProfileType;
                    reportModel.CompentencyLevel = techInterviewerRecommendation.CompentencyLevel;
                    reportModel.Country = techInterviewerRecommendation.Country;
                    if (!string.IsNullOrEmpty(techInterviewerRecommendation.SkillCategory))
                    {
                        reportModel.SkillCategoryArray = techInterviewerRecommendation.SkillCategory.Split(',');

                    }
                }


                //get candidateId

                reportModel.CandidateId = _interviewerRepository.GetCandidateId(interviewId, irfDetails.ISCId);
                reportModel.CandidateName = interviewDetails.candidateName;
                reportModel.CandidateEmail = interviewDetails.candidateEmail;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;

                reportModel.IRFName = irfDetails.IRFId;
                //reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                reportModel.PrimarySkill = irfDetails.PrimarySkill;
                reportModel.InterviewerProfileId = interviewDetails.InterviewerProfileId;
                return reportModel;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        private async Task<InterviewReport> EditInterviewReport(InterviewReport model, int irfid)
        {
            var techReportCollection = (IList<TechSkillReport>)(_interviewerRepository.GetTechSkillReport(model.InterviewId));

            if (techReportCollection.Count() == 0)
            {
                var techSkillsDetails = _interviewerRepository.GetSkillsbyIRFid(irfid);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = await _meetingRespository.GetLevelTech();
                var skills = await _meetingRespository.GetSkills();

                //assign values to model
                foreach (var item in techSkillsDetails)
                {
                    TechSkillReport obj = new TechSkillReport();
                    obj.InterviewId = model.InterviewId;
                    obj.RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name;
                    obj.RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id;
                    obj.SkillName = skills.FirstOrDefault(x => x.id == item.SkillId)?.Name;
                    obj.SkillId = skills.FirstOrDefault(x => x.id == item.SkillId).id;
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
            model.InterviewerQuestionsCollection = (IList<InterviewerQuestionsDiscussed>)(await _interviewerRepository.GetQuestionsDiscussed(model.InterviewId));
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _interviewerRepository.GetInterviewerMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.encodedresumeLink = interviewMeetingsDetails.encodedresumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            return model;
        }
        public async Task<bool> AddInterviewerReport(InterviewReport reportModel)
        {
            try
            {
                if (reportModel.SkillCategoryArray != null)
                {
                    reportModel.SkillCategory = string.Join(',', reportModel.SkillCategoryArray);
                }
                else
                {
                    reportModel.SkillCategory = string.Empty;
                }

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

        public async Task<IEnumerable<string>> GetAllTagsFromIRF()
        {
            try
            {
                return await _interviewerRepository.GetAllTagsFromIRF();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewReport> GetInterviewerRecommendationDetails(int Userid)
        {
            try
            {
                var comments = await _interviewerRepository.GetInterviewerRecommendationDetails(Userid);
                return comments;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> InterviewerRecommendation(InterviewReport reportModel)
        {
            try
            {
                reportModel.CompanySize = string.Empty;
                reportModel.CompanyStage = string.Empty;
                reportModel.Yearsofexperience = string.Empty;
                reportModel.Senioritylevel = string.Empty;
                reportModel.AssessmentGroupId = string.Empty;
                if (reportModel.SkillCategoryArray != null)
                {
                    reportModel.SkillCategory = string.Join(',', reportModel.SkillCategoryArray);
                }
                else
                {
                    reportModel.SkillCategory = string.Empty;
                }
                if (reportModel.CompanySizeArray != null)
                {
                    reportModel.CompanySize = string.Join(',', reportModel.CompanySizeArray);
                }
                if (reportModel.CompanyStageArray != null)
                {
                    reportModel.CompanyStage = string.Join(',', reportModel.CompanyStageArray);
                }
                if (reportModel.YearsofexperienceArray != null)
                {
                    reportModel.Yearsofexperience = string.Join(',', reportModel.YearsofexperienceArray);
                }
                if (reportModel.SenioritylevelArray != null)
                {
                    reportModel.Senioritylevel = string.Join(',', reportModel.SenioritylevelArray);
                }
                if (reportModel.AssessmentGroupIdArray != null)
                {
                    reportModel.AssessmentGroupId = string.Join(',', reportModel.AssessmentGroupIdArray);
                }
                if (!string.IsNullOrEmpty(reportModel.PerHourRateIndia))
                {
                    reportModel.IndiaProductRate = Convert.ToInt32((Convert.ToInt32(reportModel.PerHourRateIndia) * 1.5));

                }
                if (!string.IsNullOrEmpty(reportModel.PerHourRateOutside))
                {
                    reportModel.OutsideIndiaProductRate = Convert.ToInt32((Convert.ToInt32(reportModel.PerHourRateOutside) * 1.5));

                }
                var result = await _interviewerRepository.InterviewerRecommendation(reportModel);
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<TransInterviewerAccDetails> fetchBankDetails(int interviewerid)
        {
            try
            {

                var result = await _interviewerRepository.fetchBankDetails(interviewerid);

                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public async Task<List<TransInterviewerAvailability>> fetchAvailDetails(int interviewerid)
        {
            try
            {
                var result = await _interviewerRepository.fetchAvailDetails(interviewerid);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetUserRolebyEmailid(string Emailid)
        {
            try
            {
                return _interviewerRepository.GetUserRolebyEmailid(Emailid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<List<InterviewerRating>> fetchClientFeedbackbyInterviewer(string emailid, string type)
        {
            try
            {
                var result = await _interviewerRepository.fetchClientFeedbackbyInterviewer(emailid, type);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<CommonInterviewDetailsVM>> FetchAllInterviewerallUpcomingInterview(string type)
        {
            try
            {
                var result = await _interviewerRepository.FetchAllInterviewerallUpcomingInterview(type);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public Task<List<string>> FetchAllPasswordResetDetails(int noofdays)
        {
            try
            {
                var comments = _interviewerRepository.FetchAllPasswordResetDetails(noofdays);
                return comments;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<TdsCertificateReport>> FetchTdsCertificatesbyEmailid(string Emailid)
        {
            try
            {
                var result = await _interviewerRepository.FetchTdsCertificatesbyEmailid(Emailid);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddTdsCertificates(TdsCertificateReport model)
        {
            try
            {
                return _interviewerRepository.AddTdsCertificates(model);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public TdsCertificateReport FetchTdsCertificatebyid(int certificateid)
        {
            try
            {
                return _interviewerRepository.FetchTdsCertificatebyid(certificateid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFInterviewerMappingVM>> IRFInterviewerMappingByInterviewerid(string emailid)
        {
            try
            {
                return await _interviewerRepository.IRFInterviewerMappingByInterviewerid(emailid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<IRFInterviewerMembers>> GetAllInterviewerMembers()
        {
            try
            {
                return await _interviewerRepository.GetAllInterviewerMembers();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateAllIntervieweravailbilityStatus(int status, string uniqueid)
        {
            try
            {
                return _interviewerRepository.UpdateAllIntervieweravailbilityStatus(status, uniqueid);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<CommonInterviewDetailsVM>> FetchAllStaffingAgencyAndClientHRwithTime(DateTime stdt, DateTime etdt)
        {
            try
            {
                var result = await _interviewerRepository.FetchAllStaffingAgencyAndClientHRwithTime(stdt, etdt);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IEnumerable<ClientFeedDataVM>> GetClientFeedForInterviewer(string interviewerEmail, DateTime? postWhichDate, int? interviewId)
        {
            try
            {
                return await _interviewerRepository.GetClientFeedForInterviewer(interviewerEmail, postWhichDate, interviewId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> SendInviteCode(CodeInviteVM codeInviteVM, string loggedinUser)
        {
            try
            {
                return await _interviewerRepository.SendInviteCode(codeInviteVM, loggedinUser);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<CodeInviteVM>> GetAllIntviteDetails()
        {
            try
            {
                return await _interviewerRepository.GetAllIntviteDetails();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<CodeInviteReportVM> CheckCodeConsoleReport(string inviteid)
        {
            try
            {
                return await _interviewerRepository.CheckCodeConsoleReport(inviteid);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> AddCodeConsoleReport(CodeInviteReportVM model)
        {
            try
            {
                return await _interviewerRepository.AddCodeConsoleReport(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> AddCodeConsoleReportDetails(CodeInviteReportVM model)
        {
            try
            {
                return await _interviewerRepository.AddCodeConsoleReportDetails(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> DeleteAllHistorybefore90days()
        {
            try
            {
                var comments = await _interviewerRepository.DeleteAllHistorybefore90days();
                return comments;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> AddInterviewerAssesssmentDetails(int Assessmentid, string emailid)
        {
            try
            {
                return await _interviewerRepository.AddInterviewerAssesssmentDetails(Assessmentid, emailid);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<SkillChannelDetailsForSME>> GetSkillChannelsAssignedToSME(string loggedInInterviewer)
        {
            try
            {
                return await _interviewerRepository.GetSkillChannelsAssignedToSME(loggedInInterviewer);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IRFVm> GetSkillChannelDetailsForSMEByIdToUpdate(int interviewerProfileId, int irfId)
        {
            try
            {
                IRFVm model = await _interviewerRepository.GetSkillChannelDetailsForSMEByIdToUpdate(irfId, interviewerProfileId);
                if (model != null)
                {
                    model.SkillCollection = (IList<TransSkill>)(await _irfRepository.GetSkillsbyIRFid(irfId));
                    //model.LevelTech = new SelectList(await _irfRepository.GetLevelTech(), "id", "Name");
                    //model.SkillsList = new SelectList(await _irfRepository.GetSkills(), "id", "Name");
                }
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        public async Task<bool> UpdateSkillChannelDetailsBySME(IRFVm model, int interviewerProfileId)
        {
            try
            {
                return await _interviewerRepository.UpdateSkillChannelDetailsBySME(model, interviewerProfileId);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> SaveSMEAudio(int irfId, int interviewerProfileId, string audioUrl)
        {
            try
            {
                return await _interviewerRepository.SaveSMEAudio(irfId, interviewerProfileId, audioUrl);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> UpdateInterviewerInterest(int status, string uniqueid)
        {
            try
            {
                return await _interviewerRepository.UpdateInterviewerInterest(status, uniqueid);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<bool> SaveInterviewerResume(InterviewerProfileVM model)
        {
            try
            {
                return await _interviewerRepository.SaveInterviewerResume(model);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<IEnumerable<TransInterviewer>> GetSkillChannelInterviewersToVerify(int irfId)
        {
            try
            {
                return await _irfRepository.GetTransInterviewer(irfId);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateVerifiedInterviewers(IList<TransInterviewer> interviewers, string loggedInSME)
        {
            try
            {
                //var verifiedInterviewers = interviewers.Where(x => x.IsSMEVerified);
                return await _interviewerRepository.UpdateVerifiedInterviewers(interviewers?.ToList(), loggedInSME);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> CheckIfSME(string loggedInUser)
        {
            try
            {
                return await _interviewerRepository.CheckIfSME(loggedInUser);

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<List<ClientIRFsFeedbackVM>> FetchClientIRFFeedbackList()
        {
            try
            {
                var result = await _interviewerRepository.FetchClientIRFFeedbackList();
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewerProfileVM> GetInterviewerFlagDetails(string intervieweremailid)
        {
            try
            {
                return await _interviewerRepository.GetInterviewerFlagDetails(intervieweremailid);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> UpdateInterviewerFlag(string Emailid, bool? LeadInterviewer, bool? IsApproved, bool? IsActive)
        {
            try
            {
                return await _interviewerRepository.UpdateInterviewerFlag(Emailid, LeadInterviewer, IsApproved, IsActive);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<InterviewerStatsVM>> GetInterviewerStatsAndRatings(int? irfId = null, string clientEmailId = null)
        {
            try
            {
                return await _interviewerRepository.GetInterviewerStatsAndRatings(irfId, clientEmailId);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> DeactivateSkillChannelInterviewer(int irfId, string interviewerEmail)
        {
            try
            {
                return await _interviewerRepository.DeactivateSkillChannelInterviewer(irfId, interviewerEmail);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<IEnumerable<StatsDetailsVM>> GetInterviewerStatsAndRatingsDetails(int interviewerUserId, string type, int? irfId = null, string clientEmailId = null)
        {
            try
            {
                return await _interviewerRepository.GetInterviewerStatsAndRatingsDetails(interviewerUserId, type, irfId, clientEmailId);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddSuspiciousTypeService(SuspiciousInterviewerVM model)
        {
            try
            {
                await _interviewerRepository.AddSuspiciousType(model);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> SaveInterviewerEmploymentVerification(InterviewerProfileVM _interviewer)
        {
            try
            {
                return await _interviewerRepository.SaveInterviewerEmploymentVerification(_interviewer);
            }
            catch (SqlTypeException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
