using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDashboardService
    {
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date);
        Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date);
        Task<DashboardInterviewerCount> GetInterviewerCount();
        Task<IEnumerable<IRF>> GetIRFProgress(DateTime date);
        IEnumerable<IrfProgress> GetProgressOptions();
        Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList);
        Task<MeetingResultCount> GetMeetingResultCount();
        Task<CandidatesCount> GetCandidatesCount();
        Task<IEnumerable<Meeting>> GetDateWiseClientMeetings(DateTime date, string emailId);
    }
}
