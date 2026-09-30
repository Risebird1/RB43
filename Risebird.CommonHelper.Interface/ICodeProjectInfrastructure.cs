using Risebird.Entities.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>Private blob storage for code project artifacts. Containers are never public.</summary>
    public interface ICodeProjectBlobStore
    {
        /// <summary>Candidate-visible project zips.</summary>
        string ProjectContainer { get; }

        /// <summary>Hidden tests + reference solution. Nothing in here is ever SAS-linked to a candidate machine.</summary>
        string SecretsContainer { get; }

        Task<string> UploadAsync(string container, string blobName, byte[] content, string contentType, CancellationToken ct = default);

        Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct = default);

        /// <summary>Read-only, blob-scoped, short-lived SAS url. Refuses the secrets container.</summary>
        string CreateReadUrl(string container, string blobName, TimeSpan lifetime);

        /// <summary>
        /// Blob-scoped read/add/create/write SAS for a VM Run Command's outputBlobUri (Run Command
        /// appends the script's output stream to it). Refuses the secrets container; creates the
        /// target container privately if needed.
        /// </summary>
        Task<string> CreateAppendUrlAsync(string container, string blobName, TimeSpan lifetime, CancellationToken ct = default);
    }

    /// <summary>Runs build + test in a throwaway container — never on a VM.</summary>
    public interface ICodeProjectRunner
    {
        bool IsConfigured { get; }

        Task<CodeProjectRunResult> RunAsync(CodeProjectRunRequest request, CancellationToken ct = default);
    }

    /// <summary>
    /// Starts candidate machines from a StackImages gallery image, restores project.zip into the
    /// workspace with a baseline git commit, and hands back a connect link (Bastion).
    /// </summary>
    public interface ICodeProjectEnvironmentProvisioner
    {
        bool IsConfigured { get; }

        Task<CodeProjectProvisionResult> ProvisionAsync(CodeProjectProvisionRequest request, CancellationToken ct = default);

        Task<CodeProjectEnvironmentStatus> GetStatusAsync(string handle, CancellationToken ct = default);

        /// <summary>Pulls the workspace at the candidate's last commit, plus the commit log and diff against baseline.</summary>
        Task<CodeProjectWorkspaceSnapshot> CollectAsync(string handle, CancellationToken ct = default);

        Task ReleaseAsync(string handle, CancellationToken ct = default);
    }
}
