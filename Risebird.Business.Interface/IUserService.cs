using System.Collections.Generic;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    public interface IUserService
   {
       List<Menu> GetMenuBasedOnUserEmailld(string emailId);
       List<UserList> GetRisebirdUsers();
       List<ClientUserVM> GetClientUsers();
       List<InterviewerVM> GetInterviewers();
        bool AddClientUser(ClientUserVM clientUser);
        bool DeleteClientUser(int id);
        ClientUserVM GetClientUser(int id);
        bool UpdateClientUser(ClientUserVM clientUser);

    }
}
