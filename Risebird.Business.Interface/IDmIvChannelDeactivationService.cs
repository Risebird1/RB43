using Risebird.Entities.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDmIvChannelDeactivationService
    {
        Task<DmIvChannelDeactivationPreview> GetClientReportPreviewAsync(int interviewId, string profileName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Item 54 cascade + audit. Role from session profileName only — client flips Client Vet, internal flips RB Vet.
        /// </summary>
        Task<DmIvChannelDeactivationResult> DeactivateInterviewerOnChannelFromClientReportAsync(
            int interviewId, string profileName, string changedByEmail,
            CancellationToken cancellationToken = default);
    }
}
