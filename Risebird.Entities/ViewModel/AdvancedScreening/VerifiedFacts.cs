using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel.AdvancedScreening
{
    /// <summary>
    /// Facts computed in code - never by the candidate or the model - and passed to the scorer as
    /// &lt;verified_facts&gt;. The scorer treats them as ground truth. Stored as
    /// CandidateAdvancedScreening.FactsJson.
    /// </summary>
    public class VerifiedFacts
    {
        [JsonProperty("stability")]
        public StabilityFacts Stability { get; set; }

        [JsonProperty("companies")]
        public List<CompanyFact> Companies { get; set; } = new List<CompanyFact>();

        [JsonProperty("duplicates")]
        public DuplicateFacts Duplicates { get; set; }

        /// <summary>Share (0-1) of the resume's word shingles that also appear in the JD. Null when either text is missing.</summary>
        [JsonProperty("jdOverlap")]
        public decimal? JdOverlap { get; set; }

        [JsonProperty("dateIssues")]
        public List<string> DateIssues { get; set; } = new List<string>();

        [JsonProperty("previousVersions")]
        public List<PreviousVersionFact> PreviousVersions { get; set; } = new List<PreviousVersionFact>();

        /// <summary>Instruction-like text addressed to an AI that was found in the resume.</summary>
        [JsonProperty("suspectedInjection")]
        public List<string> SuspectedInjection { get; set; } = new List<string>();

        /// <summary>Which facts could not be computed, so the scorer can say "not verified".</summary>
        [JsonProperty("notVerified")]
        public List<string> NotVerified { get; set; } = new List<string>();
    }

    public class StabilityFacts
    {
        [JsonProperty("avgTenureMonths")]
        public decimal AvgTenureMonths { get; set; }

        [JsonProperty("jobsLast5Years")]
        public int JobsLast5Years { get; set; }

        [JsonProperty("jobsLast3Years")]
        public int JobsLast3Years { get; set; }

        [JsonProperty("shortestStintMonths")]
        public int ShortestStintMonths { get; set; }

        [JsonProperty("maxGapMonths")]
        public int MaxGapMonths { get; set; }

        [JsonProperty("overlappingFullTimeRoles")]
        public int OverlappingFullTimeRoles { get; set; }

        [JsonProperty("currentTenureMonths")]
        public int? CurrentTenureMonths { get; set; }

        [JsonProperty("rolesCounted")]
        public int RolesCounted { get; set; }
    }

    public class CompanyFact
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Null when no registry provider is configured (not verified).</summary>
        [JsonProperty("found")]
        public bool? Found { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("incorporated")]
        public string Incorporated { get; set; }

        [JsonProperty("employeeBand")]
        public string EmployeeBand { get; set; }

        [JsonProperty("blocklisted")]
        public bool Blocklisted { get; set; }

        [JsonProperty("blocklistReason")]
        public string BlocklistReason { get; set; }

        [JsonProperty("claimedStart")]
        public string ClaimedStart { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }

    public class DuplicateFacts
    {
        [JsonProperty("maxProjectSimilarity")]
        public decimal MaxProjectSimilarity { get; set; }

        /// <summary>Other people's resumes containing a project/summary paragraph at least 80% similar.</summary>
        [JsonProperty("matchingResumes")]
        public int MatchingResumes { get; set; }
    }

    public class PreviousVersionFact
    {
        [JsonProperty("irfId")]
        public int? IrfId { get; set; }

        [JsonProperty("differences")]
        public List<string> Differences { get; set; } = new List<string>();
    }

    // ------------------------------------------------------------------ work history

    /// <summary>One role extracted from a resume. Dates are "YYYY-MM"; End may be "present".</summary>
    public class WorkHistoryEntry
    {
        [JsonProperty("employer")]
        public string Employer { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("end")]
        public string End { get; set; }

        [JsonProperty("employmentType")]
        public string EmploymentType { get; set; }

        [JsonProperty("projects")]
        public List<WorkHistoryProject> Projects { get; set; } = new List<WorkHistoryProject>();
    }

    public class WorkHistoryProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("start")]
        public string Start { get; set; }

        [JsonProperty("end")]
        public string End { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary>The extractor's JSON object. It may also return a bare array; both are accepted.</summary>
    public class WorkHistoryEnvelope
    {
        [JsonProperty("workHistory")]
        public List<WorkHistoryEntry> WorkHistory { get; set; } = new List<WorkHistoryEntry>();

        [JsonProperty("summary")]
        public string Summary { get; set; }
    }

    /// <summary>usp_GetCandidateWorkHistories row.</summary>
    public class CandidateWorkHistoryRecord
    {
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
        public int? IRFId { get; set; }
        public string HistoryJson { get; set; }
        public DateTime? ExtractedOn { get; set; }
    }

    // ------------------------------------------------------------------ company registry

    /// <summary>A CompanyVerification row: cached registry lookup plus blocklist entry.</summary>
    public class CompanyVerificationRecord
    {
        public string NormalizedName { get; set; }
        public string DisplayName { get; set; }
        public bool? Found { get; set; }
        public string CIN { get; set; }
        public string Status { get; set; }
        public DateTime? IncorporationDate { get; set; }
        public DateTime? StruckOffDate { get; set; }
        public string EmployeeBand { get; set; }
        public string Website { get; set; }
        public int? DomainAgeMonths { get; set; }
        public string Source { get; set; }
        public DateTime? CheckedOn { get; set; }
        public bool IsBlocklisted { get; set; }
        public string BlocklistReason { get; set; }
        public string BlocklistedBy { get; set; }
        public DateTime? BlocklistedOn { get; set; }
    }

    /// <summary>A fingerprint returned by the duplicate search.</summary>
    public class ResumeFingerprintMatch
    {
        public int FingerprintId { get; set; }
        public int CandidateId { get; set; }
        public int? CandidateMasterId { get; set; }
        public string SectionType { get; set; }
        public string SectionName { get; set; }
        public byte[] MinHash { get; set; }
    }
}
