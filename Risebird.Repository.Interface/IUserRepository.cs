using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId);
        IEnumerable<UserList> GetRisebirdUsers();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<ClientUserVM> GetClientUsers();
    }
}
