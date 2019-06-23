using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

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
            return _userRepository.GetMenuBasedOnUserEmailld(emailId).ToList();
        }
    }
}
