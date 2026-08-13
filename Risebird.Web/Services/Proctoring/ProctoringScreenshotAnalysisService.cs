using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;

namespace Risebird.Web.Services.Proctoring
{
    public sealed class ProctoringScreenshotAnalysisService : IProctoringScreenshotAnalysisService
    {
        private const string DefaultPromptId = "pmpt_6a0442519dc88196bf2ccbe66edceb9e094878bc268fa261";
        private const string OperationType = "ProctoringScreenshotAnalysis";

        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IProctoringSessionEventRepository _proctoringSessionEvents;
        private readonly IOpenAIResponsesHelper _openAIResponsesHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProctoringScreenshotAnalysisService> _logger;

        public ProctoringScreenshotAnalysisService(
            IAzureStorageHelper azureStorageHelper,
            IProctoringSessionEventRepository proctoringSessionEvents,
            IOpenAIResponsesHelper openAIResponsesHelper,
            IConfiguration configuration,
            ILogger<ProctoringScreenshotAnalysisService> logger)
        {
            _azureStorageHelper = azureStorageHelper;
            _proctoringSessionEvents = proctoringSessionEvents;
            _openAIResponsesHelper = openAIResponsesHelper;
            _configuration = configuration;
            _logger = logger;
        }

        private string ScreenshotContainer =>
            _configuration["ProctoringSettings:ScreenshotContainerName"] ?? "proctoring-screenshots";

        private string PromptId =>
            string.IsNullOrWhiteSpace(_configuration["OpenAI:ProctoringScreenshotAnalysisPromptId"])
                ? DefaultPromptId
                : _configuration["OpenAI:ProctoringScreenshotAnalysisPromptId"].Trim();

        private int ScreenshotSampleCount
        {
            get
            {
                if (!int.TryParse(_configuration["ProctoringSettings:AnalysisScreenshotCount"], out var n))
                    n = 5;
                return Math.Max(1, Math.Min(n, 10));
            }
        }

        private int MaxImageBytes
        {
            get
            {
                if (!int.TryParse(_configuration["ProctoringSettings:AnalysisMaxImageBytes"], out var n))
                    n = 4 * 1024 * 1024;
                return Math.Max(64 * 1024, Math.Min(n, 16 * 1024 * 1024));
            }
        }

        public async Task<ProctoringScreenshotAnalysisResult> AnalyzeAsync(int interviewId, string candidateEmail = null, CancellationToken cancellationToken = default)
        {
            var result = new ProctoringScreenshotAnalysisResult();
            if (interviewId <= 0)
            {
                result.Error = "Invalid interview id.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(_configuration["OpenAI:ApiKey"]))
            {
                result.Error = "OpenAI API key is not configured.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(PromptId))
            {
                result.Error = "OpenAI:ProctoringScreenshotAnalysisPromptId is not configured.";
                return result;
            }

            List<CloudBlockBlob> blobs;
            try
            {
                blobs = await ListScreenshotBlobsAsync(interviewId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proctoring analysis: failed to list screenshots for interview {InterviewId}", interviewId);
                result.Error = "Failed to list screenshots from blob storage.";
                return result;
            }

            if (blobs.Count == 0)
            {
                result.Error = "No proctoring screenshots found for this interview.";
                return result;
            }

            var sampled = SampleEvenly(blobs, ScreenshotSampleCount);
            var images = new List<(string path, string dataUrl)>();
            foreach (var blob in sampled)
            {
                try
                {
                    if (blob.Properties?.Length > MaxImageBytes)
                    {
                        _logger.LogWarning(
                            "Proctoring analysis: skipping oversized screenshot {Blob} ({Bytes} bytes) for interview {InterviewId}",
                            blob.Name, blob.Properties.Length, interviewId);
                        continue;
                    }
                    using var ms = new MemoryStream();
                    await blob.DownloadToStreamAsync(ms).ConfigureAwait(false);
                    var bytes = ms.ToArray();
                    if (bytes.Length == 0)
                        continue;
                    images.Add((blob.Name, $"data:{MimeFromBlobName(blob.Name)};base64,{Convert.ToBase64String(bytes)}"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Proctoring analysis: failed to download screenshot {Blob} for interview {InterviewId}", blob.Name, interviewId);
                }
            }

            if (images.Count == 0)
            {
                result.Error = "Screenshots exist but none could be downloaded for analysis.";
                return result;
            }

            string analysisJson;
            try
            {
                analysisJson = await CallOpenAIPromptAsync(interviewId, images, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proctoring analysis: OpenAI call failed for interview {InterviewId}", interviewId);
                result.Error = "OpenAI analysis request failed: " + ex.Message;
                return result;
            }

            if (string.IsNullOrWhiteSpace(analysisJson))
            {
                result.Error = "OpenAI returned an empty analysis.";
                return result;
            }

            result.Ok = true;
            result.AnalysisJson = analysisJson;
            result.ScreenshotsAnalyzed = images.Count;
            result.ScreenshotPaths = images.Select(i => i.path).ToList();

            await PersistAnalysisEventAsync(interviewId, candidateEmail, result).ConfigureAwait(false);
            return result;
        }

        private async Task<List<CloudBlockBlob>> ListScreenshotBlobsAsync(int interviewId)
        {
            var list = new List<CloudBlockBlob>();
            var container = _azureStorageHelper.GetCloudBlobContainer(ScreenshotContainer);
            var prefix = $"{interviewId}/";
            BlobContinuationToken continuation = null;
            do
            {
                var segment = await container.ListBlobsSegmentedAsync(
                    prefix, true, BlobListingDetails.None, null, continuation, null, null).ConfigureAwait(false);
                continuation = segment?.ContinuationToken;
                if (segment?.Results == null)
                    continue;
                foreach (var item in segment.Results)
                {
                    if (item is CloudBlockBlob blob && IsImageBlob(blob.Name))
                    {
                        // Length can be unset on list; skip only known-empty blobs.
                        if (blob.Properties?.Length != null && blob.Properties.Length <= 0)
                            continue;
                        list.Add(blob);
                    }
                }
            } while (continuation != null);

            list.Sort((a, b) =>
            {
                var ta = a.Properties?.LastModified ?? DateTimeOffset.MinValue;
                var tb = b.Properties?.LastModified ?? DateTimeOffset.MinValue;
                return ta.CompareTo(tb);
            });
            return list;
        }

        private static bool IsImageBlob(string name)
        {
            var n = (name ?? string.Empty).ToLowerInvariant();
            return n.EndsWith(".jpg") || n.EndsWith(".jpeg") || n.EndsWith(".png") || n.EndsWith(".webp");
        }

        private static string MimeFromBlobName(string name)
        {
            var n = (name ?? string.Empty).ToLowerInvariant();
            if (n.EndsWith(".png")) return "image/png";
            if (n.EndsWith(".webp")) return "image/webp";
            return "image/jpeg";
        }

        /// <summary>Keeps first and last, spreads the rest evenly across the session timeline.</summary>
        private static List<CloudBlockBlob> SampleEvenly(List<CloudBlockBlob> blobs, int count)
        {
            if (blobs.Count <= count)
                return blobs;
            var sampled = new List<CloudBlockBlob>(count);
            for (var i = 0; i < count; i++)
            {
                var index = (int)Math.Round(i * (blobs.Count - 1) / (double)(count - 1));
                sampled.Add(blobs[index]);
            }
            return sampled.Distinct().ToList();
        }

        private async Task<string> CallOpenAIPromptAsync(
            int interviewId,
            List<(string path, string dataUrl)> images,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = new List<object>
            {
                new
                {
                    type = "input_text",
                    text = $"These are {images.Count} proctoring screenshots captured in chronological order during interview {interviewId}. " +
                           "Provide a decisive integrity assessment. Respond with JSON only using this shape: " +
                           "{\"verdict\":\"CLEAR|REVIEW_NEEDED|HIGH_RISK\",\"riskLevel\":\"low|medium|high\"," +
                           "\"eyeGaze\":\"one decisive sentence\",\"eyeGazeStatus\":\"NORMAL|SUSPICIOUS\"," +
                           "\"suspiciousApps\":[\"app names or empty\"],\"suspiciousAppsStatus\":\"NONE|DETECTED\"," +
                           "\"keyFindings\":[\"short decisive bullets\"],\"recommendation\":\"one clear next action\"}. " +
                           "Be firm and specific. Avoid vague wording.",
                },
            };
            content.AddRange(images.Select(i => (object)new
            {
                type = "input_image",
                image_url = i.dataUrl,
                detail = "auto",
            }));

            // Structured multimodal input for Responses API; prompt id from OpenAI:ProctoringScreenshotAnalysisPromptId.
            var input = new object[]
            {
                new { role = "user", content },
            };

            var runResult = await _openAIResponsesHelper.CreateJsonResponseAsync(
                input,
                OperationType,
                PromptId).ConfigureAwait(false);

            var outputText = runResult?.Data?
                .FirstOrDefault()?
                .Content?
                .FirstOrDefault()?
                .Text?
                .Value;

            if (string.IsNullOrWhiteSpace(outputText))
                throw new InvalidOperationException("OpenAI Responses API returned no analysis text.");

            return NormalizeToJson(outputText);
        }

        /// <summary>Returns valid JSON text: strips markdown fences, wraps non-JSON model output as {"raw": ...}.</summary>
        private static string NormalizeToJson(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                    trimmed = trimmed.Substring(firstNewline + 1);
                var fenceEnd = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (fenceEnd >= 0)
                    trimmed = trimmed.Substring(0, fenceEnd);
                trimmed = trimmed.Trim();
            }

            if (trimmed.Length == 0)
                return null;

            try
            {
                using (JsonDocument.Parse(trimmed))
                {
                    return trimmed;
                }
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { raw = trimmed });
            }
        }

        private async Task PersistAnalysisEventAsync(int interviewId, string candidateEmail, ProctoringScreenshotAnalysisResult result)
        {
            try
            {
                using var analysisDoc = JsonDocument.Parse(result.AnalysisJson);
                var metadataJson = JsonSerializer.Serialize(new
                {
                    screenshotsAnalyzed = result.ScreenshotsAnalyzed,
                    screenshotPaths = result.ScreenshotPaths,
                    container = ScreenshotContainer,
                    promptId = PromptId,
                    analysis = analysisDoc.RootElement,
                });
                await _proctoringSessionEvents.InsertAsync(new ProctoringSessionEvent
                {
                    InterviewId = interviewId,
                    CandidateEmail = candidateEmail,
                    EventKind = "ai_screenshot_analysis",
                    Level = "info",
                    Reason = "ai_screenshot_analysis",
                    MetadataJson = metadataJson,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Proctoring analysis: failed to persist analysis event for interview {InterviewId}", interviewId);
            }
        }
    }
}
