using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ICodeProjectQuestionService
    {
        Task<List<StackImage>> GetActiveStackImages();

        /// <summary>
        /// Persists the question (status 'validating') and stages project.zip and the secrets file in
        /// private blob storage. Returns the question id and an error when it could not be stored.
        /// The caller enqueues <see cref="ValidateAsync"/>; nothing here builds anything.
        /// </summary>
        Task<(int QuestionId, string Error)> CreateQuestionAsync(int irfId, string skillName, CodeProjectGeneration generation,
            int timeLimitMinutes, string createdBy);

        /// <summary>The validation job: build, reference passes, starter mostly fails, wall time, effort. Runs in a container.</summary>
        Task ValidateAsync(int questionId, CancellationToken ct = default);

        Task<List<int>> GetUnfinishedValidations();

        Task<CodeProjectDetails> GetDetails(int questionId);

        Task<List<CodeProjectStatusRow>> GetStatusesByIRF(int irfId);

        /// <summary>Starts the candidate machine for (session, question) if nobody has yet. Safe to call repeatedly.</summary>
        Task PrepareEnvironmentAsync(string sessionId, int questionId, CancellationToken ct = default);

        /// <summary>Look-ahead: if the question after the current one is a code project, start its machine now.</summary>
        Task PrepareNextEnvironmentAsync(string sessionId, int interviewId, int irfId, int currentQuestionId, CancellationToken ct = default);

        /// <summary>Current state, polling the provisioner while the machine is still coming up.</summary>
        Task<CodeProjectEnvironment> RefreshEnvironmentAsync(string sessionId, int questionId, CancellationToken ct = default);

        /// <summary>
        /// Link and sign-in for a handed-over machine, read live from the provisioner (the Bastion link
        /// carries no credentials). Null unless the environment has been handed over. Never stored here.
        /// </summary>
        Task<CodeProjectEnvironmentStatus> GetConnectionAsync(CodeProjectEnvironment env, CancellationToken ct = default);

        /// <summary>Hands the machine to the candidate. The clock starts here, once; a reconnect does not restart it.</summary>
        Task<CodeProjectEnvironment> HandOverAsync(string sessionId, int questionId, CancellationToken ct = default);

        /// <summary>Starts the machine again after a failure that happened before any machine existed; otherwise returns the environment unchanged.</summary>
        Task<CodeProjectEnvironment> RetryEnvironmentAsync(string sessionId, int questionId, CancellationToken ct = default);

        /// <summary>Pulls the last commit off the VM, grades it against the hidden tests in a container, saves, releases the VM.</summary>
        Task<(CodeProjectAssessment Assessment, CodeProjectGradeResult Grade)> SubmitAsync(string sessionId, int questionId,
            string createdBy, CancellationToken ct = default);

        Task<List<CodeProjectAssessment>> GetAssessmentsForSession(string sessionId);

        /// <summary>Text logged to the transcript so the interview evaluator scores the project against the rubric.</summary>
        string BuildTranscriptAnswer(CodeProjectDetails details, CodeProjectAssessment assessment, CodeProjectGradeResult grade);
    }
}
