using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IZohoSignHelper
    {
        /// <summary>ZohoSign:ClientId, ClientSecret and RefreshToken are set.</summary>
        bool IsConfigured { get; }

        /// <summary>Uploads the prepared agreement PDF, places the layout's fields and sends it for signing.</summary>
        Task<ZohoSignRequestResult> SendInterviewerAgreementAsync(ZohoSignAgreementRequest request, AgreementDocument document);
        Task<string> GetEmbeddedSignUrlAsync(string requestId, string actionId, string host);
        /// <summary>Re-sends the signing email to the recipients who have not signed yet.</summary>
        Task SendReminderAsync(string requestId);
        Task<ZohoSignRequestDetails> GetRequestDetailsAsync(string requestId, string actionId);
        Task<byte[]> DownloadSignedDocumentAsync(string requestId);
        /// <summary>Values filled in by the signers, keyed by field label (case-insensitive).</summary>
        Task<IReadOnlyDictionary<string, string>> GetFieldValuesAsync(string requestId);
        bool IsWebhookSignatureValid(string payload, string signature);
    }
}
