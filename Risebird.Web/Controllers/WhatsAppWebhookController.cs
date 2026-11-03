using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Interakt webhook - records a WhatsApp "Stop" reply as an opt-out (WhatsAppOptOut.Source = 'StopKeyword').
    /// Configure in Interakt → Settings → Developer Settings → Webhooks:
    ///   URL     https://portal.risebird.io/WhatsAppWebhook/Interakt
    ///   Secret  the same value as the app setting WhatsApp:WebhookSecret
    /// with the "message_received" event on. Every other event is acknowledged and ignored. Interakt waits 3 seconds
    /// and does not retry, so the endpoint always answers 200 for a signed call.
    ///
    /// A Stop is not tied to one role, so a candidate is opted out of WhatsApp for every IRF and marked cold on each
    /// (ColdMarkedBY 'WhatsApp Stop reply', booked interviews kept); an interviewer is opted out of interviewer messages.
    /// </summary>
    [AllowAnonymous]
    [Route("WhatsAppWebhook")]
    public class WhatsAppWebhookController : Controller
    {
        private readonly IConfiguration          _configuration;
        private readonly IWhatsappRepository     _whatsappRepository;
        private readonly IApplicationErrorLogger _logger;

        public WhatsAppWebhookController(IConfiguration configuration, IWhatsappRepository whatsappRepository, IApplicationErrorLogger logger)
        {
            _configuration      = configuration;
            _whatsappRepository = whatsappRepository;
            _logger             = logger;
        }

        [HttpPost("Interakt")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Interakt()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body))
                rawBody = await reader.ReadToEndAsync();

            var secret = _configuration["WhatsApp:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                // Never act on an unsigned call; the setting is missing, not the caller wrong.
                await _logger.LogMessage(LogLevelInfo.Error, "Interakt webhook received but WhatsApp:WebhookSecret is not set - ignored.");
                return Ok();
            }
            var signature = Request.Headers.TryGetValue(InteraktWebhook.SignatureHeader, out var values) ? values.ToString() : null;
            if (!InteraktWebhook.VerifySignature(rawBody, signature, secret))
                return StatusCode(401, "Invalid signature.");

            try
            {
                var message = InteraktWebhook.IncomingMessage(JObject.Parse(rawBody));
                if (message == null || !InteraktWebhook.IsStop(message.Value.Text))
                    return Ok();

                var lastDigits = InteraktWebhook.LastDigits(message.Value.Phone);
                var owners = await _whatsappRepository.FindUsersByWhatsAppNumberAsync(lastDigits);
                foreach (var owner in owners)
                {
                    if (owner.IsCandidate)
                    {
                        await _whatsappRepository.SaveOptOutAsync(WhatsAppHelper.AudienceCandidate, owner.Email, null, null, "StopKeyword");
                        await _whatsappRepository.MarkCandidateColdAsync(owner.Email, null, "WhatsApp Stop reply", "WhatsApp Stop reply", "WhatsApp Stop reply");
                    }
                    else
                    {
                        await _whatsappRepository.SaveOptOutAsync(WhatsAppHelper.AudienceInterviewer, owner.Email, null, null, "StopKeyword");
                        await _whatsappRepository.MarkInterviewerChurnAsync(owner.Email, "WhatsApp Stop reply");
                    }
                }
                await _logger.LogMessage(LogLevelInfo.Info,
                    "Interakt Stop reply from ...." + (lastDigits.Length >= 4 ? lastDigits.Substring(lastDigits.Length - 4) : lastDigits)
                    + ": opted out " + (owners.Count == 0 ? "nobody (number not on any portal user)" : string.Join(", ", owners.Select(o => o.Email + (o.IsCandidate ? " (candidate)" : " (interviewer)")))));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return Ok();
        }
    }
}
