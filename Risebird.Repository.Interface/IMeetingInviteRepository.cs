using System;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IMeetingInviteRepository
    {
        Task<string> CheckMeetingAccount(DateTime starttime, DateTime endtime);
        Task<string> InsertMeetingInvite(DateTime starttime, DateTime endtime, string Emailaddress, string Accountemail, string Linkurl, string whom, string Subject, string timeZone);
    }
}
