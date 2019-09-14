using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IUserRepository
    {
        IEnumerable<Menu> GetMenuBasedOnUserEmailld(string emailId);
        IEnumerable<UserList> GetRisebirdUsers();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<ClientUserVM> GetClientUsers();

        IEnumerable<QCInterviewerVM> GetQCInterviewers();
        Task<bool> AddUser(UserList user);
        Task<bool> DeleteUser(int id);
        Task<UserList> GetUser(int id);
        Task<bool> UpdateUser(UserList user);

        Task<bool> AddClientUser(ClientUserVM client);
        Task<bool> DeleteClientUser(int id);
        Task<ClientUserVM> GetClientUser(int id);
        Task<bool> UpdateClientUser(ClientUserVM clientUser);

        Task<bool> AddInterviewer(InterviewerVM interviewer);
        Task<bool> AddQCInterviewer(QCInterviewerVM interviewer);
        Task<bool> DeleteInterviewer(int id);
        Task<InterviewerVM> GetInterviewer(int id);
        Task<bool> UpdateInterviewer(InterviewerVM Interviewer);

        Task<QCInterviewerVM> GetQCInterviewer(int id);
        Task<bool> UpdateQCInterviewer(QCInterviewerVM qcInterviewer);
        IEnumerable<string> GetAllHrsEmailld();

        Task<bool> DeleteQCInterviewer(int id);
    }
}
