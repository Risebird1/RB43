using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface INotificationHelper
    {
        string SendHelpsupportMail(Mail model);
        string SendIRFBasedInterviewerMail(List<string> interviwers, string meetingDate);
        string SendInterviewerMailOnAssinment(string interviwers, string meetingDate);
        string SendIRFBasedClientMail(List<TransClientUser> clientUsers, string meetingDate, string IRFId, string candidateName, string interviewStatus);
    }
}
