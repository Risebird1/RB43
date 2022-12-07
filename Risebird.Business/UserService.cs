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
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IPasswordHelper _passwordHelper;
        public UserService(IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger, IPasswordHelper passwordHelper)
        {
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _passwordHelper = passwordHelper;
        }

        public List<Menu> GetMenuBasedOnUserEmailld(string emailId, string sessionId = null)
        {
            try
            {
                return _userRepository.GetMenuBasedOnUserEmailld(emailId, sessionId).ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> CheckMultiSession(string emailId)
        {
            try
            {
                return _userRepository.CheckMultiSession(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return true;
            }
        }

        public async Task<bool> ClearSessionForOthersUsers(string emailId)
        {
            try
            {
                return _userRepository.ClearSessionForOthersUsers(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #region User Service Functions

        public List<UserList> GetRisebirdUsers()
        {
            try
            {
                return _userRepository.GetRisebirdUsers().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public UserList GetUser(int id)
        {
            try
            {
                var user = _userRepository.GetUser(id);
                var userClients = _userRepository.GetUserClients(id);
                if (user == null)
                {
                    return null;
                }
                else
                {
                    user.Result.ClientsArray = userClients.ToArray();
                    return user.Result;
                }
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddUser(UserList user)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = user.FirstName.Replace(" ", ""),
                    LastName = user.LastName.Replace(" ", ""),
                    EmailId = user.EmailId.Replace(" ", "")
                };
                user.UserPassword = _passwordHelper.GenerateUserPassword(userDetails, false).Result;
                return _userRepository.AddUser(user).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteUser(int id)
        {
            try
            {
                return _userRepository.DeleteUser(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool UpdateUser(UserList user)
        {
            try
            {
                return _userRepository.UpdateUser(user).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion

        #region Client User Service Functions

        public List<ClientUserVM> GetClientUsers(string EmailId)
        {
            try
            {
                return _userRepository.GetClientUsers(EmailId).ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public ClientUserVM GetClientUser(int id)
        {
            try
            {
                var clientUser = _userRepository.GetClientUser(id);

                if (clientUser == null)
                    return null;
                else
                    return clientUser.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddClientUser(ClientUserVM clientUser)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = clientUser.FirstName.Replace(" ", ""),
                    LastName = clientUser.LastName.Replace(" ", ""),
                    EmailId = clientUser.EmailId.Replace(" ", "")
                };
                var isPasswordReset = _userRepository.GetClientPasswordResetEnableStatus(clientUser.SelectedClient.Value).Result;
                //clientUser.UserPassword = GenerateUserPassword(clientUser).Result;
                // TODO for Prashant, Please call method to fetch PasswordReswet Flag and pass it below
                clientUser.UserPassword = _passwordHelper.GenerateUserPassword(userDetails, isPasswordReset).Result;
                return _userRepository.AddClientUser(clientUser).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        //private async Task<string> GenerateUserPassword(ClientUserVM clientUser)
        //{
        //    Random rand = new Random(); // <-- Make this static somewhere

        //    const int maxValue = 999;
        //    string number = rand.Next(maxValue + 1).ToString("D3");
        //    var userobj = new UserList
        //    {
        //        FirstName = clientUser.FirstName,
        //        LastName = clientUser.LastName,
        //        EmailId = clientUser.EmailId,
        //        UserPassword = clientUser.FirstName.ToUpper() + "@" + number
        //    };
        //    if( await _b2CClientService.CreateUser(userobj))
        //    {
        //        return userobj.UserPassword;
        //    }
        //    return string.Empty;
        //}
        //public async Task<string> GenerateUserPassword(UserDetails user)
        //{
        //    Random rand = new Random(); // <-- Make this static somewhere

        //    const int maxValue = 999;
        //    string number = rand.Next(maxValue + 1).ToString("D3");
        //    var userobj = new UserList
        //    {
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        EmailId = user.EmailId,
        //        UserPassword = user.FirstName.ToUpper() + "@" + number
        //    };
        //    if (await _b2CClientService.CreateUser(userobj))
        //    {
        //        return userobj.UserPassword;
        //    }
        //    return string.Empty;
        //}

        public bool DeleteClientUser(int id)
        {
            try
            {
                return _userRepository.DeleteClientUser(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool UpdateClientUser(ClientUserVM clientUser)
        {
            try
            {
                return _userRepository.UpdateClientUser(clientUser).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> ResetNavigation(int userId)
        {
            try
            {
                return await _userRepository.ResetNavigation(userId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        #endregion

        #region Interviewer Service Functions

        public List<InterviewerVM> GetInterviewers()
        {
            try
            {
                return _userRepository.GetInterviewers().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<InterviewerVM> GetInterviewersDetails()
        {
            try
            {
                return _userRepository.GetInterviewersDetails().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddInterviewer(InterviewerVM interviewer)
        {
            try
            {
                return _userRepository.AddInterviewer(interviewer).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public InterviewerVM GetInterviewer(int id)
        {
            try
            {
                var interviewer = _userRepository.GetInterviewer(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<InterviewerVM> GetInterviewerVideos(string Emailid)
        {
            try
            {
                var interviewer = _userRepository.GetInterviewerVideos(Emailid);

                if (interviewer == null)
                    return null;
                else
                    return interviewer.ToList();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public MeetingVM GetMeetingbyid(int id)
        {
            try
            {
                var interviewer = _userRepository.GetMeetingbyid(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public bool UpdateInterviewer(InterviewerVM interviewer)
        {
            try
            {
                return _userRepository.UpdateInterviewer(interviewer).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteInterviewer(int id)
        {
            try
            {
                return _userRepository.DeleteInterviewer(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion

        #region QCInterviewer Service Functions

        public List<QCInterviewerVM> GetQCInterviewers()
        {
            try
            {
                return _userRepository.GetQCInterviewers().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddQCInterviewer(QCInterviewerVM qcinterviewer)
        {
            try
            {
                return _userRepository.AddQCInterviewer(qcinterviewer).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public QCInterviewerVM GetQCInterviewer(int id)
        {
            try
            {
                var interviewer = _userRepository.GetQCInterviewer(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateQCInterviewer(QCInterviewerVM qcInterviewer)
        {
            try
            {
                return _userRepository.UpdateQCInterviewer(qcInterviewer).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteQCInterviewer(int id)
        {
            try
            {
                return _userRepository.DeleteQCInterviewer(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        #endregion

        public IEnumerable<string> GetAllHrsEmailld()
        {
            try
            {
                return _userRepository.GetAllHrsEmailld();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public List<ResumeEvaluator> GetResumeEvaluators()
        {
            try
            {
                return _userRepository.GetResumeEvaluators().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                return _userRepository.AddResumeEvaluator(resumeEvaluator).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public ResumeEvaluator GetResumeEvaluator(int id)
        {
            try
            {
                return _userRepository.GetResumeEvaluator(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool EditResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                return _userRepository.EditResumeEvaluator(resumeEvaluator);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteResumeEvaluator(int userId)
        {
            try
            {
                return _userRepository.DeleteResumeEvaluator(userId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        public List<Recruiter> GetRecruiters()
        {
            try
            {
                if (_userRepository.GetRecruiters() != null)
                    return _userRepository.GetRecruiters().ToList();
                else
                    return new List<Recruiter>();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddRecruiter(Recruiter recruiter)
        {
            try
            {
                return _userRepository.AddRecruiter(recruiter).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public Recruiter GetRecruiter(int id)
        {
            try
            {
                return _userRepository.GetRecruiter(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool EditRecruiter(Recruiter recruiter)
        {
            try
            {
                return _userRepository.EditRecruiter(recruiter).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool DeleteRecruiter(int userId)
        {
            try
            {
                return _userRepository.DeleteRecruiter(userId).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public void UpdateUserSigInDetails(string emailId)
        {
            try
            {
                _userRepository.UpdateUserSigInDetails(emailId);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }
        }

        public string GetCurrenttheme(string emailId)
        {
            try
            {
                return _userRepository.GetCurrenttheme(emailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<AssignmentVM> GetInterviewerEntitle()
        {
            try
            {
                return _userRepository.GetInterviewerEntitle().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<AssignmentVM> InterviewerEntitleDetails(int id)
        {
            try
            {
                return _userRepository.InterviewerEntitleDetails(id).ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetCurrentuserImage(string emailId)
        {
            try
            {
                return _userRepository.GetCurrentuserImage(emailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string FetchWhatsAppNumberByEmail(string Emailid)
        {
            try
            {
                return _userRepository.FetchWhatsAppNumberByEmail(Emailid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public string FetchNameByEmail(string Emailid)
        {
            try
            {
                return _userRepository.FetchNameByEmail(Emailid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public string SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl)
        {
            try
            {
                return _userRepository.SaveCallRecording(interviewid, IRFId, CandidateEmail, WhoCalled, CallSid, RecordingUrl);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }

        public async Task<string> SaveCallRecordingData(int? interviewId, string callRecipient, string caller, string CallSid, string RecordingSid, string callRecipientPhoneNumber = null, string IRFId = null)
        {
            try
            {
                return await _userRepository.SaveCallRecordingData(interviewId, callRecipient, caller, CallSid, RecordingSid, callRecipientPhoneNumber, IRFId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public List<CallRecordingDetailsVM> FetchCallRecording(string candidateEmailid, string irfid)
        {
            try
            {
                return _userRepository.FetchCallRecording(candidateEmailid, irfid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<ZoomAccountDetailsVM> FetchInstaZoomAccounts()
        {
            try
            {
                return _userRepository.FetchInstaZoomAccounts();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool DeleteInstaZoomAccount(int Accountid)
        {
            try
            {
                return _userRepository.DeleteInstaZoomAccount(Accountid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool AddInstaZoomAccount(string AccountEmail)
        {
            try
            {
                return _userRepository.AddInstaZoomAccount(AccountEmail);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public string GetCurrentUsername(string emailId)
        {
            try
            {
                return _userRepository.GetCurrentUsername(emailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public AssignmentVM GetInterviewerEntitleById(int assessmentid)
        {
            try
            {
                return _userRepository.GetInterviewerEntitleById(assessmentid);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string SaveCallRecordingInterviewer(string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl)
        {
            try
            {
                return _userRepository.SaveCallRecordingInterviewer(CandidateEmail, WhoCalled, CallSid, RecordingUrl);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public List<CallRecordingDetailsVM> FetchCallRecordingInterviewer(string Emailid)
        {
            try
            {
                return _userRepository.FetchCallRecordingInterviewer(Emailid);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddPremiumClientUser(ClientUserVM clientUser)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = clientUser.FirstName.Replace(" ", ""),
                    LastName = clientUser.LastName.Replace(" ", ""),
                    EmailId = clientUser.EmailId.Replace(" ", "")
                };
                var isPasswordReset = _userRepository.GetClientPasswordResetEnableStatus(clientUser.SelectedClient.Value).Result;
                clientUser.UserPassword = _passwordHelper.GenerateUserPassword(userDetails, isPasswordReset).Result;
                return _userRepository.AddPremiumClientUser(clientUser).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public List<ClientUserVM> GetClientPremiumUsers(string EmailId)
        {
            try
            {
                return _userRepository.GetClientPremiumUsers(EmailId).ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public ClientUserVM GetClientPremiumUser(int id)
        {
            try
            {
                var clientUser = _userRepository.GetClientPremiumUser(id);
                if (clientUser == null)
                    return null;
                else
                    return clientUser.Result;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateClientPremiumUser(ClientUserVM clientUser)
        {
            try
            {
                return _userRepository.UpdateClientPremiumUser(clientUser).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public string GetWhatsappno(string emailId)
        {
            try
            {
                return _userRepository.GetWhatsappno(emailId);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string UpdateWhatsappno(string emailId, string WhatsappNumber)
        {
            try
            {
                return _userRepository.UpdateWhatsappno(emailId, WhatsappNumber);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public List<ClientUserVM> GetClientUsersForReviewNotification()
        {
            try
            {
                return _userRepository.GetClientUsersForReviewNotification().ToList();
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
    }
}
