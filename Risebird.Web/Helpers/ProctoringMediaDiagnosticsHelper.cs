using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Risebird.Web.Helpers
{
    public static class ProctoringMediaDiagnosticsHelper
    {
        public static ProctoringMediaDiagnosticsViewModel Build(
            MeetingVM meeting,
            IReadOnlyList<ProctoringSessionEvent> events,
            IReadOnlyList<ProctoringCapturedBlobViewModel> fallbackScreenshots,
            IReadOnlyList<ProctoringCapturedBlobViewModel> fallbackChunks)
        {
            var list = events?.OrderBy(x => x.CreatedAtUtc).ToList() ?? new List<ProctoringSessionEvent>();
            var shots = list.Where(x => string.Equals(x.EventKind, "screenshot", StringComparison.OrdinalIgnoreCase)).ToList();
            var chunks = list.Where(x => string.Equals(x.EventKind, "screen_chunk", StringComparison.OrdinalIgnoreCase)).ToList();

            int WithBlob(IEnumerable<ProctoringSessionEvent> rows) =>
                rows.Count(e => !string.IsNullOrWhiteSpace(TryGetMetaString(e.MetadataJson, "blobUrl")));

            int UploadFailed(IEnumerable<ProctoringSessionEvent> rows) =>
                rows.Count(e => !string.IsNullOrWhiteSpace(TryGetMetaString(e.MetadataJson, "uploadError")));

            var durationMin = meeting?.interviewDuration > 0 ? meeting.interviewDuration : 60;
            var windowStart = meeting?.interviewDate ?? DateTime.MinValue;
            var windowEnd = windowStart.AddMinutes(durationMin + 5);

            var diag = new ProctoringMediaDiagnosticsViewModel
            {
                InterviewId = meeting?.InterviewId ?? 0,
                ScheduledStartLocal = windowStart,
                ScheduledEndLocal = windowEnd,
                MeetingStatusId = meeting?.MeetingStatus,
                TotalEvents = list.Count,
                JoinEvents = list.Count(e => string.Equals(e.EventKind, "join", StringComparison.OrdinalIgnoreCase)),
                StartEvents = list.Count(e => string.Equals(e.EventKind, "start", StringComparison.OrdinalIgnoreCase)),
                StopEvents = list.Count(e => string.Equals(e.EventKind, "stop", StringComparison.OrdinalIgnoreCase)),
                ScreenshotEvents = shots.Count,
                ScreenshotWithBlob = WithBlob(shots),
                ScreenshotUploadFailed = UploadFailed(shots),
                ChunkEvents = chunks.Count,
                ChunkWithBlob = WithBlob(chunks),
                ChunkUploadFailed = UploadFailed(chunks),
                AzureScreenshotFallback = fallbackScreenshots?.Count(x => !string.IsNullOrWhiteSpace(x.BlobUrl)) ?? 0,
                AzureChunkFallback = fallbackChunks?.Count(x => !string.IsNullOrWhiteSpace(x.BlobUrl)) ?? 0,
            };

            var causes = new List<string>();
            if (diag.TotalEvents == 0)
            {
                causes.Add("Nothing reached the server — this is not a blob upload failure. No join/start/screenshot/chunk API calls were stored.");
                causes.Add("Typical causes: Risebird Proctoring extension not installed or disabled; interview opened without the official AI Interview link (seId); content script blocked; join API never called; or join blocked outside the scheduled window (join can succeed in the browser but not persist in DB).");
                causes.Add("Candidate checklist: Chrome extension v1.9.1+ loaded; page URL contains /AIInterview/; allow Entire screen when prompted; stay on the page 2+ minutes after sharing.");
            }
            else
            {
                if (diag.JoinEvents == 0)
                    causes.Add("No join event — extension did not call POST /api/proctoring/session/join (wrong page, extension disabled, or outside scheduled telemetry window).");
                if (diag.JoinEvents > 0 && diag.StartEvents == 0)
                {
                    causes.Add("Join recorded but no start — POST /api/proctoring/session/start did not persist (network, expired token, or setup stopped before start retries finished).");
                    causes.Add("Fix: Chrome extension v1.9.1+, reload AI Interview tab, allow Entire screen when prompted, stay on page 2+ minutes. Start is sent during join (before screen picker) with automatic retries.");
                }
                if (diag.StartEvents > 0 && diag.ScreenshotEvents == 0 && diag.ChunkEvents == 0)
                    causes.Add("Session started but no screenshot or screen_chunk API calls — screen capture/offscreen pipeline did not run, share was cancelled, or the tab was closed before the first capture. Extension v1.9.10+ auto-recovers: stay on AI Interview 45s+, allow Entire screen when prompted again, or reload the page.");
                if (diag.ScreenshotEvents > 0 && diag.ScreenshotWithBlob == 0)
                    causes.Add($"Screenshot API called {diag.ScreenshotEvents} time(s) but none have blobUrl — uploads failed or were rejected ({diag.ScreenshotUploadFailed} with uploadError in metadata).");
                if (diag.ChunkEvents > 0 && diag.ChunkWithBlob == 0)
                    causes.Add($"Screen chunk API called {diag.ChunkEvents} time(s) but none have blobUrl — Azure upload failed ({diag.ChunkUploadFailed} with uploadError), chunk rejected as too small (< MinScreenChunkBytes), or oversized payload. Video uses larger POST bodies than screenshots; extension v1.9.9+ merges partial recorder blobs and retries blob 500 errors.");
                if (diag.ScreenshotWithBlob > 0 && diag.ChunkWithBlob == 0 && diag.ChunkEvents == 0)
                    causes.Add("Screenshots uploaded but no screen_chunk events — MediaRecorder/offscreen video pipeline did not run (screen share ended early, recorder error, or extension below v1.9.9).");
                if (diag.AzureScreenshotFallback == 0 && diag.AzureChunkFallback == 0 &&
                    diag.ScreenshotWithBlob == 0 && diag.ChunkWithBlob == 0)
                    causes.Add($"Azure containers have no blobs under interview folder {diag.InterviewId} — nothing was stored in proctoring-screenshots or proctoring-screen-chunks.");
            }

            if (causes.Count == 0 && diag.ScreenshotWithBlob + diag.ChunkWithBlob + diag.AzureScreenshotFallback + diag.AzureChunkFallback == 0)
                causes.Add("Media counters are zero — verify the candidate used extension v1.8.6+, allowed entire-screen share, and stayed on the interview for at least one minute.");

            diag.LikelyCauses = causes;
            return diag;
        }

        private static string TryGetMetaString(string metadataJson, string key)
        {
            if (string.IsNullOrWhiteSpace(metadataJson) || string.IsNullOrWhiteSpace(key))
                return null;
            try
            {
                using var doc = JsonDocument.Parse(metadataJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty(key, out var prop) &&
                    prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                }
            }
            catch
            {
                // ignore
            }
            return null;
        }
    }
}
