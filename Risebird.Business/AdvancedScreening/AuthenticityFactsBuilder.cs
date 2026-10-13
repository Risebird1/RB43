using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel.AdvancedScreening;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// Produces &lt;verified_facts&gt; for one candidate: stability metrics, date checks, company
    /// registry and blocklist results, duplicate project text across other resumes, JD overlap,
    /// differences from the same person's earlier applications, and suspected injected
    /// instructions. The maths is in AuthenticityFacts; this class does the data access around it.
    /// </summary>
    public class AuthenticityFactsBuilder
    {
        private const int MaxCompaniesChecked = 8;
        private const int MinWordsToFingerprint = 30;
        private const decimal DuplicateThreshold = 0.8m;
        private static readonly TimeSpan RegistryCacheAge = TimeSpan.FromDays(90);

        private readonly IAdvancedScreeningRepository _repository;
        private readonly ICompanyRegistryClient _registry;

        public AuthenticityFactsBuilder(IAdvancedScreeningRepository repository, ICompanyRegistryClient registry)
        {
            _repository = repository;
            _registry = registry;
        }

        public async Task<VerifiedFacts> BuildAsync(CandidateAdvancedScreeningVM candidate, WorkHistoryEnvelope history,
            string resumeText, string jdText, IReadOnlyList<string> hiddenText, DateTime asOfUtc)
        {
            var roles = history?.WorkHistory ?? new List<WorkHistoryEntry>();
            var facts = new VerifiedFacts();

            facts.Stability = AuthenticityFacts.ComputeStability(roles, asOfUtc);
            if (facts.Stability == null)
            {
                facts.NotVerified.Add("stability: no dated roles could be extracted from the resume");
            }

            facts.DateIssues = AuthenticityFacts.CheckDates(roles, asOfUtc);
            facts.Companies = await CompanyFactsAsync(roles, facts);
            facts.Duplicates = await DuplicateFactsAsync(candidate, roles, history?.Summary);
            facts.JdOverlap = AuthenticityFacts.JdOverlap(resumeText, jdText);
            facts.PreviousVersions = await PreviousVersionsAsync(candidate, roles, asOfUtc);

            var injection = AuthenticityFacts.DetectInjection(resumeText);
            foreach (var run in hiddenText ?? Array.Empty<string>())
            {
                injection.Add("Hidden text in the PDF (white, tiny or invisible): " + run);
            }
            facts.SuspectedInjection = injection.Distinct().Take(6).ToList();

            return facts;
        }

        private async Task<List<CompanyFact>> CompanyFactsAsync(List<WorkHistoryEntry> roles, VerifiedFacts facts)
        {
            var result = new List<CompanyFact>();
            var employers = roles
                .Where(r => !string.IsNullOrWhiteSpace(r.Employer))
                .GroupBy(r => AuthenticityFacts.NormalizeCompanyName(r.Employer))
                .Where(g => g.Key.Length > 0)
                .Take(MaxCompaniesChecked)
                .ToList();

            var anyUnverified = false;
            foreach (var g in employers)
            {
                var displayName = g.First().Employer.Trim();
                var record = await _repository.GetCompanyVerification(g.Key);

                var stale = record == null || record.CheckedOn == null || DateTime.UtcNow - record.CheckedOn.Value > RegistryCacheAge;
                if (stale && _registry.IsConfigured)
                {
                    var looked = await _registry.LookupAsync(displayName);
                    if (looked != null)
                    {
                        looked.NormalizedName = g.Key;
                        looked.DisplayName ??= displayName;
                        looked.Source ??= _registry.ProviderName;
                        await _repository.SaveCompanyVerification(looked, setBlocklist: false);
                        looked.IsBlocklisted = record?.IsBlocklisted ?? false;
                        looked.BlocklistReason = record?.BlocklistReason;
                        record = looked;
                    }
                }

                var hasRegistryData = record != null && record.CheckedOn != null && record.Found != null;
                if (!hasRegistryData)
                {
                    anyUnverified = true;
                }

                result.Add(new CompanyFact
                {
                    Name = displayName,
                    Found = hasRegistryData ? record.Found : null,
                    Status = hasRegistryData ? record.Status : "not verified",
                    Incorporated = record?.IncorporationDate?.ToString("yyyy-MM"),
                    EmployeeBand = record?.EmployeeBand,
                    Blocklisted = record?.IsBlocklisted ?? false,
                    BlocklistReason = record?.IsBlocklisted == true ? record.BlocklistReason : null,
                    ClaimedStart = g.Select(r => r.Start).Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s).FirstOrDefault(),
                    Source = hasRegistryData ? record.Source : (record?.IsBlocklisted == true ? "internal blocklist" : "not verified")
                });
            }

            if (anyUnverified)
            {
                facts.NotVerified.Add(_registry.IsConfigured
                    ? "companyGenuineness: some employers could not be looked up in the company registry"
                    : "companyGenuineness: no company registry provider is configured; only the internal blocklist was checked");
            }
            return result;
        }

        private async Task<DuplicateFacts> DuplicateFactsAsync(CandidateAdvancedScreeningVM candidate, List<WorkHistoryEntry> roles, string summary)
        {
            var sections = roles
                .SelectMany(r => (r.Projects ?? new List<WorkHistoryProject>()).Select(p => (Type: "project", Name: p?.Name, Text: p?.Text)))
                .Concat(new[] { (Type: "summary", Name: "Summary", Text: summary) })
                .Where(s => AuthenticityFacts.Words(s.Text).Count >= MinWordsToFingerprint)
                .Take(15)
                .ToList();

            // Re-screening replaces this candidate's fingerprints rather than adding more.
            await _repository.DeleteResumeFingerprints(candidate.CandidateId);

            var maxSimilarity = 0m;
            var matchingPeople = new HashSet<string>();
            foreach (var section in sections)
            {
                var signature = MinHash.Signature(section.Text);
                if (signature == null) continue;
                var bands = MinHash.Bands(signature);

                foreach (var match in await _repository.FindSimilarResumeFingerprints(candidate.CandidateId, candidate.CandidateMasterId, bands))
                {
                    var similarity = MinHash.Similarity(signature, MinHash.FromBytes(match.MinHash));
                    maxSimilarity = Math.Max(maxSimilarity, similarity);
                    if (similarity >= DuplicateThreshold)
                    {
                        matchingPeople.Add(match.CandidateMasterId?.ToString() ?? "c" + match.CandidateId);
                    }
                }

                await _repository.SaveResumeFingerprint(candidate.CandidateId, candidate.CandidateMasterId, section.Type,
                    section.Name, MinHash.ToBytes(signature), bands);
            }

            return new DuplicateFacts { MaxProjectSimilarity = maxSimilarity, MatchingResumes = matchingPeople.Count };
        }

        private async Task<List<PreviousVersionFact>> PreviousVersionsAsync(CandidateAdvancedScreeningVM candidate, List<WorkHistoryEntry> roles, DateTime asOf)
        {
            var result = new List<PreviousVersionFact>();
            if (roles.Count == 0 || candidate.CandidateMasterId <= 0)
            {
                return result;
            }
            foreach (var previous in await _repository.GetCandidateWorkHistories(candidate.CandidateMasterId, candidate.CandidateId))
            {
                var parsed = WorkHistoryParser.Parse(previous.HistoryJson);
                var diffs = AuthenticityFacts.CompareVersions(roles, parsed?.WorkHistory ?? new List<WorkHistoryEntry>(), asOf);
                if (diffs.Count > 0)
                {
                    result.Add(new PreviousVersionFact { IrfId = previous.IRFId, Differences = diffs.Take(6).ToList() });
                }
            }
            return result;
        }
    }

    /// <summary>Reads the work-history extractor's JSON, which may be the envelope object or a bare array.</summary>
    public static class WorkHistoryParser
    {
        public static WorkHistoryEnvelope Parse(string raw)
        {
            var cleaned = AdvancedScreeningScorer.StripJsonFence(raw);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }
            try
            {
                if (cleaned.TrimStart().StartsWith("["))
                {
                    return new WorkHistoryEnvelope { WorkHistory = JsonConvert.DeserializeObject<List<WorkHistoryEntry>>(cleaned) ?? new List<WorkHistoryEntry>() };
                }
                var envelope = JsonConvert.DeserializeObject<WorkHistoryEnvelope>(cleaned);
                if (envelope != null)
                {
                    envelope.WorkHistory ??= new List<WorkHistoryEntry>();
                    envelope.WorkHistory = envelope.WorkHistory.Where(w => w != null).ToList();
                }
                return envelope;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
