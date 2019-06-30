using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.ViewModel;

namespace Risebird.Business
{
    public class UserService:IUserService
    {
        private IUserRepository _userRepository;
        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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
    }
}
