using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
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
        Task<IEnumerable<Meeting>> GetDateWiseMeetings(int addDays = 0);
    }
}
