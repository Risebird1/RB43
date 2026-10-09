using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.Helpers;
using Risebird.Entities.ViewModel;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Database writes specific to the AI Audio Adaptive screening intake. Channel creation itself
    /// still goes through the shared, unmodified usp_CreateSkillChannelFromTemplateV1.
    /// </summary>
    public interface IAIScreeningRepository
    {
        /// <summary>
        /// Creates the AI Audio template holding the two-section rubric. Idempotent on
        /// <paramref name="templateName"/>: returns the existing template id rather than colliding
        /// with UC_ClonableSkillChannel_Name.
        /// </summary>
        Task<int> CreateAIAudioTemplate(
            string templateName,
            int assessmentGroupId,
            IList<TransSkill> rubric,
            string createdBy,
            string meetingPlatform = "Zoho",
            bool isAadhaarVerificationRequired = false);

        /// <summary>
        /// Every job-description url any channel still points at, for the retention job to keep clear
        /// of. Returns null when the read failed, which the caller must treat as "cannot tell what is
        /// in use" and delete nothing - distinct from an empty list, which means nothing is in use.
        /// </summary>
        Task<IReadOnlyCollection<string>> GetReferencedJdLinksAsync();

        /// <summary>
        /// Stamps the fields usp_CreateSkillChannelFromTemplateV1 does not write: the two-section
        /// count, the stored-prompt ids, the ideal candidate profile, and the rubric with its
        /// SkillSet sections.
        /// </summary>
        Task<bool> SaveAIScreeningProvisioning(
            int irfIntId,
            int skillSetsEvaluate,
            string openAIPromptId,
            string openAIEvaluatorPromptId,
            string idealCandidateProfile,
            IList<TransSkill> rubric,
            string jdTitleTwo = null,
            bool? candidateTranscriptReview = null);

        /// <summary>True when a template already carries this name.</summary>
        Task<bool> IsTemplateNameTaken(string templateName);

        /// <summary>
        /// True when the channel was created by the AI screening intake (IRF.IsAIAudioScreening).
        /// The single predicate the report gates screening-only behaviour on, so expert channels
        /// keep their current headline verdict.
        /// </summary>
        Task<bool> IsAIScreeningChannel(int irfIntId);

        /// <summary>
        /// Resolves the nearest Assessment Group to a job title, matched on the Assessment Group's
        /// own name. Returns 0 when nothing scores at least <paramref name="minimumScore"/>, and
        /// the caller falls back to the configured catch-all rather than guessing.
        /// </summary>
        /// <param name="minimumScore">Similarity between 0 and 1 a candidate must reach.</param>
        Task<int> ResolveAssessmentGroupIdByJobTitle(
            string jobTitle,
            double minimumScore = AssessmentGroupMatcher.DefaultMinimumScore);

        /// <summary>
        /// Assessment Group id for an exact name, used to find the seeded "AIInterview" catch-all
        /// without pinning its identity id in configuration. Returns 0 when it does not exist.
        /// </summary>
        Task<int> GetAssessmentGroupIdByName(string assessmentGroupName);
    }
}
