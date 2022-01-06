using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Business.Interface
{
    public interface IInterviewRequestService
    {
        bool CreateInerviewRequest(InterviewRequestVM meeting);
        IEnumerable<InterviewRequestVM> GetAllInterviewRequests(string emailId, string profileName);
        InterviewRequestVM FetchInterviewRequestById(int id);
        bool AssignInerviewRequest(int userid, int id);
        bool SetIRFCreated(int id, string IRFId);
        string FetchInterviewerNameByEmailid(string emailid);
    }
}
