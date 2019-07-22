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

        bool AddUser(UserList clientUser);
        bool DeleteUser(int id);
        UserList GetUser(int id);
        bool UpdateUser(UserList clientUser);

        bool AddClientUser(ClientUserVM clientUser);
        bool DeleteClientUser(int id);
        ClientUserVM GetClientUser(int id);
        bool UpdateClientUser(ClientUserVM clientUser);

        bool AddInterviewer(InterviewerVM interviewer);
        InterviewerVM GetInterviewer(int id);
        bool DeleteInterviewer(int id);
        bool UpdateInterviewer(InterviewerVM interviewer);

    }
}
