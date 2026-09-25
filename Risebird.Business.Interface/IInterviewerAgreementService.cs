using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerAgreementService
    {
        Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId);
        /// <summary>The agreement PDF (as it will be signed) for the interviewer to read before signing.</summary>
        Task<byte[]> GetAgreementDocumentForReview();
        Task<InterviewerAgreementVM> GetInterviewerAgreementByZohoRequestId(string zohoRequestId);
        /// <summary>Creates the Zoho Sign request (or reuses the open one). Returns null plus a message on failure.</summary>
        Task<(InterviewerAgreementVM Agreement, string Message)> InitiateAgreement(int interviewerProfileId, string returnUrl);
        /// <summary>
        /// Portal signing (ZohoSign:SigningMode = Portal): stamps the interviewer's details and drawn
        /// signature into the agreement, stores the signed PDF and marks the agreement completed.
        /// </summary>
        Task<(InterviewerAgreementVM Agreement, string Message)> SignInPortal(int interviewerProfileId, PortalSignatureStamp stamp);
        Task<string> GetEmbeddedSignUrl(int interviewerProfileId, string host);
        /// <summary>Email signing: asks Zoho Sign to send the signing email again.</summary>
        Task<bool> ResendAgreementEmail(int interviewerProfileId);
        /// <summary>Re-reads the request from Zoho Sign and stores the status (and the signed PDF once completed).</summary>
        Task<InterviewerAgreementVM> SyncAgreementStatus(string zohoRequestId);
        Task<byte[]> GetSignedDocument(InterviewerAgreementVM agreement);
    }
}
