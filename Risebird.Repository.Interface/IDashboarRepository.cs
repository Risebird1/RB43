using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IDashboarRepository
    {
        IEnumerable<Meeting> GetTotalInvites();
        IEnumerable<Meeting> GetTotalInterviewer();
        IEnumerable<Meeting> GetTotalcandidate();
        Task<string> GetDoughnutGraphDetails(string flag);
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(DateTime date);
        Task<IEnumerable<DailyInterviewSlot>> GetDateWiseMeetingsSlots(DateTime date);
        Task<DashboardInterviewerCount> GetInterviewerCount();
        Task<IEnumerable<IRF>> GetIRFProgress();
        IEnumerable<IrfProgress> GetProgressOptions();
        Task<bool> UpdateIRFProgress(IList<IRF> irfProgressList);
        Task<MeetingResultCount> GetMeetingResultCount();
        Task<CandidatesCount> GetCandidatesCount();
    }
}
