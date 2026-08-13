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
    }
}
