using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>Provisioner state for code project candidate machines (used by Risebird.AzureTimerFunctions).</summary>
    public interface ICodeProjectVmRepository
    {
        /// <summary>Inserts the machine, or returns the existing handle for the same (session, question).</summary>
        Task<string> Add(CodeProjectVm vm);

        Task<CodeProjectVm> Get(string handle);

        /// <summary>True when this caller now holds the short advance lease.</summary>
        Task<bool> TryLease(string handle, int leaseSeconds = 60);

        Task Update(string handle, string step, string message, int? vmPropertyId = null, string connectUrl = null,
            bool clearProjectZipUrl = false, bool releaseLease = true);

        Task<List<string>> GetInProgress();

        Task<List<string>> GetExpired(int maxLifetimeMinutes, int failedGraceMinutes);

        /// <summary>Set once at handover; later calls (reconnects) do not extend it.</summary>
        Task SetDeadline(string handle, System.DateTime deadlineUtc);

        /// <summary>Machines in the 'ready' step, for the session monitor and deadline lock.</summary>
        Task<List<CodeProjectVm>> GetReady();

        Task UpdateMonitor(string handle, string seenSessionIdsJson, string integrityFlagsJson, bool markLocked);

        /// <summary>Handles not yet released, for the nightly orphan sweep.</summary>
        Task<List<string>> GetLiveHandles();

        /// <summary>VMProperties row (Purpose = 'CodeProject') that CodingAssessment.VMPropertyId points at.</summary>
        Task<int> AddVMProperty(string name, string resourceGroupName, string osType, string location, string userName);
    }
}
