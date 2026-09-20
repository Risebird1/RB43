using Microsoft.Extensions.Options;
using Risebird.Mcp.BugTriage.Configuration;
using Risebird.Mcp.BugTriage.Services;

namespace Risebird.Mcp.BugTriage.Tests;

/// <summary>
/// A throwaway checkout under the temp directory, so no test can touch the real repository.
/// Disposing deletes it.
/// </summary>
public sealed class SandboxRepository : IDisposable
{
    public string Root { get; }

    public SandboxRepository()
    {
        Root = Path.Combine(Path.GetTempPath(), "rb-mcp-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(Root, "src"));
        Directory.CreateDirectory(Path.Combine(Root, "src", "bin", "Debug"));
    }

    public string WriteFile(string relativePath, string content)
    {
        var absolute = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
        return absolute;
    }

    public string ReadFile(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath));

    public TriageOptions Options(Action<FixOptions>? configureFix = null)
    {
        var options = new TriageOptions
        {
            Repository = new RepositoryOptions { RootPath = Root },
            Fix = new FixOptions { Enabled = true },
        };

        configureFix?.Invoke(options.Fix);
        return options;
    }

    public (FixService Fix, FixProposalStore Store) CreateFixService(Action<FixOptions>? configureFix = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(Options(configureFix));
        var repository = new RepositoryService(options);
        var store = new FixProposalStore();
        return (new FixService(repository, store, options), store);
    }

    public RepositoryService CreateRepositoryService() =>
        new(Microsoft.Extensions.Options.Options.Create(Options()));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A locked file in a temp sandbox is not worth failing a test run over.
        }
    }
}
