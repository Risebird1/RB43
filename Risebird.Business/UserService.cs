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
        private readonly IInterviewerListingSearchRepository _interviewerListingSearchRepository;
        private readonly ICallCatalogueListingSearchRepository _callCatalogueListingSearchRepository;

        public UserService(
            IUserRepository userRepository,
            IApplicationErrorLogger applicationErrorLogger,
            IPasswordHelper passwordHelper,
            IInterviewerListingSearchRepository interviewerListingSearchRepository,
            ICallCatalogueListingSearchRepository callCatalogueListingSearchRepository)
        {
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _passwordHelper = passwordHelper;
            _interviewerListingSearchRepository = interviewerListingSearchRepository;
            _callCatalogueListingSearchRepository = callCatalogueListingSearchRepository;
        }

        public async Task<List<Menu>> GetMenuBasedOnUserEmailld(string emailId, string sessionId = null)
        {
            try
            {
                return await _userRepository.GetMenuBasedOnUserEmailld(emailId, sessionId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> CheckMultiSession(string emailId)
        {
            try
            {
                return await _userRepository.CheckMultiSession(emailId);
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
                return await _userRepository.ClearSessionForOthersUsers(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #region User Service Functions

        public async Task<List<UserList>> GetRisebirdUsers()
        {
            try
            {
                return await _userRepository.GetRisebirdUsers();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<UserList> GetUser(int id)
        {
            try
            {
                var user = await _userRepository.GetUser(id);
                var userClients = await _userRepository.GetUserClients(id);
                if (user == null)
                {
                    return null;
                }
                else
                {
                    user.ClientsArray = userClients.ToArray();
                    return user;
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddUser(UserList user)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = user.FirstName.Replace(" ", ""),
                    LastName = user.LastName.Replace(" ", ""),
                    EmailId = user.EmailId.Replace(" ", "")
                };
                user.UserPassword = await _passwordHelper.GenerateUserPassword(userDetails, false);
                return await _userRepository.AddUser(user);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteUser(int id)
        {
            try
            {
                var profileId = await TryResolveInterviewerProfileIdByUserIdAsync(id);
                var result = await _userRepository.DeleteUser(id);
                if (result && profileId > 0)
                    await TryRemoveInterviewerListingAsync(profileId);
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> UpdateUser(UserList user)
        {
            try
            {
                var result = await _userRepository.UpdateUser(user);
                // Name / email / IsActive on UserList are indexed for interviewer listing.
                if (result && user != null && user.Id > 0)
                    await TrySyncInterviewerListingByUserIdAsync(user.Id);
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion

        #region Client User Service Functions

        public async Task<List<ClientUserVM>> GetClientUsers(string EmailId)
        {
            try
            {
                return await _userRepository.GetClientUsers(EmailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<ClientUserVM> GetClientUser(int id)
        {
            try
            {
                var clientUser = await _userRepository.GetClientUser(id);

                if (clientUser == null)
                    return null;
                else
                    return clientUser;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddClientUser(ClientUserVM clientUser)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = clientUser.FirstName.Replace(" ", ""),
                    LastName = clientUser.LastName.Replace(" ", ""),
                    EmailId = clientUser.EmailId.Replace(" ", "")
                };
                var isPasswordReset = await _userRepository.GetClientPasswordResetEnableStatus(clientUser.SelectedClient.Value);
                //clientUser.UserPassword = GenerateUserPassword(clientUser).Result;
                // TODO for Prashant, Please call method to fetch PasswordReswet Flag and pass it below
                clientUser.UserPassword = await _passwordHelper.GenerateUserPassword(userDetails, isPasswordReset);
                return await _userRepository.AddClientUser(clientUser);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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

        public async Task<bool> DeleteClientUser(int id)
        {
            try
            {
                return await _userRepository.DeleteClientUser(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> UpdateClientUser(ClientUserVM clientUser)
        {
            try
            {
                return await _userRepository.UpdateClientUser(clientUser);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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

        public async Task<List<InterviewerVM>> GetInterviewers()
        {
            try
            {
                return await _userRepository.GetInterviewers();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<List<InterviewerVM>> GetInterviewersDetails()
        {
            try
            {
                return await _userRepository.GetInterviewersDetails();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddInterviewer(InterviewerVM interviewer)
        {
            try
            {
                return await _userRepository.AddInterviewer(interviewer);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<InterviewerVM> GetInterviewer(int id)
        {
            try
            {
                var interviewer = await _userRepository.GetInterviewer(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<InterviewerVM>> GetInterviewerVideos(string Emailid)
        {
            try
            {
                var interviewer = await _userRepository.GetInterviewerVideos(Emailid);

                if (interviewer == null)
                    return null;
                else
                    return interviewer;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<MeetingVM> GetMeetingbyid(int id)
        {
            try
            {
                var interviewer = await _userRepository.GetMeetingbyid(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateInterviewer(InterviewerVM interviewer)
        {
            try
            {
                return await _userRepository.UpdateInterviewer(interviewer);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteInterviewer(int id)
        {
            try
            {
                return await _userRepository.DeleteInterviewer(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        #endregion

        #region QCInterviewer Service Functions

        public async Task<List<QCInterviewerVM>> GetQCInterviewers()
        {
            try
            {
                return await _userRepository.GetQCInterviewers();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddQCInterviewer(QCInterviewerVM qcinterviewer)
        {
            try
            {
                return await _userRepository.AddQCInterviewer(qcinterviewer);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<QCInterviewerVM> GetQCInterviewer(int id)
        {
            try
            {
                var interviewer = await _userRepository.GetQCInterviewer(id);

                if (interviewer == null)
                    return null;
                else
                    return interviewer;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> UpdateQCInterviewer(QCInterviewerVM qcInterviewer)
        {
            try
            {
                return await _userRepository.UpdateQCInterviewer(qcInterviewer);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteQCInterviewer(int id)
        {
            try
            {
                return await _userRepository.DeleteQCInterviewer(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        #endregion

        public async Task<List<string>> GetAllHrsEmailld()
        {
            try
            {
                return await _userRepository.GetAllHrsEmailld();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<List<ResumeEvaluator>> GetResumeEvaluators()
        {
            try
            {
                return await _userRepository.GetResumeEvaluators();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                return await _userRepository.AddResumeEvaluator(resumeEvaluator);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<ResumeEvaluator> GetResumeEvaluator(int id)
        {
            try
            {
                return await _userRepository.GetResumeEvaluator(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> EditResumeEvaluator(ResumeEvaluator resumeEvaluator)
        {
            try
            {
                return await _userRepository.EditResumeEvaluator(resumeEvaluator);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> DeleteResumeEvaluator(int userId)
        {
            try
            {
                return await _userRepository.DeleteResumeEvaluator(userId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        public async Task<List<Recruiter>> GetRecruiters()
        {
            try
            {
                return await _userRepository.GetRecruiters() ?? new List<Recruiter>();

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddRecruiter(Recruiter recruiter)
        {
            try
            {
                return await _userRepository.AddRecruiter(recruiter);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<Recruiter> GetRecruiter(int id)
        {
            try
            {
                return await _userRepository.GetRecruiter(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> EditRecruiter(Recruiter recruiter)
        {
            try
            {
                return await _userRepository.EditRecruiter(recruiter);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteRecruiter(int userId)
        {
            try
            {
                return await _userRepository.DeleteRecruiter(userId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task UpdateUserSigInDetails(string emailId)
        {
            try
            {
                await _userRepository.UpdateUserSigInDetails(emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

            }
        }

        public async Task<string> GetCurrenttheme(string emailId)
        {
            try
            {
                return await _userRepository.GetCurrenttheme(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<AssignmentVM>> GetInterviewerEntitle()
        {
            try
            {
                return await _userRepository.GetInterviewerEntitle();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<AssignmentVM>> InterviewerEntitleDetails(int id)
        {
            try
            {
                return await _userRepository.InterviewerEntitleDetails(id);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<string> GetCurrentuserImage(string emailId)
        {
            try
            {
                return await _userRepository.GetCurrentuserImage(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<(string, string)> FetchWhatsAppNumberByEmail(string Emailid)
        {
            try
            {
                return await _userRepository.FetchWhatsAppNumberByEmail(Emailid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return (string.Empty, string.Empty);
            }
        }
        public async Task<string> FetchNameByEmail(string Emailid)
        {
            try
            {
                return await _userRepository.FetchNameByEmail(Emailid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "";
            }
        }
        public async Task<string> SaveCallRecording(int interviewid, string IRFId, string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl)
        {
            try
            {
                var result = await _userRepository.SaveCallRecording(interviewid, IRFId, CandidateEmail, WhoCalled, CallSid, RecordingUrl);
                if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                    await TrySyncCallCatalogueCandidateAsync(IRFId, CandidateEmail);
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
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
        public async Task<List<CallRecordingDetailsVM>> FetchCallRecording(string candidateEmailid, string irfid)
        {
            try
            {
                return await _userRepository.FetchCallRecording(candidateEmailid, irfid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<ZoomAccountDetailsVM>> FetchInstaZoomAccounts()
        {
            try
            {
                return await _userRepository.FetchInstaZoomAccounts();
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> DeleteInstaZoomAccount(int Accountid)
        {
            try
            {
                return await _userRepository.DeleteInstaZoomAccount(Accountid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> AddInstaZoomAccount(string AccountEmail)
        {
            try
            {
                return await _userRepository.AddInstaZoomAccount(AccountEmail);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<string> GetCurrentUsername(string emailId)
        {
            try
            {
                return await _userRepository.GetCurrentUsername(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<AssignmentVM> GetInterviewerEntitleById(int assessmentid)
        {
            try
            {
                return await _userRepository.GetInterviewerEntitleById(assessmentid);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> SaveCallRecordingInterviewer(string CandidateEmail, string WhoCalled, string CallSid, string RecordingUrl)
        {
            try
            {
                var result = await _userRepository.SaveCallRecordingInterviewer(CandidateEmail, WhoCalled, CallSid, RecordingUrl);
                if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
                    await TrySyncCallCatalogueInterviewerAsync(CandidateEmail);
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Fail";
            }
        }
        public async Task<List<CallRecordingDetailsVM>> FetchCallRecordingInterviewer(string Emailid)
        {
            try
            {
                return await _userRepository.FetchCallRecordingInterviewer(Emailid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> AddPremiumClientUser(ClientUserVM clientUser)
        {
            try
            {
                var userDetails = new UserDetails
                {
                    FirstName = clientUser.FirstName.Replace(" ", ""),
                    LastName = clientUser.LastName.Replace(" ", ""),
                    EmailId = clientUser.EmailId.Replace(" ", "")
                };
                var isPasswordReset = await _userRepository.GetClientPasswordResetEnableStatus(clientUser.SelectedClient.Value);
                clientUser.UserPassword = await _passwordHelper.GenerateUserPassword(userDetails, isPasswordReset);
                return await _userRepository.AddPremiumClientUser(clientUser);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<List<ClientUserVM>> GetClientPremiumUsers(string EmailId)
        {
            try
            {
                return await _userRepository.GetClientPremiumUsers(EmailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<ClientUserVM> GetClientPremiumUser(int id)
        {
            try
            {
                var clientUser = await _userRepository.GetClientPremiumUser(id);
                if (clientUser == null)
                    return null;
                else
                    return clientUser;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<bool> UpdateClientPremiumUser(ClientUserVM clientUser)
        {
            try
            {
                return await _userRepository.UpdateClientPremiumUser(clientUser);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<string> GetWhatsappno(string emailId)
        {
            try
            {
                return await _userRepository.GetWhatsappno(emailId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> UpdateWhatsappno(string emailId, string WhatsappNumber)
        {
            try
            {
                return await _userRepository.UpdateWhatsappno(emailId, WhatsappNumber);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<ClientUserVM>> GetClientUsersForReviewNotification()
        {
            try
            {
                return await _userRepository.GetClientUsersForReviewNotification();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<InterviewerProfileVM>> GetInterviewerInterestNotification()
        {
            try
            {
                return await _userRepository.GetInterviewerInterestNotification();
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private async Task TrySyncCallCatalogueCandidateAsync(string irfId, string candidateEmail)
        {
            if (_callCatalogueListingSearchRepository == null
                || !_callCatalogueListingSearchRepository.IsEnabled
                || string.IsNullOrWhiteSpace(candidateEmail))
                return;

            try
            {
                await _callCatalogueListingSearchRepository.UpsertCandidateCallCatalogueAsync(irfId, candidateEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task TrySyncCallCatalogueInterviewerAsync(string interviewerEmail)
        {
            if (_callCatalogueListingSearchRepository == null
                || !_callCatalogueListingSearchRepository.IsEnabled
                || string.IsNullOrWhiteSpace(interviewerEmail))
                return;

            try
            {
                await _callCatalogueListingSearchRepository.UpsertInterviewerCallCatalogueAsync(interviewerEmail);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task TrySyncInterviewerListingByUserIdAsync(int userId)
        {
            if (_interviewerListingSearchRepository == null
                || !_interviewerListingSearchRepository.IsEnabled
                || userId <= 0)
                return;

            try
            {
                // No-op / false when the user is not an interviewer — expected for client users.
                await _interviewerListingSearchRepository.UpsertInterviewerByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task<int> TryResolveInterviewerProfileIdByUserIdAsync(int userId)
        {
            if (_interviewerListingSearchRepository == null || userId <= 0)
                return 0;

            try
            {
                var docs = await _interviewerListingSearchRepository.GetIndexDocumentsFromDatabaseAsync(userId: userId);
                return docs?.FirstOrDefault()?.InterviewerProfileId ?? 0;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        private async Task TryRemoveInterviewerListingAsync(int interviewerProfileId)
        {
            if (_interviewerListingSearchRepository == null
                || !_interviewerListingSearchRepository.IsEnabled
                || interviewerProfileId <= 0)
                return;

            try
            {
                var removed = await _interviewerListingSearchRepository.RemoveInterviewerAsync(interviewerProfileId);
                if (!removed)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Exception,
                        new InvalidOperationException(
                            $"Interviewer Azure Search remove returned false for interviewerProfileId={interviewerProfileId}."));
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
