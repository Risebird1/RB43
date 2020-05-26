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

        public InterviewerService(IInterviewerRepository interviewerRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _interviewerRepository = interviewerRepository;
            _applicationErrorLogger = applicationErrorLogger;
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


    }
}
