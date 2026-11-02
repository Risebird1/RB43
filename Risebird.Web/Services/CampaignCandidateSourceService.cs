using Microsoft.Extensions.Caching.Memory;
using Risebird.Business.Interface;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Services
{
    /// <summary>
    /// The candidate lists a campaign is created from - By IRF and the centralized Not-scheduled, Reschedule and
    /// Unconfirmed lists - shared by the PlivoCX and WhatsApp campaign controllers. Each caller passes its own
    /// cache-key prefix so the two features never share a cached copy.
    /// </summary>
    public interface ICampaignCandidateSourceService
    {
        Task<List<PlivoCXCampaignIRFOption>> GetActiveIRFsAsync();
        Task<List<PlivoCXCampaignIRFPickerRow>> GetIRFRowsAsync(int irfId);
        Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> NotScheduledAsync(string cachePrefix, string email, bool refresh);
        Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> RescheduleAsync(string cachePrefix, string email, DateTime? startDate, DateTime? endDate, bool refresh);
        Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> UnconfirmedAsync(string cachePrefix, bool refresh);
    }

    public class CampaignCandidateSourceService : ICampaignCandidateSourceService
    {
        private readonly IPlivoCXCampaignRepository _campaignRepository;
        private readonly IMeetingService            _meetingService;
        private readonly IDashboardService          _dashboardService;
        private readonly IMemoryCache               _cache;

        // The centralized lists are slow (usp_GetClientNotScheduledMeetings runs with a 10-minute
        // timeout across every channel), so each user's result is kept briefly and an in-flight
        // load is shared rather than started twice.
        private static readonly TimeSpan ListCacheFor = TimeSpan.FromMinutes(10);

        public CampaignCandidateSourceService(
            IPlivoCXCampaignRepository campaignRepository,
            IMeetingService            meetingService,
            IDashboardService          dashboardService,
            IMemoryCache               cache)
        {
            _campaignRepository = campaignRepository;
            _meetingService     = meetingService;
            _dashboardService   = dashboardService;
            _cache              = cache;
        }

        public Task<List<PlivoCXCampaignIRFOption>> GetActiveIRFsAsync() => _campaignRepository.GetActiveIRFsAsync();

        public Task<List<PlivoCXCampaignIRFPickerRow>> GetIRFRowsAsync(int irfId) => _campaignRepository.GetPickerRowsForIRFAsync(irfId);

        /// <summary>Not-scheduled list (/Client/NotScheduledMeetings), across clients for internal users.</summary>
        public Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> NotScheduledAsync(string cachePrefix, string email, bool refresh) =>
            CachedListAsync(cachePrefix, "NotScheduled|" + email, refresh, async () =>
            {
                // Same list as /Client/NotScheduledMeetings, from a lean procedure: the page's own
                // query takes minutes across every channel.
                var rows = await _campaignRepository.GetNotScheduledAsync(email);
                return Dedupe(rows.Select(r => new PlivoCXCampaignPickerItem
                {
                    CandidateId = r.CandidateId,
                    Name        = r.CandidateName,
                    Email       = r.CandidateEmail,
                    Phone       = r.CandidatePhone,
                    IrfId       = r.IRFId,
                    IrfName     = r.IRFName,
                    JobTitle    = r.JobTitle,
                    ClientName  = r.ClientName,
                    Eligibility = string.IsNullOrWhiteSpace(r.CandidatePhone) ? "no-number" : "eligible",
                    Selected    = !string.IsNullOrWhiteSpace(r.CandidatePhone)
                }));
            });

        /// <summary>Reschedule list (/Client/ReScheduleMeetings) for a meeting-date range (default: the last month).</summary>
        public Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> RescheduleAsync(string cachePrefix, string email, DateTime? startDate, DateTime? endDate, bool refresh)
        {
            var start = (startDate ?? DateTime.Now.AddMonths(-1)).Date;
            var end = (endDate ?? DateTime.Now).Date.AddDays(1); // the list pages make the end date inclusive this way
            return CachedListAsync(cachePrefix, $"Reschedule|{email}|{start:yyyyMMdd}|{end:yyyyMMdd}", refresh, async () =>
            {
                var rows = await _meetingService.GetClientReScheduledMeetingList(0, email, start, end) ?? new List<MeetingVM>();
                // dtCreatedOn is MAX(interviewDate) here - the meeting being rescheduled.
                return Dedupe(rows.Select(m => ToPickerItem(m, m.imid > 0 ? m.imid : m.irfIntId, null,
                    m.dtCreatedOn > DateTime.MinValue ? m.dtCreatedOn.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : null, m.ClientName)));
            });
        }

        /// <summary>Unconfirmed list (/Dashboard/UnconfirmedCandidates): Ops-wide, meetings in the next 7 days.</summary>
        public Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> UnconfirmedAsync(string cachePrefix, bool refresh) =>
            // Ops-wide, so one cached copy serves every user.
            CachedListAsync(cachePrefix, "Unconfirmed", refresh, async () =>
            {
                var rows = await _dashboardService.GetCandidatesWhoHaventConfirmed() ?? Enumerable.Empty<MeetingVM>();
                // Here imid is the InterviewMeetings id and irfIntId the IRF id.
                return Dedupe(rows.Select(m => ToPickerItem(m, m.irfIntId, m.imid,
                    m.interviewDate > DateTime.MinValue ? m.interviewDate.ToString("dd MMM yyyy, h:mm tt", CultureInfo.InvariantCulture) : null,
                    m.ClientName ?? m.Client)));
            });

        /// <summary>
        /// A list result from the cache, or from <paramref name="load"/> - shared by concurrent
        /// callers while it runs. <paramref name="refresh"/> drops the cached copy first. A failed
        /// load is not cached.
        /// </summary>
        private async Task<(List<PlivoCXCampaignPickerItem> Items, DateTime LoadedAtUtc)> CachedListAsync(
            string cachePrefix, string key, bool refresh, Func<Task<List<PlivoCXCampaignPickerItem>>> load)
        {
            key = cachePrefix + ":list:" + key;
            if (refresh) _cache.Remove(key);
            var entry = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = ListCacheFor;
                return new Lazy<Task<(List<PlivoCXCampaignPickerItem>, DateTime)>>(async () => (await load(), DateTime.UtcNow));
            });
            try
            {
                return await entry.Value;
            }
            catch
            {
                _cache.Remove(key);
                throw;
            }
        }

        public static string LoadedAtText(DateTime loadedAtUtc)
        {
            var age = DateTime.UtcNow - loadedAtUtc;
            return age.TotalSeconds < 30 ? "just now" : (int)Math.Max(1, Math.Round(age.TotalMinutes)) + " min ago";
        }

        /// <summary>MeetingVM -> picker row. No number = Cold; no candidate record = cannot be called.</summary>
        private static PlivoCXCampaignPickerItem ToPickerItem(MeetingVM m, int irfId, int? interviewId, string originalDate, string clientName)
        {
            var item = new PlivoCXCampaignPickerItem
            {
                CandidateId  = m.CandidateId,
                Name         = m.candidateName?.Trim(),
                Email        = m.candidateEmail,
                Phone        = m.MobileNumber,
                IrfId        = irfId > 0 ? irfId : (int?)null,
                IrfName      = m.IRFId,
                JobTitle     = m.JDTiltle,
                ClientName   = clientName,
                InterviewId  = interviewId > 0 ? interviewId : null,
                OriginalDate = originalDate
            };
            if (m.CandidateId <= 0) { item.Eligibility = "not-eligible"; item.Note = "no candidate record on the IRF"; }
            else if (string.IsNullOrWhiteSpace(m.MobileNumber)) item.Eligibility = "no-number";
            else item.Eligibility = "eligible";
            item.Selected = item.Eligibility == "eligible";
            return item;
        }

        // The lists can return a candidate once per meeting; a campaign holds each candidate once.
        private static List<PlivoCXCampaignPickerItem> Dedupe(IEnumerable<PlivoCXCampaignPickerItem> items) =>
            items.GroupBy(i => i.CandidateId > 0 ? "c" + i.CandidateId : "e" + i.Email + "|" + i.IrfId)
                 .Select(g => g.First()).ToList();
    }
}
