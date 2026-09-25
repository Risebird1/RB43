using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Supplies the interviewer service agreement PDF (from a local folder or Azure Blob),
    /// prepared for signing according to its layout file.
    /// </summary>
    public interface IInterviewerAgreementDocumentProvider
    {
        Task<AgreementDocument> GetDocumentAsync();

        /// <summary>
        /// Portal signing: stamps the interviewer's details, drawn signature, signing date and an
        /// electronic-signature audit block into the prepared agreement. Returns the signed PDF.
        /// </summary>
        byte[] StampPortalSignature(AgreementDocument document, PortalSignatureStamp stamp);
    }
}
