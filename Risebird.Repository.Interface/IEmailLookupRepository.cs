using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IEmailLookupRepository
    {
        Task<EmailLookupResultVM> GetEmailLookupData(string emailId);
        Task<List<OpsToolkitEmergencyUnassignVM>> GetEmergencyUnassignments(DateTime fromDate, DateTime toDate);
        Task<OpsToolkitInterviewHistoryVM> GetInterviewPointsHistory(int interviewId);
        Task<OpsToolkitActionResultVM> RemoveUnassignPenalty(int interviewPointsId, string reason, string performedBy);
    }
}
