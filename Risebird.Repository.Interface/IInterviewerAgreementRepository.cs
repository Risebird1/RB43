using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerAgreementRepository
    {
        Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId);
        Task<InterviewerAgreementVM> GetInterviewerAgreementByZohoRequestId(string zohoRequestId);
        Task<bool> SaveInterviewerAgreementRequest(int interviewerProfileId, ZohoSignRequestResult request, string agreementVersion);
        Task<bool> UpdateInterviewerAgreementStatus(string zohoRequestId, string agreementStatus, bool isAgreementSigned,
            DateTime? signedOn, DateTime? completedOn, string documentUrl, string declineReason,
            string signerName = null, string fatherName = null, string address = null);
    }
}
