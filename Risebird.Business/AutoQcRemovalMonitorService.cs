using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class AutoQcRemovalMonitorService : IAutoQcRemovalMonitorService
    {
        private readonly IAutoQcRemovalMonitorRepository _repository;

        public AutoQcRemovalMonitorService(IAutoQcRemovalMonitorRepository repository)
        {
            _repository = repository;
        }

        public Task<AutoQcRemovalMonitorVm> GetMonitorAsync(DateTime? fromUtc, DateTime? toUtc, int? irfId, string source, string verdict) =>
            _repository.GetMonitorDataAsync(fromUtc, toUtc, irfId, source, verdict);
    }
}
