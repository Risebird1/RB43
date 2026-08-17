using Risebird.Entities.Data.OpenAIModels;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IOpenAIResponsesHelper
    {
        /// <summary>
        /// Calls the OpenAI Responses API with a stored prompt and JSON output.
        /// Returns a RunResult compatible with existing Assistants API consumers.
        /// </summary>
        Task<RunResult> CreateJsonResponseAsync(
            string input,
            string operationType,
            string promptId);

        /// <summary>
        /// Same as <see cref="CreateJsonResponseAsync(string, string, string)"/> but accepts
        /// structured Responses API <c>input</c> (e.g. multimodal content with input_image parts).
        /// </summary>
        Task<RunResult> CreateJsonResponseAsync(
            object input,
            string operationType,
            string promptId);

        /// <summary>
        /// Same as CreateJsonResponseAsync but returns the OpenAI response id for multi-turn chaining.
        /// </summary>
        Task<(RunResult Result, string ResponseId)> CreateJsonResponseWithIdAsync(
            string input,
            string operationType,
            string promptId,
            string previousResponseId = null);

        /// <summary>
        /// Calls the Responses API without JSON output formatting (free-form text).
        /// </summary>
        Task<RunResult> CreateTextResponseAsync(
            string input,
            string operationType,
            string promptId);
    }
}
