using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IChatService
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
        string checkOnlinemettings(DateTime starttime,DateTime endtime);
        string InsertOnlinemeeting(DateTime starttime, DateTime endtime,string Emailaddress,string Accountemail,string Linkurl,string whom,string Subject);
    }
}
