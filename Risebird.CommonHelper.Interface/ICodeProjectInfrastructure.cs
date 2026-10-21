using Risebird.Entities.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>Configuration first, then the CodeProjectSettings table written by the one-click setup.</summary>
    public interface ICodeProjectSettingsProvider
    {
        /// <summary>The value, or null when neither configuration nor the table has one.</summary>
        string Get(string key);

        /// <summary>Base URL of the functions app (…/api), falling back to the local functions host in Development.</summary>
        string FunctionsBaseUrl();

        void Invalidate();
    }

    /// <summary>Private blob storage for code project artifacts. Containers are never public.</summary>
    public interface ICodeProjectBlobStore
    {
        /// <summary>Candidate-visible project zips.</summary>
        string ProjectContainer { get; }

        /// <summary>Hidden tests + reference solution. Nothing in here is ever SAS-linked to a candidate machine.</summary>
        string SecretsContainer { get; }

        /// <summary>The storage account behind the connection string (setup limits candidate egress to it).</summary>
        string AccountName { get; }

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

        /// <summary>Best-effort delete; a missing blob is not an error.</summary>
        Task DeleteAsync(string container, string blobName, CancellationToken ct = default);

        /// <summary>True when the blob exists.</summary>
        Task<bool> ExistsAsync(string container, string blobName, CancellationToken ct = default);
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

        /// <summary>
        /// Tells the provisioner when the candidate's slot ends. From then on it locks the account and
        /// disconnects sessions on its own, so the limit holds even if the browser never submits.
        /// </summary>
        Task<bool> HandoverAsync(string handle, DateTime deadlineUtc, CancellationToken ct = default);
    }
}
