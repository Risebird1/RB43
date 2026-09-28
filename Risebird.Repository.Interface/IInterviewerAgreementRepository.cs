using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerAgreementRepository
    {
        Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId);
        Task<bool> SaveInterviewerAgreement(int interviewerProfileId, string agreementVersion, string signerName, string fatherName, string address, string signatureUrl);
    }
}
