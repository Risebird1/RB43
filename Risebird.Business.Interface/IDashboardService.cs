using Risebird.Entities.Data;
using System.Collections.Generic;
namespace Risebird.Business.Interface
{
    public interface IDashboardService
    {
        List<Meeting> GetInvitecnt();
        List<Meeting> GetInterviwercnt();
        List<Meeting> GetCandidatecnt();
        string GetDoughnutGraphDetails(string flag);
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(int addDays = 0);
    }
}
