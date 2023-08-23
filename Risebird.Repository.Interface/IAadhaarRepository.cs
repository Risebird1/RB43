using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAadhaarRepository
    {
        Task<bool> SaveAadhaarDetails(AadhaarData model);
        Task<AadhaarData> FetchAadhaarDetails(string MCQId, string candidateEmail);
        Task<AadhaarDetails> FetchCandidateEmailidByUniqueId(string uniqueid);
        Task<string> FetchUniqueIdByEmailid(string emailid);
        Task<AadhaarData> FetchInterviewAadhaarDetails(string candidateEmail);
        Task<AadhaarData> FetchInterviewCandidateDetails(string candidateEmail);
        Task<IEnumerable<AadhaarData>> GetAadharDetails();
        Task<bool> SaveCandidateVerificationDetails(candidateDetailsresult model);
        Task SaveFaceMatchScorePercentage(string email, int score);
        Task SaveEmploymentDetails(SignzyEmployeeResponse model);
        Task<IEnumerable<EmploymentDetailsDM>> GetEmployementDetails();
        Task<int> CheckEmployementDetails(string mobilenumber);
        Task<EmploymentDetailsDM> GetEmployementDetailsByMobileNumber(string mobilenumber);
        Task<bool> CheckCompanyStatus(string CompanyName);
    }
}
