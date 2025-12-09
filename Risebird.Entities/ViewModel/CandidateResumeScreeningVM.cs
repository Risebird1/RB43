using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    //CandidateResumeScreeningVM
    public class CandidateResumeScreeningVM
    {
        [JsonProperty("mustHaveTechnicalCriteria")]
        public List<MustHaveTechnicalCriterion> MustHaveTechnicalCriteria { get; set; }

        [JsonProperty("evaluationCriteriaScores")]
        public List<EvaluationCriteriaScore> EvaluationCriteriaScores { get; set; }

        [JsonProperty("sectionScores")]
        public SectionScores SectionScores { get; set; }

        [JsonProperty("totalScore")]
        public int TotalScore { get; set; }

        [JsonProperty("maxScore")]
        public int MaxScore { get; set; }

        [JsonProperty("disqualificationFlags")]
        public DisqualificationFlags DisqualificationFlags { get; set; }

        [JsonProperty("stabilityNotes")]
        public string StabilityNotes { get; set; }

        [JsonProperty("employerFraudNotes")]
        public string EmployerFraudNotes { get; set; }

        [JsonProperty("fitmentSummary")]
        public string FitmentSummary { get; set; }

        [JsonProperty("verdict")]
        public string Verdict { get; set; }
    }

    public class MustHaveTechnicalCriterion
    {
        [JsonProperty("criterionId")]
        public string CriterionId { get; set; }

        [JsonProperty("technicalSkill")]
        public string TechnicalSkill { get; set; }

        [JsonProperty("depthExpected")]
        public string DepthExpected { get; set; }

        [JsonProperty("complexityExpected")]
        public string ComplexityExpected { get; set; }

        [JsonProperty("isPrimarySkill")]
        public bool IsPrimarySkill { get; set; }
    }

    public class EvaluationCriteriaScore
    {
        [JsonProperty("criterionId")]
        public string CriterionId { get; set; }

        [JsonProperty("technicalSkill")]
        public string TechnicalSkill { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("maxScore")]
        public int MaxScore { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class SectionScore
    {
        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("maxScore")]
        public int MaxScore { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class SectionScores
    {
        [JsonProperty("technicalSkillsMatch")]
        public SectionScore TechnicalSkillsMatch { get; set; }

        [JsonProperty("realTimeImplementationExperience")]
        public SectionScore RealTimeImplementationExperience { get; set; }

        [JsonProperty("employerCredibility")]
        public SectionScore EmployerCredibility { get; set; }

        [JsonProperty("primarySpecialization")]
        public SectionScore PrimarySpecialization { get; set; }

        [JsonProperty("recentHandsOnExperience")]
        public SectionScore RecentHandsOnExperience { get; set; }

        [JsonProperty("educationalBackground")]
        public SectionScore EducationalBackground { get; set; }

        [JsonProperty("certificationsAndTraining")]
        public SectionScore CertificationsAndTraining { get; set; }

        [JsonProperty("multipleSpecializationsAdjustment")]
        public SectionScore MultipleSpecializationsAdjustment { get; set; }

        [JsonProperty("teachingRoleAdjustment")]
        public SectionScore TeachingRoleAdjustment { get; set; }

        [JsonProperty("employmentEducationGapsAdjustment")]
        public SectionScore EmploymentEducationGapsAdjustment { get; set; }

        [JsonProperty("freelancingRoleAdjustment")]
        public SectionScore FreelancingRoleAdjustment { get; set; }

        [JsonProperty("primarySpecializationCurrentJobAdjustment")]
        public SectionScore PrimarySpecializationCurrentJobAdjustment { get; set; }
    }

    public class DisqualificationFlags
    {
        [JsonProperty("zeroRealTimeAndRecentHandsOn")]
        public bool ZeroRealTimeAndRecentHandsOn { get; set; }

        [JsonProperty("employerFraudSuspected")]
        public bool EmployerFraudSuspected { get; set; }

        [JsonProperty("stabilityConcerns")]
        public bool StabilityConcerns { get; set; }
    }
}




