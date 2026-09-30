using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ICodeProjectQuestionRepository
    {
        Task<List<StackImage>> GetActiveStackImages();

        /// <summary>Inserts the question row and its CodeProjectDetails in one transaction, status 'validating'. Returns the new question id, or 0.</summary>
        Task<int> AddCodeProjectQuestion(int irfId, string skillName, CodeProjectGeneration generation, int timeLimitMinutes,
            string sanitizedPayload, string createdBy);

        Task<bool> UpdateValidation(int questionId, string status, string validationJson, string projectBlobUri = null, string secretsBlobUri = null);

        Task<CodeProjectDetails> GetCodeProjectDetails(int questionId);

        Task<List<int>> GetUnfinishedValidations();

        Task<List<CodeProjectStatusRow>> GetStatusesByIRF(int irfId);

        /// <summary>The next question after the one on screen, if it is a servable code project question.</summary>
        Task<(int QuestionId, string StackImageCode)?> PeekNextCodeProjectQuestion(int interviewId, int irfId, int currentQuestionId);

        /// <summary>True only for the first caller for this (session, question).</summary>
        Task<bool> ClaimEnvironment(string sessionId, int questionId, string stackImageCode);

        Task<bool> UpdateEnvironment(string sessionId, int questionId, string state, string environmentHandle = null,
            string connectUrl = null, int? vmPropertyId = null, string message = null);

        Task<CodeProjectEnvironment> GetEnvironment(string sessionId, int questionId);

        Task<int> SaveCodeProjectAssessment(CodeProjectAssessment assessment);

        Task<CodeProjectAssessment> GetCodeProjectAssessment(string sessionId, int questionId);

        Task<List<CodeProjectAssessment>> GetCodeProjectAssessmentsBySessionId(string sessionId);
    }
}
