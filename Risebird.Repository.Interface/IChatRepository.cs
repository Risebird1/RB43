using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.Repository.Interface
{
    public interface IChatRepository
    {
        List<Conversation> GetConversation(string userid, string contact, string status, string WhoSend);
        void AddConversation(string userid, string contact, string message);
        void AddGroup(string GroupName);
        List<GroupDetails> FetchGroup();
        void DeleteGroup(string GroupId);
        List<GroupDetails> FetchUser(string GroupId);
        string AddUser(string GroupId, string Emailid);
        void DeleteUser(string Userid);
        string Savetheme(string theme, string userid);
    }
}
