using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Code project blobs. Deliberately not built on AzureStorageHelper: that helper creates missing
    /// containers with public access, which would publish hidden tests and reference solutions.
    /// </summary>
    public class CodeProjectBlobStore : ICodeProjectBlobStore
    {
        private readonly string _connectionString;

        public CodeProjectBlobStore(IConfiguration configuration)
        {
            _connectionString = Setting(configuration, "CodeProjects:StorageConnectionString")
                ?? Setting(configuration, "SASURLSettings:ConnectionString");
            ProjectContainer = Setting(configuration, "CodeProjects:ProjectContainer") ?? "codeprojects";
            SecretsContainer = Setting(configuration, "CodeProjects:SecretsContainer") ?? "codeprojectsecrets";
        }

        public string ProjectContainer { get; }
        public string SecretsContainer { get; }

        private static string Setting(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public async Task<string> UploadAsync(string container, string blobName, byte[] content, string contentType, CancellationToken ct = default)
        {
            var containerClient = new BlobContainerClient(_connectionString, container);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            var blob = containerClient.GetBlobClient(blobName);
            using (var stream = new MemoryStream(content))
            {
                await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, ct);
            }
            return blob.Uri.ToString();
        }

        public async Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct = default)
        {
            var blob = new BlobContainerClient(_connectionString, container).GetBlobClient(blobName);
            var result = await blob.DownloadContentAsync(ct);
            return result.Value.Content.ToArray();
        }

        public string CreateReadUrl(string container, string blobName, TimeSpan lifetime)
        {
            if (string.Equals(container, SecretsContainer, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Secrets are never shared by link.");

            var blob = new BlobContainerClient(_connectionString, container).GetBlobClient(blobName);
            if (!blob.CanGenerateSasUri) return null;
            var sas = new BlobSasBuilder
            {
                BlobContainerName = container,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime),
                Protocol = SasProtocol.Https
            };
            sas.SetPermissions(BlobSasPermissions.Read);
            return blob.GenerateSasUri(sas).ToString();
        }

        public async Task<string> CreateAppendUrlAsync(string container, string blobName, TimeSpan lifetime, CancellationToken ct = default)
        {
            if (string.Equals(container, SecretsContainer, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Secrets are never shared by link.");

            var containerClient = new BlobContainerClient(_connectionString, container);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            var blob = containerClient.GetBlobClient(blobName);
            if (!blob.CanGenerateSasUri) return null;
            var sas = new BlobSasBuilder
            {
                BlobContainerName = container,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.Add(lifetime),
                Protocol = SasProtocol.Https
            };
            // Run Command requires read, append, create and write on its output blob.
            sas.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Add | BlobSasPermissions.Create | BlobSasPermissions.Write);
            return blob.GenerateSasUri(sas).ToString();
        }
    }

    /// <summary>
    /// Calls the container runner (an HTTP-triggered function) configured at CodeProjects:RunnerUrl.
    /// Contract: POST CodeProjectRunRequest as JSON, receive CodeProjectRunResult.
    /// </summary>
    public class HttpCodeProjectRunner : ICodeProjectRunner
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;
        private readonly string _url;
        private readonly string _key;

        public HttpCodeProjectRunner(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _url = configuration["CodeProjects:RunnerUrl"];
            _key = configuration["CodeProjects:RunnerKey"];
            _http.Timeout = TimeSpan.FromMinutes(5);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_url);

        public async Task<CodeProjectRunResult> RunAsync(CodeProjectRunRequest request, CancellationToken ct = default)
        {
            if (!IsConfigured)
                return new CodeProjectRunResult { Completed = false, Error = "The code project runner is not configured (CodeProjects:RunnerUrl)." };
            try
            {
                using (var msg = new HttpRequestMessage(HttpMethod.Post, _url) { Content = JsonContent.Create(request, options: Json) })
                {
                    if (!string.IsNullOrEmpty(_key)) msg.Headers.Add("x-functions-key", _key);
                    using (var resp = await _http.SendAsync(msg, ct))
                    {
                        if (!resp.IsSuccessStatusCode)
                            return new CodeProjectRunResult { Completed = false, Error = $"Runner returned HTTP {(int)resp.StatusCode}." };
                        var result = await resp.Content.ReadFromJsonAsync<CodeProjectRunResult>(Json, ct);
                        return result ?? new CodeProjectRunResult { Completed = false, Error = "Runner returned an empty response." };
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException)
            {
                return new CodeProjectRunResult { Completed = false, Error = "Runner call failed: " + ex.Message };
            }
        }
    }

    /// <summary>
    /// Calls the environment provisioner configured at CodeProjects:ProvisionerUrl. Endpoints:
    /// POST {base}/provision, GET {base}/status?handle=, POST {base}/collect, POST {base}/release.
    /// The provisioner owns the Azure side: gallery image, Custom Script Extension that pulls
    /// project.zip into the workspace with a baseline commit, NSG/VNet isolation and the Bastion link.
    /// </summary>
    public class HttpCodeProjectEnvironmentProvisioner : ICodeProjectEnvironmentProvisioner
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _key;

        public HttpCodeProjectEnvironmentProvisioner(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _baseUrl = configuration["CodeProjects:ProvisionerUrl"]?.TrimEnd('/');
            _key = configuration["CodeProjects:ProvisionerKey"];
            // Collect runs a Run Command on the VM and can take up to ~3 minutes.
            _http.Timeout = TimeSpan.FromMinutes(4);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl);

        public async Task<CodeProjectProvisionResult> ProvisionAsync(CodeProjectProvisionRequest request, CancellationToken ct = default)
        {
            if (!IsConfigured)
                return new CodeProjectProvisionResult { Accepted = false, Error = "The environment provisioner is not configured (CodeProjects:ProvisionerUrl)." };
            return await SendAsync<CodeProjectProvisionResult>(HttpMethod.Post, "/provision", request, ct)
                ?? new CodeProjectProvisionResult { Accepted = false, Error = "The provisioner did not respond." };
        }

        public async Task<CodeProjectEnvironmentStatus> GetStatusAsync(string handle, CancellationToken ct = default)
        {
            if (!IsConfigured) return new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Failed, Message = "Provisioner not configured." };
            return await SendAsync<CodeProjectEnvironmentStatus>(HttpMethod.Get, "/status?handle=" + Uri.EscapeDataString(handle ?? ""), null, ct)
                ?? new CodeProjectEnvironmentStatus { State = CodeProjectEnvironmentState.Provisioning, Message = "Waiting for the machine…" };
        }

        public async Task<CodeProjectWorkspaceSnapshot> CollectAsync(string handle, CancellationToken ct = default)
        {
            if (!IsConfigured) return new CodeProjectWorkspaceSnapshot { Success = false, Error = "Provisioner not configured." };
            return await SendAsync<CodeProjectWorkspaceSnapshot>(HttpMethod.Post, "/collect", new { handle }, ct)
                ?? new CodeProjectWorkspaceSnapshot { Success = false, Error = "The provisioner did not return the workspace." };
        }

        public async Task ReleaseAsync(string handle, CancellationToken ct = default)
        {
            if (!IsConfigured || string.IsNullOrEmpty(handle)) return;
            await SendAsync<object>(HttpMethod.Post, "/release", new { handle }, ct);
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string path, object body, CancellationToken ct) where T : class
        {
            try
            {
                using (var msg = new HttpRequestMessage(method, _baseUrl + path))
                {
                    if (body != null) msg.Content = JsonContent.Create(body, options: Json);
                    if (!string.IsNullOrEmpty(_key)) msg.Headers.Add("x-functions-key", _key);
                    using (var resp = await _http.SendAsync(msg, ct))
                    {
                        if (!resp.IsSuccessStatusCode || resp.Content.Headers.ContentLength == 0) return null;
                        return await resp.Content.ReadFromJsonAsync<T>(Json, ct);
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException)
            {
                return null;
            }
        }
    }
}
