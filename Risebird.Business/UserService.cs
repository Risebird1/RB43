using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.ViewModel;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.Business
{
    public class UserService : IUserService
    {
        private IUserRepository _userRepository;        
        private IApplicationErrorLogger _applicationErrorLogger;

        public UserService(IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
    }

        public List<Menu> GetMenuBasedOnUserEmailld(string emailId)
        {
            try
            {
                return _userRepository.GetMenuBasedOnUserEmailld(emailId).ToList();
            }
            catch (Exception e)
            {
                return null;
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
                return null;
            }
        }

        public UserList GetUser(int id)
        {
            try
            {
                var user = _userRepository.GetUser(id);

                if (user == null)
                    return null;
                else
                    return user.Result;
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

        public List<ClientUserVM> GetClientUsers()
        {
            try
            {
                return _userRepository.GetClientUsers().ToList();
            }
            catch (Exception e)
            {
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

            catch(Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddClientUser(ClientUserVM clientUser)
        {
            try
            {
                return _userRepository.AddClientUser(clientUser).Result;
            }

            catch(Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

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



    }
}
