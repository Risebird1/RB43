using System.Threading.Tasks;

namespace Risebird.Web.Services.Proctoring
{
    /// <summary>
    /// Determines whether proctoring telemetry should be persisted — AI Interview sessions only (active window).
    /// </summary>
    public interface IProctoringSessionEligibilityService
    {
        Task<bool> ShouldPersistProctoringEventsAsync(ProctoringTokenPayload payload);
    }
}
