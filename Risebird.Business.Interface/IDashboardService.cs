using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Risebird.Business.Interface
{
   public interface IDashboardService
    {
        List<Meeting> GetInvitecnt();
        List<Meeting> GetInterviwercnt();
        List<Meeting> GetCandidatecnt();
        string GetDoughnutGraphDetails(string flag);
    }
}
