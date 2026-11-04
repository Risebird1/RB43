using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Risebird.Business.CallPreference
{
    /// <summary>
    /// Reads a call opt-out from whatever JSON the provider posts to api/CallPreference/*/OptOut: an ElevenLabs
    /// server tool body, an ElevenLabs post-call webhook, or a Plivo IVR / portal post. Fields are found by name
    /// anywhere in the payload; the shallowest match wins (a match that doesn't parse falls through to the next).
    /// Names are compared case-insensitively with <c>_</c> and <c>-</c> ignored, so <c>candidate_id</c> and
    /// <c>candidateId</c> are the same field. An object shaped <c>{ "value": x }</c> is read as <c>x</c>, which covers
    /// ElevenLabs <c>analysis.data_collection_results</c>.
    /// </summary>
    public class CallOptOutPayload
    {
        private readonly List<(string Name, int Depth, JsonElement Value)> _fields;

        private CallOptOutPayload(List<(string, int, JsonElement)> fields) => _fields = fields;

        private static readonly string[] FlagNames = { "unsubscribe", "unsubscribed", "optout", "optedout", "notinterested", "donotcall", "dnd" };
        private static readonly string[] ActionNames = { "action", "event", "intent", "disposition", "outcome" };
        private static readonly string[] ActionValues = { "unsubscribe", "optout", "notinterested", "donotcall", "dnc", "dnd", "stop" };
        private static readonly string[] TrueWords = { "true", "yes", "y", "1" };

        /// <summary>Parses the body; null when it isn't a JSON object.</summary>
        public static CallOptOutPayload Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
                var fields = new List<(string, int, JsonElement)>();
                Flatten(doc.RootElement.Clone(), 0, fields);
                return new CallOptOutPayload(fields);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>Whether the payload carries an opt-out: a true flag, or an action / event equal to an opt-out value.</summary>
        public bool IsOptOut
        {
            get
            {
                // The shallowest flag decides, so { "unsubscribe": { "value": false } } is not an opt-out
                // even when a deeper field says otherwise.
                var flag = Ordered(FlagNames).FirstOrDefault();
                if (flag.Name != null) return IsTrue(flag.Value);
                return Ordered(ActionNames).Any(f => ActionValues.Contains(Normalize(AsString(f.Value))));
            }
        }

        public int? UserId => FirstInt("userid", "interviewerid", "intervieweruserid");
        public int? CandidateId => FirstInt("candidateid");
        public int? IRFId => FirstInt("irfid", "skillchannelid");
        public string Phone => FirstString("phone", "phonenumber", "mobile", "mobilenumber", "externalnumber", "callee", "to");
        public string Email => FirstString("email", "emailid");
        public string CallId => FirstString("callid", "calluuid", "conversationid", "callsid");
        public string Reason => FirstString("optoutreason", "reason");

        /// <summary>ElevenLabs when conversation_id or agent_id is present, Plivo when CallUUID is, otherwise Unknown.</summary>
        public string DetectSource()
        {
            if (Has("conversationid") || Has("agentid")) return "ElevenLabs";
            if (Has("calluuid")) return "Plivo";
            return "Unknown";
        }

        private bool Has(string name) => _fields.Any(f => f.Name == name && f.Value.ValueKind != JsonValueKind.Null);

        private IEnumerable<(string Name, int Depth, JsonElement Value)> Ordered(params string[] names) =>
            _fields.Where(f => names.Contains(f.Name) && f.Value.ValueKind != JsonValueKind.Null).OrderBy(f => f.Depth);

        private string FirstString(params string[] names) =>
            Ordered(names).Select(f => AsString(f.Value)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

        private int? FirstInt(params string[] names)
        {
            foreach (var f in Ordered(names))
            {
                if (f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var n) && n > 0) return n;
                if (int.TryParse(AsString(f.Value)?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s > 0) return s;
            }
            return null;
        }

        private static void Flatten(JsonElement element, int depth, List<(string, int, JsonElement)> fields)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in element.EnumerateObject())
                {
                    var value = Unwrap(p.Value);
                    fields.Add((Normalize(p.Name), depth, value));
                    if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                        Flatten(value, depth + 1, fields);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    Flatten(item, depth + 1, fields);
            }
        }

        // { "value": x, ... } -> x (ElevenLabs data collection results carry value + rationale).
        private static JsonElement Unwrap(JsonElement value) =>
            value.ValueKind == JsonValueKind.Object && value.TryGetProperty("value", out var inner) ? inner : value;

        private static bool IsTrue(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => value.TryGetInt32(out var n) && n == 1,
            JsonValueKind.String => TrueWords.Contains(value.GetString()?.Trim().TrimEnd('.').ToLowerInvariant()),
            _ => false
        };

        private static string AsString(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        private static string Normalize(string name) =>
            name == null ? null : name.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
    }
}
