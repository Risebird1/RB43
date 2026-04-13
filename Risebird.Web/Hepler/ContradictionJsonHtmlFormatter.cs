using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Risebird.Web.Hepler
{
    public static class ContradictionJsonHtmlFormatter
    {
        private const int MaxJsonDepth = 40;

        /// <summary>
        /// Exact JSON property name → display label for &lt;th&gt; cells when the generic PascalCase split is wrong.
        /// </summary>
        private static readonly Dictionary<string, string> HeaderLabelOverrides =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TechSkillScoreContradictionWithAI"] = "TechSkillScore Contradiction With AI",
            };

        /// <summary>
        /// Makes JSON property names readable in table headers (e.g. camelCase and snake_case → spaced words).
        /// </summary>
        private static string FormatHeaderLabel(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (HeaderLabelOverrides.TryGetValue(name, out var overridden))
            {
                return overridden;
            }

            var s = name.Replace('_', ' ').Replace('-', ' ');
            s = Regex.Replace(s, "([a-z])([A-Z])", "$1 $2");
            s = Regex.Replace(s, "([A-Z])([A-Z][a-z])", "$1 $2");
            s = Regex.Replace(s, "([a-zA-Z])([0-9])", "$1 $2");
            s = Regex.Replace(s, "([0-9])([a-zA-Z])", "$1 $2");
            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }

            return s.Trim();
        }

        public static string FormatContradictionJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "<p class=\"text-muted mb-0\">No contradiction report data is available for this interview.</p>";
            }

            var trimmed = json.Trim();
            try
            {
                var token = JToken.Parse(trimmed);
                var sb = new StringBuilder();
                sb.Append("<div class=\"contradiction-json-root\">");
                sb.Append(RenderJsonToken(token, 0));
                sb.Append("</div>");
                return sb.ToString();
            }
            catch
            {
                return "<div class=\"alert alert-warning\">The stored contradiction data could not be parsed as JSON. Raw content is shown below.</div>"
                    + "<pre class=\"border rounded p-3 bg-light small mb-0\">" + WebUtility.HtmlEncode(trimmed) + "</pre>";
            }
        }

        private static string RenderJsonToken(JToken token, int depth)
        {
            if (depth > MaxJsonDepth)
            {
                return "<span class=\"text-muted\">…</span>";
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    return RenderJsonObject((JObject)token, depth);
                case JTokenType.Array:
                    return RenderJsonArray((JArray)token, depth);
                default:
                    var s = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
                    return "<span class=\"json-leaf\">" + WebUtility.HtmlEncode(s ?? string.Empty) + "</span>";
            }
        }

        private static string RenderJsonObject(JObject obj, int depth)
        {
            if (obj.Count == 0)
            {
                return "<span class=\"text-muted\">{ }</span>";
            }

            var sb = new StringBuilder();
            var firstTableClass = depth == 0 ? " contradiction-json-first-table" : string.Empty;
            sb.Append("<div class=\"table-responsive mb-2").Append(firstTableClass).Append("\"><table class=\"table table-sm table-bordered contradiction-json-table mb-0\">");
            foreach (var prop in obj.Properties())
            {
                sb.Append("<tr><th class=\"bg-light text-wrap contradiction-json-th-row\" style=\"min-width:140px;max-width:280px;\">");
                sb.Append(WebUtility.HtmlEncode(FormatHeaderLabel(prop.Name)));
                sb.Append("</th><td>");
                sb.Append(RenderJsonToken(prop.Value, depth + 1));
                sb.Append("</td></tr>");
            }

            sb.Append("</table></div>");
            return sb.ToString();
        }

        private static string RenderJsonArray(JArray arr, int depth)
        {
            if (arr.Count == 0)
            {
                return "<span class=\"text-muted\">(empty list)</span>";
            }

            if (TryRenderUniformObjectArrayAsTable(arr, depth, out var tableHtml))
            {
                return tableHtml;
            }

            var sb = new StringBuilder();
            sb.Append("<ol class=\"contradiction-json-ol mb-0 ps-3\">");
            foreach (var item in arr)
            {
                sb.Append("<li class=\"mb-2\">");
                sb.Append(RenderJsonToken(item, depth + 1));
                sb.Append("</li>");
            }

            sb.Append("</ol>");
            return sb.ToString();
        }

        private static bool TryRenderUniformObjectArrayAsTable(JArray arr, int depth, out string html)
        {
            html = null;
            if (arr.Count == 0)
            {
                return false;
            }

            var objects = new List<JObject>();
            foreach (var t in arr)
            {
                if (t is not JObject jo)
                {
                    return false;
                }

                objects.Add(jo);
            }

            var firstKeys = objects[0].Properties().Select(p => p.Name).ToList();
            if (firstKeys.Count == 0 || firstKeys.Count > 16)
            {
                return false;
            }

            var firstSet = new HashSet<string>(firstKeys);
            foreach (var o in objects.Skip(1))
            {
                var keys = o.Properties().Select(p => p.Name).ToList();
                if (keys.Count != firstSet.Count)
                {
                    return false;
                }

                foreach (var k in keys)
                {
                    if (!firstSet.Contains(k))
                    {
                        return false;
                    }
                }
            }

            var sb = new StringBuilder();
            var firstTableClass = depth == 0 ? " contradiction-json-first-table" : string.Empty;
            sb.Append("<div class=\"table-responsive mb-2").Append(firstTableClass).Append("\"><table class=\"table table-striped table-bordered table-sm contradiction-json-array-table mb-0\"><thead><tr>");
            foreach (var key in firstKeys)
            {
                sb.Append("<th class=\"bg-light\">");
                sb.Append(WebUtility.HtmlEncode(FormatHeaderLabel(key)));
                sb.Append("</th>");
            }

            sb.Append("</tr></thead><tbody>");
            foreach (var o in objects)
            {
                sb.Append("<tr>");
                foreach (var key in firstKeys)
                {
                    sb.Append("<td>");
                    var v = o[key];
                    if (v == null)
                    {
                        sb.Append("<span class=\"text-muted\">—</span>");
                    }
                    else
                    {
                        sb.Append(RenderJsonToken(v, depth + 1));
                    }

                    sb.Append("</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            html = sb.ToString();
            return true;
        }
    }
}
