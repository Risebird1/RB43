using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerEmailAvailabilityRepository
    {
        Task<int> InsertEmailAvailabilityAsync(InterviewerEmailAvailability record);

        Task<List<InterviewerEmailAvailabilityVM>> GetEmailAvailabilityAsync(
            string status, string senderEmail, string resumeFitment,
            System.DateTime? dateFrom, System.DateTime? dateTo, int page, int pageSize);

        Task<InterviewerEmailAvailabilityVM> GetEmailAvailabilityByIdAsync(int id);

        Task<bool> UpdateEmailAvailabilityStatusAsync(int id, string status, string changedBy);

        Task<bool> UpdateEmailAvailabilityVerificationAsync(
            int id, bool? employmentVerified, bool? resumeAIAnalysisCompleted,
            string resumeAIAnalysisResult, string newStatus);

        Task<bool> AssignInterviewerToAssessmentGroupAsync(
            int emailAvailabilityId, int interviewerProfileId,
            int assessmentGroupId, string assignedBy);

        Task<bool> DeleteEmailAvailabilityAsync(int id);

        Task<bool> DropInterviewerProfileAsync(int interviewerProfileId, string modifiedBy);
    }
}
