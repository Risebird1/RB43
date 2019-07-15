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
    }
}
