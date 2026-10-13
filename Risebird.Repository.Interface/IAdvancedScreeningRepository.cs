using Risebird.Entities.ViewModel.AdvancedScreening;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>Advanced Resume Screening data access: one method per stored procedure.</summary>
    public interface IAdvancedScreeningRepository
    {
        Task<IRFScreeningRubricVM> GetIRFScreeningRubric(int irfId);
        Task<RubricSaveResult> SaveIRFScreeningRubric(RubricSaveRequest request);

        /// <summary>Claims the next batch atomically, setting it to Processing.</summary>
        Task<IEnumerable<AdvancedScreeningWorkItem>> ClaimCandidatesForAdvancedScreening(int batchSize);
        Task<bool> SaveCandidateAdvancedScreening(CandidateAdvancedScreeningSave model);
        Task<int> QueueAdvancedScreening(int irfId, string scope, int? candidateId, string triggeredBy);
        Task<CandidateAdvancedScreeningVM> GetCandidateAdvancedScreening(int candidateId);
        Task<AdvancedScreeningCounts> GetAdvancedScreeningCounts(int irfId);

        Task<CompanyVerificationRecord> GetCompanyVerification(string normalizedName);
        Task<bool> SaveCompanyVerification(CompanyVerificationRecord record, bool setBlocklist);

        Task<bool> SaveCandidateWorkHistory(int candidateId, int candidateMasterId, int irfId, string historyJson);
        Task<IEnumerable<CandidateWorkHistoryRecord>> GetCandidateWorkHistories(int candidateMasterId, int excludeCandidateId);

        Task<bool> DeleteResumeFingerprints(int candidateId);
        Task<bool> SaveResumeFingerprint(int candidateId, int? candidateMasterId, string sectionType, string sectionName,
            byte[] minHash, IEnumerable<(byte BandNo, long BandHash)> bands);
        Task<IEnumerable<ResumeFingerprintMatch>> FindSimilarResumeFingerprints(int candidateId, int? candidateMasterId,
            IEnumerable<(byte BandNo, long BandHash)> bands);
    }
}
