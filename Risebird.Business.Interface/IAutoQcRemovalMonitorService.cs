using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAutoQcRemovalMonitorService
    {
        Task<AutoQcRemovalMonitorVm> GetMonitorAsync(DateTime? fromUtc, DateTime? toUtc, int? irfId, string source, string verdict);
    }
}
