using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerAgreementService
    {
        Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId);
        /// <summary>Uploads the signature image to blob storage and saves the agreement details. Returns an error message, or null on success.</summary>
        Task<string> SignAgreement(int interviewerProfileId, InterviewerAgreementSignRequest request);
    }
}
