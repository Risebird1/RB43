using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerAgreementService
    {
        Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId);
        /// <summary>Uploads the signature image and the proof screenshot to blob storage and saves the agreement details. Returns an error message, or null on success.</summary>
        Task<string> SignAgreement(int interviewerProfileId, InterviewerAgreementSignRequest request);
        /// <summary>The interviewer's signature image (proof = false) or proof screenshot (proof = true); null if there is none.</summary>
        Task<(byte[] Content, string ContentType)?> GetAgreementImage(int interviewerProfileId, bool proof);
    }
}
