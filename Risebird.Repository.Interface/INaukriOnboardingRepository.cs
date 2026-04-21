using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface INaukriOnboardingRepository
    {
        Task<int> InsertAsync(NaukriProspectiveInterviewer record);

        Task<List<NaukriProspectiveInterviewerVM>> GetListAsync(
            string status, string candidateName, string magName,
            DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);

        Task<NaukriProspectiveInterviewerVM> GetByIdAsync(int id);

        Task<bool> UpdateStatusAsync(int id, string status, string changedBy);

        Task<(bool isExisting, int? existingUserId, int? existingProfileId)> UpdateResumeParseResultAsync(
            int id, string parsedEmail, string parsedPhone);

        Task<bool> UpdateEmploymentVerificationAsync(
            int id, bool? verified, string companyName, bool? isEmployed, string details);

        Task<bool> UpdateAIAnalysisAsync(
            int id, bool completed, string analysisResult, string verdict, int? score, string newStatus);

        Task<bool> UpdateOnboardingResultAsync(
            int id, int? userId, int? profileId, int? assessmentGroupId, bool ackSent);

        Task<bool> UpdateAckEmailSentAsync(int id);

        Task<bool> DeleteAsync(int id);

        Task<int?> GetInterviewerProfileIdByUserIdAsync(int userId);

        Task<bool> UpdateResumeLinkAsync(int id, string resumeLink);
    }
}
