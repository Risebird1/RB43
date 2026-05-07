using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Parses <see cref="ProctoringSessionEvent.MetadataJson"/> for blob URLs saved by the proctoring API.
    /// </summary>
    public static class ProctoringBlobPreviewHelper
    {
        /// <summary>
        /// Builds preview rows for events that reference blob storage (screenshots and MediaRecorder chunks).
        /// </summary>
        public static IReadOnlyList<ProctoringBlobPreviewVm> TakeBlobPreviews(IEnumerable<ProctoringSessionEvent> events, int maxItems = 96)
        {
            if (events == null || maxItems <= 0)
                return Array.Empty<ProctoringBlobPreviewVm>();

            var result = new List<ProctoringBlobPreviewVm>();
            foreach (var ev in events.OrderByDescending(e => e.CreatedAtUtc))
            {
                if (result.Count >= maxItems)
                    break;
                if (ev == null || string.IsNullOrWhiteSpace(ev.MetadataJson))
                    continue;

                if (!IsBlobBackedKind(ev.EventKind))
                    continue;

                var vm = TryParse(ev);
                if (vm != null && !string.IsNullOrWhiteSpace(vm.BlobUrl))
                    result.Add(vm);
            }

            return result;
        }

        private static bool IsBlobBackedKind(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind))
                return false;
            return string.Equals(kind, "screenshot", StringComparison.OrdinalIgnoreCase)
                || string.Equals(kind, "screen-chunk", StringComparison.OrdinalIgnoreCase);
        }

        private static ProctoringBlobPreviewVm TryParse(ProctoringSessionEvent ev)
        {
            try
            {
                var obj = JObject.Parse(ev.MetadataJson);
                var url = FirstString(obj, "blobUrl", "BlobUrl");
                if (string.IsNullOrWhiteSpace(url))
                    return null;

                var mime = FirstString(obj, "mimeType", "MimeType");
                var captured = FirstString(obj, "capturedAt", "CapturedAt");
                long? chunkSeq = null;
                var cs = obj["chunkSeq"] ?? obj["ChunkSeq"];
                if (cs != null && cs.Type != JTokenType.Null && long.TryParse(cs.ToString(), out var csLong))
                    chunkSeq = csLong;

                return new ProctoringBlobPreviewVm
                {
                    BlobUrl = url.Trim(),
                    EventKind = ev.EventKind ?? "",
                    CreatedAtUtc = ev.CreatedAtUtc,
                    MimeType = mime ?? "",
                    ChunkSeq = chunkSeq,
                    CapturedAtRaw = captured
                };
            }
            catch
            {
                return null;
            }
        }

        private static string FirstString(JObject obj, params string[] keys)
        {
            foreach (var key in keys)
            {
                var t = obj[key];
                if (t != null && t.Type != JTokenType.Null)
                {
                    var s = t.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        return s.Trim();
                }
            }

            return null;
        }
    }
}
