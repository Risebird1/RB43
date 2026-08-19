using Risebird.Entities.Data.OpenAIModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAIResponsesHelper
    {
        /// <summary>
        /// Calls the OpenAI Responses API with a stored prompt and JSON output.
        /// Returns a RunResult compatible with existing Assistants API consumers.
        /// Set <paramref name="useStoredPromptModel"/> when the stored prompt carries model-specific
        /// settings (e.g. reasoning) that OpenAI:ResponsesModel would conflict with.
        /// </summary>
        Task<RunResult> CreateJsonResponseAsync(
            string input,
            string operationType,
            string promptId,
            bool useStoredPromptModel = false);

        /// <summary>
        /// Same as <see cref="CreateJsonResponseAsync(string, string, string, bool)"/> but accepts
        /// structured Responses API <c>input</c> (e.g. multimodal content with input_image parts).
        /// </summary>
        Task<RunResult> CreateJsonResponseAsync(
            object input,
            string operationType,
            string promptId,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false);

        /// <summary>
        /// Same as CreateJsonResponseAsync but returns the OpenAI response id for multi-turn chaining.
        /// </summary>
        Task<(RunResult Result, string ResponseId)> CreateJsonResponseWithIdAsync(
            string input,
            string operationType,
            string promptId,
            string previousResponseId = null,
            int? maxOutputTokens = null,
            bool useStoredPromptModel = false);

        /// <summary>
        /// Calls the Responses API without JSON output formatting (free-form text).
        /// </summary>
        Task<RunResult> CreateTextResponseAsync(
            string input,
            string operationType,
            string promptId,
            bool useStoredPromptModel = false);
    }
}
