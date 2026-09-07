using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.Web.Middleware
{
    /// <summary>
    /// Turns a failed AI interview report into a readable error instead of a login page.
    ///
    /// Outside Development the app is configured with <c>UseExceptionHandler("/Home/Index")</c>,
    /// so any unhandled exception re-executes the site root, which bounces the reader to sign-in.
    /// A crashed report is therefore indistinguishable from an expired session, and the real
    /// exception is visible only to whoever reads the server log.
    ///
    /// AIInterviewController.Report already wraps its own body in a try/catch. That is not enough:
    /// an action returning a ViewResult has not rendered anything yet - Razor executes after the
    /// action returns, during result execution - and MVC exception filters do not cover result
    /// execution either. A null dereference inside Report.cshtml escapes both. This middleware sits
    /// inside UseExceptionHandler, so it sees such an exception first and answers it here.
    ///
    /// Deliberately scoped to the report path: every other route keeps the app's existing
    /// behaviour, so this cannot change how the rest of the site handles errors.
    /// </summary>
    public class AIInterviewReportErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AIInterviewReportErrorMiddleware> _logger;

        public AIInterviewReportErrorMiddleware(RequestDelegate next, ILogger<AIInterviewReportErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!IsReportRequest(context))
            {
                await _next(context);
                return;
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var sessionId = context.Request.Query["seId"].ToString();

                _logger.LogError(ex,
                    "AI interview report failed to render for session {SessionId} ({Path}{Query}).",
                    string.IsNullOrEmpty(sessionId) ? "(unknown)" : sessionId,
                    context.Request.Path, context.Request.QueryString);

                // The response may already be partially written - Razor streams as it renders - in
                // which case the status line is long gone and the only honest thing left is to stop.
                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "text/html; charset=utf-8";

                await context.Response.WriteAsync(BuildErrorPage(sessionId));
            }
        }

        private static bool IsReportRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            return path.StartsWith("/AIInterview/Report", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/AIInterview/GenerateAIReportPDFFile", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildErrorPage(string sessionId)
        {
            var quoted = string.IsNullOrWhiteSpace(sessionId)
                ? string.Empty
                : "<p style=\"color:#6b7280;font-size:14px;margin:18px 0 0;\">Quote session "
                  + WebUtility.HtmlEncode(sessionId) + " when reporting this.</p>";

            return @"<!doctype html>
<html><head><meta charset=""utf-8""><title>Report unavailable</title>
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
</head>
<body style=""margin:0;font-family:-apple-system,Segoe UI,Roboto,sans-serif;background:#f7f8fa;"">
  <div style=""max-width:560px;margin:14vh auto;padding:32px;background:#fff;border:1px solid #e5e7eb;border-radius:12px;"">
    <h1 style=""margin:0 0 12px;font-size:20px;color:#111827;"">This report could not be generated</h1>
    <p style=""margin:0;color:#374151;line-height:1.6;"">
      Something failed while building the report. The error has been logged with the session id, and
      your session is still signed in - this is not a sign-out.
    </p>" + quoted + @"
  </div>
</body></html>";
        }
    }
}
