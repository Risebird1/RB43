using Risebird.Entities.Data;
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
    }
}
