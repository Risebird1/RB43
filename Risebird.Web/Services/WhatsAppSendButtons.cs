using Microsoft.Extensions.Configuration;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// The WhatsApp send buttons on the candidate lists (Candidate list, Not Scheduled, Reschedule): marks each row
    /// opted out of WhatsApp for its IRF and with the sends of the page's stage used, in one query for the whole
    /// list, so the row icon renders grey with the reason. The send itself refuses these candidates either way.
    /// </summary>
    public static class WhatsAppSendButtons
    {
        public const string ScheduleEvent = "Schedule Interview";
        public const string RescheduleEvent = "Reschedule Interview";

        /// <summary>The per-IRF limit of the stage's event (WhatsApp:MaxNotScheduledSendsPerIrf / MaxRescheduleSendsPerIrf).</summary>
        public static int LimitFor(IConfiguration configuration, string eventName)
        {
            var key = eventName == RescheduleEvent ? "WhatsApp:MaxRescheduleSendsPerIrf" : "WhatsApp:MaxNotScheduledSendsPerIrf";
            return int.TryParse(configuration?[key], out var value) && value > 0 ? value : 3;
        }

        /// <summary>Not Scheduled / Reschedule rows: imid is the IRF id there.</summary>
        public static async Task AnnotateAsync(IWhatsappRepository repository, IEnumerable<MeetingVM> rows, string eventName)
        {
            var list = rows?.ToList() ?? new List<MeetingVM>();
            var status = await LookupAsync(repository, list.Select(m => (EmailOf(m), m.imid)), eventName);
            foreach (var m in list)
            {
                if (status.TryGetValue(Key(EmailOf(m), m.imid), out var s))
                {
                    m.WhatsAppOptedOut = s.OptedOut;
                    m.WhatsAppSendsUsed = s.SendsUsed;
                }
            }
        }

        /// <summary>Candidate list rows of one IRF.</summary>
        public static async Task AnnotateAsync(IWhatsappRepository repository, IEnumerable<Candidate> rows, int irfId, string eventName)
        {
            var list = rows?.ToList() ?? new List<Candidate>();
            var status = await LookupAsync(repository, list.Select(c => (c.EmailId, irfId)), eventName);
            foreach (var c in list)
            {
                if (status.TryGetValue(Key(c.EmailId, irfId), out var s))
                {
                    c.WhatsAppOptedOut = s.OptedOut;
                    c.WhatsAppSendsUsed = s.SendsUsed;
                }
            }
        }

        /// <summary>The icon's tooltip: why it is disabled, or the usage so far.</summary>
        public static string Tooltip(bool optedOut, int used, int limit) =>
            optedOut ? "Unsubscribed from WhatsApp for this role"
            : used >= limit ? "WhatsApp limit reached (" + used + "/" + limit + ")"
            : "WhatsApp invites sent: " + used + "/" + limit;

        public static bool Disabled(bool optedOut, int used, int limit) => optedOut || used >= limit;

        /// <summary>Fills the rows and sets ViewBag-ready limit, resolving the repository from the request.</summary>
        public static async Task<int> PrepareAsync(IServiceProvider services, IEnumerable<MeetingVM> rows, string eventName)
        {
            await AnnotateAsync(services.GetService(typeof(IWhatsappRepository)) as IWhatsappRepository, rows, eventName);
            return LimitFor(services.GetService(typeof(IConfiguration)) as IConfiguration, eventName);
        }

        public static async Task<int> PrepareAsync(IServiceProvider services, IEnumerable<Candidate> rows, int irfId, string eventName)
        {
            await AnnotateAsync(services.GetService(typeof(IWhatsappRepository)) as IWhatsappRepository, rows, irfId, eventName);
            return LimitFor(services.GetService(typeof(IConfiguration)) as IConfiguration, eventName);
        }

        private static string EmailOf(MeetingVM m) => m.candidateEmail ?? m.EmailId;

        private static string Key(string email, int irfId) => irfId + "|" + (email ?? "").Trim().ToLowerInvariant();

        private static async Task<Dictionary<string, Entities.Data.WhatsAppSendStatusRow>> LookupAsync(
            IWhatsappRepository repository, IEnumerable<(string Email, int IRFId)> keys, string eventName)
        {
            var result = new Dictionary<string, Entities.Data.WhatsAppSendStatusRow>();
            if (repository == null) return result;
            foreach (var row in await repository.GetSendStatusAsync(keys, eventName))
                result[Key(row.Email, row.IRFId)] = row;
            return result;
        }
    }
}
