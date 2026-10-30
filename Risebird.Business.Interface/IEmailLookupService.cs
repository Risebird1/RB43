using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IEmailLookupService
    {
        Task<EmailLookupResultVM> GetEmailLookupData(string emailId);
        Task<OpsToolkitVM> GetOpsToolkitData(DateTime fromDate, DateTime toDate);
        Task<OpsToolkitInterviewHistoryVM> GetInterviewPointsHistory(int interviewId);
        Task<OpsToolkitActionResultVM> RemoveUnassignPenalty(int interviewPointsId, string reason, string performedBy);
    }
}
