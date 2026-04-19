using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Schema for Report AI QC analysis response (bucket_results, findings, final_score, final_verdict, etc.).
    /// </summary>
    public class ReportAIQCResponseSchema
    {
        public string auditable { get; set; }
        public List<string> auditability_fail_reasons { get; set; }
        public string extraction_confidence { get; set; }
        public DerivedMetrics derived_metrics { get; set; }
        public Dictionary<string, BucketResult> bucket_results { get; set; }
        public List<Finding> findings { get; set; }
        public List<string> flags { get; set; }
        public string audit_summary { get; set; }
        public int final_score { get; set; }
        public string final_verdict { get; set; }
        public string audit_confidence { get; set; }
        public string manual_review_recommended { get; set; }
    }

    public class DerivedMetrics
    {
        public double tech_skill_percentage { get; set; }
        public int skill_coverage_percentage { get; set; }
        public int score_mismatch { get; set; }
        public string status_band_from_score { get; set; }
        public SkillCoverageAnalysis skill_coverage_analysis { get; set; }
    }

    public class SkillCoverageAnalysis
    {
        public List<string> expected_skills { get; set; }
        public Dictionary<string, string> skill_status { get; set; }
    }

    public class BucketResult
    {
        public int score { get; set; }
        public string verdict { get; set; }
        public string observation { get; set; }
        public string confidence { get; set; }
    }

    public class Finding
    {
        public string issue { get; set; }
        public string bucket { get; set; }
        public int penalty { get; set; }
        public List<string> evidence { get; set; }
        public string reasoning { get; set; }
        public string confidence { get; set; }
    }
}
