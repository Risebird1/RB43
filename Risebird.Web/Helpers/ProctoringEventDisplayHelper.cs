using System;
using System.Text.Json;
using Risebird.Entities.Data;

namespace Risebird.Web.Helpers
{
    public static class ProctoringEventDisplayHelper
    {
        /// <summary>Human-readable line for the Reason column: <see cref="ProctoringSessionEvent.Reason"/>, else detail from <see cref="ProctoringSessionEvent.MetadataJson"/> (extensions often post blocked URL / host only in JSON).</summary>
        public static string GetReasonTextForDisplay(ProctoringSessionEvent e)
        {
            if (e == null) return "—";
            var reason = (e.Reason ?? string.Empty).Trim();
            var metaLine = string.IsNullOrWhiteSpace(e.MetadataJson)
                ? null
                : (TryBuildDetailFromMetadata(e.MetadataJson, out var m) && !string.IsNullOrWhiteSpace(m) ? m.Trim() : null);

            if (string.IsNullOrEmpty(reason))
            {
                if (!string.IsNullOrEmpty(metaLine)) return metaLine;
                if (string.IsNullOrWhiteSpace(e.MetadataJson)) return "—";
                return e.MetadataJson.Length > 200 ? e.MetadataJson.Trim() + "…" : e.MetadataJson.Trim();
            }

            if (string.IsNullOrEmpty(metaLine) || string.Equals(reason, metaLine, StringComparison.Ordinal))
                return reason;
            if (metaLine.Contains(reason, StringComparison.OrdinalIgnoreCase) && metaLine.Length > reason.Length)
                return metaLine;
            if (ShouldAppendMetadataDetail(reason, metaLine)) return reason + " — " + metaLine;
            return reason;
        }

        private static bool ShouldAppendMetadataDetail(string reason, string metaLine)
        {
            if (string.Equals(metaLine, reason, StringComparison.OrdinalIgnoreCase)) return false;
            if (metaLine.Contains("://", StringComparison.Ordinal) && reason.IndexOf("://", StringComparison.Ordinal) < 0) return true;
            if (metaLine.Length > 12 && (IsGenericProctoringReasonToken(reason) || reason.Length < 4)) return true;
            return false;
        }

        private static bool IsGenericProctoringReasonToken(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            var t = s.Trim();
            if (t.Length == 0) return true;
            return t.Equals("violation", StringComparison.OrdinalIgnoreCase)
                || t.Equals("info", StringComparison.OrdinalIgnoreCase)
                || t.Equals("event", StringComparison.OrdinalIgnoreCase)
                || t.Equals("warning", StringComparison.OrdinalIgnoreCase)
                || t.Equals("telemetry", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryBuildDetailFromMetadata(string json, out string text)
        {
            text = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.String)
                {
                    text = root.GetString();
                    return !string.IsNullOrWhiteSpace(text);
                }
                if (root.ValueKind != JsonValueKind.Object) return false;

                // Order matters: human message first, then URL / block context (restricted domains are often in metadata as url/host only).
                var flatNames = new[]
                {
                    "message", "detail", "description", "summary", "reason", "error", "violation",
                    "name", "title", "type",
                    "url", "href", "blockedurl", "blockedUrl", "blocked_url", "blockedUrlHost",
                    "currenturl", "currentUrl", "pageurl", "pageUrl", "taburl", "tabUrl", "location",
                    "host", "hostname", "domain", "restrictedHost", "matchedDomain",
                };
                foreach (var name in flatNames)
                {
                    foreach (var p in root.EnumerateObject())
                    {
                        if (!string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                        if (p.Value.ValueKind == JsonValueKind.String)
                        {
                            var s = p.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) { text = s; return true; }
                        }
                    }
                }
                if (TryFindStringByPropertyName(root, 5, out text) && !string.IsNullOrWhiteSpace(text)) return true;
            }
            catch
            { /* not JSON or partial */ }
            return false;
        }

        private static bool IsUrlOrBlockRelatedPropertyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Contains("url", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("host", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("domain", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("href", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("navigation", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("block", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("restrict", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>First non-empty string for any nested property that looks like URL / host / block context.</summary>
        private static bool TryFindStringByPropertyName(JsonElement el, int maxDepth, out string value)
        {
            value = null;
            if (maxDepth <= 0) return false;
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in el.EnumerateObject())
                {
                    if (p.Value.ValueKind == JsonValueKind.String && IsUrlOrBlockRelatedPropertyName(p.Name))
                    {
                        var s = p.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) { value = s; return true; }
                    }
                    if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array
                        && TryFindStringByPropertyName(p.Value, maxDepth - 1, out value))
                        return true;
                }
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                {
                    if (TryFindStringByPropertyName(item, maxDepth - 1, out value)) return true;
                }
            }
            return false;
        }
    }
}
