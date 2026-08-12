using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ICodeExecutionService
    {
        /// <summary>
        /// True when a sandbox backend is configured and candidates may run their code.
        /// When false the IDE still works as an editor, it just cannot execute.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Compiles and runs the supplied source in an isolated sandbox and returns
        /// whatever the program wrote to stdout/stderr. Never throws: transport and
        /// backend failures come back as a result with Success = false.
        /// </summary>
        Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request);
    }
}
