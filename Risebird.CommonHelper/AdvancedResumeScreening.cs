using Newtonsoft.Json;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Advanced (rubric-based) resume screening, ported from screen.risebird.io.
    ///
    /// Everything the screener is told and everything read back from it goes through here, so the
    /// rubric review screen, the payload and the report cannot drift apart. It is a static helper
    /// over plain models - no database, no HTTP - so each mapping can be checked in isolation.
    ///
    /// Two deliberate differences from the MCQ product:
    /// - The complete rubric is always sent (MCQ's ScreeningRubricScope.CompleteRubric). MCQ keeps
    ///   an "as built" scope only so channels screened before Seniority Level, Company Genuinity and
    ///   Employment Type were sent keep reproducing their scores; no Risebird IRF has that history.
    /// - A criterion with a weight but no description is sent with weight 0 - MCQ applies this to
    ///   those three criteria only, here it applies to all. A weight against blank text tells the
    ///   model to allocate points against nothing, and the resulting score cannot be explained;
    ///   at zero the criterion drops out of the effective maximum instead.
    /// </summary>
    public static class AdvancedResumeScreening
    {
        public const int PointsBudget = 300;

        public const string SectionSkills = "Skills";
        public const string SectionWork = "Work experience";
        public const string SectionEducation = "Education";

        // ------------------------------------------------------------------ verdict

        public const string StronglyRecommended = "Strongly Recommended";
        public const string Recommended = "Recommended";
        public const string Consider = "Consider";
        public const string NotRecommended = "Not Recommended";

        /// <summary>Used when the model returns something outside both scales. Never coerced to a pass.</summary>
        public const string Unclassified = "Unclassified";

        /// <summary>
        /// Maps whatever verdict the model returned onto one fixed scale (MCQ's ScreeningVerdict).
        /// The stored prompt is pinned to neither "Recommended" nor "Excellent/Good" wording, so
        /// both are accepted; anything unrecognised becomes Unclassified rather than a guess, since
        /// a wrong "Recommended" is a hiring decision made on a parsing accident. Qualified forms
        /// are tested first because "not recommended" and "strongly recommended" both contain
        /// "recommended".
        /// </summary>
        public static string NormalizeVerdict(string verdict)
        {
            if (string.IsNullOrWhiteSpace(verdict))
            {
                return Unclassified;
            }

            var v = verdict.Trim().ToLowerInvariant();

            if (v.Contains("strongly") && v.Contains("recommend")) return StronglyRecommended;
            if (v.Contains("not") && v.Contains("recommend")) return NotRecommended;
            if (v.Contains("consider")) return Consider;
            if (v.Contains("recommend")) return Recommended;

            if (v.Contains("excellent")) return StronglyRecommended;
            if (v.Contains("below") && v.Contains("average")) return NotRecommended;
            if (v.Contains("good")) return Recommended;
            if (v.Contains("average")) return Consider;
            if (v.Contains("poor") || v.Contains("weak")) return NotRecommended;

            return Unclassified;
        }

        // ---------------------------------------------------------------- payload

        /// <summary>
        /// The JSON the advanced screener receives. Mapping is MCQ's ScreeningCriteriaSource with
        /// the complete rubric, including its column-to-key quirks (CompanyPercentile is sent as
        /// targetCompanies, Gaps as currentCompanyCredentialsMatch, EducationType as
        /// achievementReputedPublicExam, EducationGaps as additionalAcademicQualifications) - the
        /// stored prompt reads those keys, so they are the contract.
        /// </summary>
        public static CandidateScreeningInput BuildScreeningInput(ResumeScreeningCriteriaVM c)
        {
            var input = new CandidateScreeningInput();
            if (c == null)
            {
                return input;
            }

            var s = input.Skills_Section;
            (s.JD_Title, s.JD_Title_Score) = Criterion(c.Title, c.JDTitleMaxScore);
            (s.primarySkills, s.primarySkills_Score) = Criterion(c.PrimarySkill, c.PrimarySkillsMaxScore);
            (s.secondarySkills, s.secondarySkills_Score) = Criterion(c.SecondarySkill, c.SecondarySkillsMaxScore);
            (s.experienceLevel, s.experienceLevel_Score) = Criterion(c.WorkExperience, c.WorkExperienceMaxScore);
            (s.seniorityLevel, s.seniorityLevel_Score) = Criterion(c.SeniorityLevel, c.SeniorityLevelScore);

            var w = input.Work_Experience;
            (w.targetCompanies, w.targetCompanies_Score) = Criterion(c.CompanyPercentile, c.CompanyPercentileScore);
            (w.Current_Company_Genuinity, w.Current_Company_Genuinity_Score) = Criterion(c.CompanyGenuinity, c.CompanyGenuinityScore);
            (w.Employment_Type, w.Employment_Type_Score) = Criterion(c.EmploymentType, c.EmploymentTypeScore);
            (w.verifiedImpactSkillCredibility, w.verifiedImpactSkillCredibility_Score) = Criterion(c.VerifiedImpactSkillCredibility, c.VerifiedImpactSkillCredibilityScore);
            (w.currentProjectRelevanceCount, w.currentProjectRelevanceCount_Score) = Criterion(c.CurrentProjectRelevanceCount, c.CurrentProjectRelevanceCountScore);
            (w.careerGraphCount, w.careerGraphCount_Score) = Criterion(c.CareerGraphCount, c.CareerGraphCountScore);
            (w.mustHavesCurrentWork, w.mustHavesCurrentWork_Score) = Criterion(c.MustHavesCurrentWork, c.MustHavesCurrentWorkScore);
            (w.jobFunctions, w.jobFunctions_Score) = Criterion(c.JobFunction, c.JobFunctionScore);
            (w.workEnvironmentFit, w.workEnvironmentFit_Score) = Criterion(c.WorkEnvironmentFit, c.WorkEnvironmentFitScore);
            (w.relevantIndustries, w.relevantIndustries_Score) = Criterion(c.RelevantIndustries, c.RelevantIndustriesScore);
            (w.relevantTools, w.relevantTools_Score) = Criterion(c.RelevantTools, c.RelevantToolsScore);
            (w.KPIs, w.KPIs_Score) = Criterion(c.KPIs, c.KPIsScore);
            (w.KRAs, w.KRAs_Score) = Criterion(c.KRAs, c.KRAsScore);
            (w.currentCompanyCredentialsMatch, w.currentCompanyCredentialsMatch_Score) = Criterion(c.Gaps, c.GapsScore);
            (w.StabilityIndicators, w.StabilityIndicators_Score) = Criterion(c.StabilityIndicators, c.StabilityIndicatorsScore);

            var e = input.Education;
            (e.targetGraduationInstitutions, e.targetGraduationInstitutions_Score) = Criterion(c.TargetEducationInstitution, c.TargetEducationInstitutionScore);
            (e.highestEducationQualification, e.highestEducationQualification_Score) = Criterion(c.HighestEducationQualification, c.HighestEducationQualificationScore);
            (e.achievementReputedPublicExam, e.achievementReputedPublicExam_Score) = Criterion(c.EducationType, c.EducationTypeScore);
            (e.educationSpecialization, e.educationSpecialization_Score) = Criterion(c.EducationSpecialization, c.EducationSpecializationScore);
            (e.industryCertifications, e.industryCertifications_Score) = Criterion(c.EducationCertification, c.EducationCertificateScore);
            (e.additionalAcademicQualifications, e.additionalAcademicQualifications_Score) = Criterion(c.EducationGaps, c.EducationGapsScore);

            return input;
        }

        /// <summary>Null text becomes "" (MCQ's convention); blank text sends weight 0, and a weight is never negative.</summary>
        private static (string text, int weight) Criterion(string text, int weight)
        {
            return string.IsNullOrWhiteSpace(text)
                ? ("", 0)
                : (text.Trim(), Math.Max(0, weight));
        }

        // ------------------------------------------------------- generated rubric

        /// <summary>
        /// Copies a rubric produced by the criteria generator onto an IRF's criteria record.
        /// MCQ's ScreeningProjectController.MapGeneratedCriteriaOntoChannel, which - unlike the
        /// older skill-channel wizard - also keeps Seniority Level and Industry Certifications.
        /// </summary>
        public static void ApplyGeneratedCriteria(CandidateScreeningInput g, ResumeScreeningCriteriaVM t)
        {
            if (g == null || t == null)
            {
                return;
            }

            var s = g.Skills_Section ?? new CandidateScreeningSkills();
            var w = g.Work_Experience ?? new CandidateScreeningWorkExperience();
            var e = g.Education ?? new CandidateScreeningEducation();

            t.Title = s.JD_Title;
            t.PrimarySkill = s.primarySkills;
            t.SecondarySkill = s.secondarySkills;
            t.WorkExperience = s.experienceLevel;
            t.SeniorityLevel = s.seniorityLevel;
            t.JDTitleMaxScore = s.JD_Title_Score;
            t.PrimarySkillsMaxScore = s.primarySkills_Score;
            t.SecondarySkillsMaxScore = s.secondarySkills_Score;
            t.WorkExperienceMaxScore = s.experienceLevel_Score;
            t.SeniorityLevelScore = s.seniorityLevel_Score;

            t.CompanyPercentile = w.targetCompanies;
            t.CompanyGenuinity = w.Current_Company_Genuinity;
            t.EmploymentType = w.Employment_Type;
            t.WorkEnvironmentFit = w.workEnvironmentFit;
            t.RelevantIndustries = w.relevantIndustries;
            t.RelevantTools = w.relevantTools;
            t.KPIs = w.KPIs;
            t.KRAs = w.KRAs;
            t.Gaps = w.currentCompanyCredentialsMatch;
            t.StabilityIndicators = w.StabilityIndicators;
            t.MustHavesCurrentWork = w.mustHavesCurrentWork;
            t.JobFunction = w.jobFunctions;
            t.CareerGraphCount = w.careerGraphCount;
            t.VerifiedImpactSkillCredibility = w.verifiedImpactSkillCredibility;
            t.CurrentProjectRelevanceCount = w.currentProjectRelevanceCount;
            t.CompanyPercentileScore = w.targetCompanies_Score;
            t.CompanyGenuinityScore = w.Current_Company_Genuinity_Score;
            t.EmploymentTypeScore = w.Employment_Type_Score;
            t.WorkEnvironmentFitScore = w.workEnvironmentFit_Score;
            t.RelevantIndustriesScore = w.relevantIndustries_Score;
            t.RelevantToolsScore = w.relevantTools_Score;
            t.KPIsScore = w.KPIs_Score;
            t.KRAsScore = w.KRAs_Score;
            t.GapsScore = w.currentCompanyCredentialsMatch_Score;
            t.StabilityIndicatorsScore = w.StabilityIndicators_Score;
            t.MustHavesCurrentWorkScore = w.mustHavesCurrentWork_Score;
            t.JobFunctionScore = w.jobFunctions_Score;
            t.CareerGraphCountScore = w.careerGraphCount_Score;
            t.VerifiedImpactSkillCredibilityScore = w.verifiedImpactSkillCredibility_Score;
            t.CurrentProjectRelevanceCountScore = w.currentProjectRelevanceCount_Score;

            t.TargetEducationInstitution = e.targetGraduationInstitutions;
            t.HighestEducationQualification = e.highestEducationQualification;
            t.EducationType = e.achievementReputedPublicExam;
            t.EducationSpecialization = e.educationSpecialization;
            t.EducationGaps = e.additionalAcademicQualifications;
            t.EducationCertification = e.industryCertifications;
            t.TargetEducationInstitutionScore = e.targetGraduationInstitutions_Score;
            t.HighestEducationQualificationScore = e.highestEducationQualification_Score;
            t.EducationTypeScore = e.achievementReputedPublicExam_Score;
            t.EducationSpecializationScore = e.educationSpecialization_Score;
            t.EducationGapsScore = e.additionalAcademicQualifications_Score;
            t.EducationCertificateScore = e.industryCertifications_Score;
        }

        // ---------------------------------------------------------- review screen

        /// <summary>
        /// The 26 weights as data, for the review screen. One list rather than 26 blocks of markup,
        /// so every row shown is a criterion the payload actually sends. Field names are
        /// ResumeScreeningCriteriaVM property names, so the posted form binds straight back.
        /// </summary>
        public static List<ScreeningCriterionRow> BuildCriteriaRows(ResumeScreeningCriteriaVM m)
        {
            return new List<ScreeningCriterionRow>
            {
                Row(SectionSkills, "JD Title", nameof(m.Title), nameof(m.JDTitleMaxScore), m.Title, m.JDTitleMaxScore, required: true),
                Row(SectionSkills, "Primary Skills", nameof(m.PrimarySkill), nameof(m.PrimarySkillsMaxScore), m.PrimarySkill, m.PrimarySkillsMaxScore, required: true),
                Row(SectionSkills, "Secondary Skills", nameof(m.SecondarySkill), nameof(m.SecondarySkillsMaxScore), m.SecondarySkill, m.SecondarySkillsMaxScore),
                Row(SectionSkills, "Experience In Years", nameof(m.WorkExperience), nameof(m.WorkExperienceMaxScore), m.WorkExperience, m.WorkExperienceMaxScore),
                Row(SectionSkills, "Seniority Level", nameof(m.SeniorityLevel), nameof(m.SeniorityLevelScore), m.SeniorityLevel, m.SeniorityLevelScore),

                Row(SectionWork, "Target Company Percentile", nameof(m.CompanyPercentile), nameof(m.CompanyPercentileScore), m.CompanyPercentile, m.CompanyPercentileScore),
                Row(SectionWork, "Company Genuinity", nameof(m.CompanyGenuinity), nameof(m.CompanyGenuinityScore), m.CompanyGenuinity, m.CompanyGenuinityScore),
                Row(SectionWork, "Employment Type", nameof(m.EmploymentType), nameof(m.EmploymentTypeScore), m.EmploymentType, m.EmploymentTypeScore),
                Row(SectionWork, "Verified Impact & Skill Credibility", nameof(m.VerifiedImpactSkillCredibility), nameof(m.VerifiedImpactSkillCredibilityScore), m.VerifiedImpactSkillCredibility, m.VerifiedImpactSkillCredibilityScore),
                Row(SectionWork, "Job Functions", nameof(m.JobFunction), nameof(m.JobFunctionScore), m.JobFunction, m.JobFunctionScore),
                Row(SectionWork, "Current Project Relevance", nameof(m.CurrentProjectRelevanceCount), nameof(m.CurrentProjectRelevanceCountScore), m.CurrentProjectRelevanceCount, m.CurrentProjectRelevanceCountScore),
                Row(SectionWork, "Work Environment Fit", nameof(m.WorkEnvironmentFit), nameof(m.WorkEnvironmentFitScore), m.WorkEnvironmentFit, m.WorkEnvironmentFitScore),
                Row(SectionWork, "Career Graph", nameof(m.CareerGraphCount), nameof(m.CareerGraphCountScore), m.CareerGraphCount, m.CareerGraphCountScore),
                Row(SectionWork, "Relevant Industries", nameof(m.RelevantIndustries), nameof(m.RelevantIndustriesScore), m.RelevantIndustries, m.RelevantIndustriesScore),
                Row(SectionWork, "Must-Haves in Current Work", nameof(m.MustHavesCurrentWork), nameof(m.MustHavesCurrentWorkScore), m.MustHavesCurrentWork, m.MustHavesCurrentWorkScore),
                Row(SectionWork, "Relevant Tools", nameof(m.RelevantTools), nameof(m.RelevantToolsScore), m.RelevantTools, m.RelevantToolsScore),
                Row(SectionWork, "Key Performance Indicators", nameof(m.KPIs), nameof(m.KPIsScore), m.KPIs, m.KPIsScore),
                Row(SectionWork, "Key Responsibility Areas", nameof(m.KRAs), nameof(m.KRAsScore), m.KRAs, m.KRAsScore),
                Row(SectionWork, "Current Company Credentials Match", nameof(m.Gaps), nameof(m.GapsScore), m.Gaps, m.GapsScore),
                Row(SectionWork, "Stability Indicators", nameof(m.StabilityIndicators), nameof(m.StabilityIndicatorsScore), m.StabilityIndicators, m.StabilityIndicatorsScore),

                Row(SectionEducation, "Target Education Institution", nameof(m.TargetEducationInstitution), nameof(m.TargetEducationInstitutionScore), m.TargetEducationInstitution, m.TargetEducationInstitutionScore),
                Row(SectionEducation, "Highest Education Qualification", nameof(m.HighestEducationQualification), nameof(m.HighestEducationQualificationScore), m.HighestEducationQualification, m.HighestEducationQualificationScore),
                Row(SectionEducation, "Achievement in Reputed Public Exam", nameof(m.EducationType), nameof(m.EducationTypeScore), m.EducationType, m.EducationTypeScore),
                Row(SectionEducation, "Education Specialization", nameof(m.EducationSpecialization), nameof(m.EducationSpecializationScore), m.EducationSpecialization, m.EducationSpecializationScore),
                Row(SectionEducation, "Additional Academic Qualifications", nameof(m.EducationGaps), nameof(m.EducationGapsScore), m.EducationGaps, m.EducationGapsScore),
                Row(SectionEducation, "Industry Certifications", nameof(m.EducationCertification), nameof(m.EducationCertificateScore), m.EducationCertification, m.EducationCertificateScore)
            };
        }

        private static ScreeningCriterionRow Row(string section, string label, string textField, string scoreField,
            string text, int score, bool required = false)
        {
            return new ScreeningCriterionRow
            {
                Section = section,
                Label = label,
                TextField = textField,
                ScoreField = scoreField,
                Text = text,
                Score = score,
                IsRequired = required,
                MaxLength = ColumnWidth(textField)
            };
        }

        /// <summary>Widths of the IRFResumeScreeningCriteria text columns; keep in step with the table script.</summary>
        private static int ColumnWidth(string textField)
        {
            return textField == nameof(ResumeScreeningCriteriaVM.Title) ? 300 : 2000;
        }

        /// <summary>Labels of criteria whose text is wider than its column, for a save to refuse rather than truncate.</summary>
        public static List<string> OverlongCriteria(ResumeScreeningCriteriaVM m)
        {
            return BuildCriteriaRows(m)
                .Where(r => r.Text != null && r.Text.Length > r.MaxLength)
                .Select(r => $"{r.Label} (max {r.MaxLength} characters)")
                .ToList();
        }

        /// <summary>
        /// Cuts generated text to the column widths, for the automatic save where nobody is there
        /// to shorten it. Explicit, so it happens here and visibly rather than as a silent
        /// truncation at parameter binding. The widths are generous; this is a backstop.
        /// </summary>
        public static void ClampToColumnWidths(ResumeScreeningCriteriaVM m)
        {
            foreach (var row in BuildCriteriaRows(m).Where(r => r.Text != null && r.Text.Length > r.MaxLength))
            {
                typeof(ResumeScreeningCriteriaVM).GetProperty(row.TextField)?.SetValue(m, row.Text.Substring(0, row.MaxLength));
            }
        }

        /// <summary>
        /// Copies the criteria the review screen owns onto the stored record, so a post that
        /// carries only the rubric cannot blank out the IRF context.
        /// </summary>
        public static void OverlayEditedCriteria(ResumeScreeningCriteriaVM from, ResumeScreeningCriteriaVM onto)
        {
            onto.Title = from.Title;
            onto.PrimarySkill = from.PrimarySkill;
            onto.SecondarySkill = from.SecondarySkill;
            onto.WorkExperience = from.WorkExperience;
            onto.SeniorityLevel = from.SeniorityLevel;
            onto.JDTitleMaxScore = from.JDTitleMaxScore;
            onto.PrimarySkillsMaxScore = from.PrimarySkillsMaxScore;
            onto.SecondarySkillsMaxScore = from.SecondarySkillsMaxScore;
            onto.WorkExperienceMaxScore = from.WorkExperienceMaxScore;
            onto.SeniorityLevelScore = from.SeniorityLevelScore;

            onto.CompanyPercentile = from.CompanyPercentile;
            onto.CompanyGenuinity = from.CompanyGenuinity;
            onto.EmploymentType = from.EmploymentType;
            onto.VerifiedImpactSkillCredibility = from.VerifiedImpactSkillCredibility;
            onto.JobFunction = from.JobFunction;
            onto.CurrentProjectRelevanceCount = from.CurrentProjectRelevanceCount;
            onto.WorkEnvironmentFit = from.WorkEnvironmentFit;
            onto.CareerGraphCount = from.CareerGraphCount;
            onto.RelevantIndustries = from.RelevantIndustries;
            onto.MustHavesCurrentWork = from.MustHavesCurrentWork;
            onto.RelevantTools = from.RelevantTools;
            onto.KPIs = from.KPIs;
            onto.KRAs = from.KRAs;
            onto.Gaps = from.Gaps;
            onto.StabilityIndicators = from.StabilityIndicators;
            onto.CompanyPercentileScore = from.CompanyPercentileScore;
            onto.CompanyGenuinityScore = from.CompanyGenuinityScore;
            onto.EmploymentTypeScore = from.EmploymentTypeScore;
            onto.VerifiedImpactSkillCredibilityScore = from.VerifiedImpactSkillCredibilityScore;
            onto.JobFunctionScore = from.JobFunctionScore;
            onto.CurrentProjectRelevanceCountScore = from.CurrentProjectRelevanceCountScore;
            onto.WorkEnvironmentFitScore = from.WorkEnvironmentFitScore;
            onto.CareerGraphCountScore = from.CareerGraphCountScore;
            onto.RelevantIndustriesScore = from.RelevantIndustriesScore;
            onto.MustHavesCurrentWorkScore = from.MustHavesCurrentWorkScore;
            onto.RelevantToolsScore = from.RelevantToolsScore;
            onto.KPIsScore = from.KPIsScore;
            onto.KRAsScore = from.KRAsScore;
            onto.GapsScore = from.GapsScore;
            onto.StabilityIndicatorsScore = from.StabilityIndicatorsScore;

            onto.TargetEducationInstitution = from.TargetEducationInstitution;
            onto.HighestEducationQualification = from.HighestEducationQualification;
            onto.EducationType = from.EducationType;
            onto.EducationSpecialization = from.EducationSpecialization;
            onto.EducationGaps = from.EducationGaps;
            onto.EducationCertification = from.EducationCertification;
            onto.TargetEducationInstitutionScore = from.TargetEducationInstitutionScore;
            onto.HighestEducationQualificationScore = from.HighestEducationQualificationScore;
            onto.EducationTypeScore = from.EducationTypeScore;
            onto.EducationSpecializationScore = from.EducationSpecializationScore;
            onto.EducationGapsScore = from.EducationGapsScore;
            onto.EducationCertificateScore = from.EducationCertificateScore;
        }

        // ----------------------------------------------------------------- result

        /// <summary>Strips a markdown fence and deserialises; null when the text is not the expected JSON.</summary>
        public static T ParseModelJson<T>(string raw) where T : class
        {
            var cleaned = StripJsonFence(raw);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(cleaned);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string StripJsonFence(string raw)
        {
            return raw?.Replace("```json", "").Replace("```", "").Trim();
        }

        /// <summary>The fitment percentage as a whole number 0-100. The model returns it as text, sometimes "78%" or "78.5".</summary>
        public static int ParsePercentage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var cleaned = value.Replace("%", "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            {
                return (int)Math.Round(Math.Min(100m, Math.Max(0m, d)), MidpointRounding.AwayFromZero);
            }
            return 0;
        }

        /// <summary>One scored criterion in the screener's answer, labelled for display.</summary>
        public class ScoredCriterion
        {
            public string Section { get; set; }
            public string Label { get; set; }
            public string Icon { get; set; }
            public SectionsInfo Info { get; set; }
        }

        /// <summary>
        /// Every criterion the screener answered, in report order. The single place that names the
        /// response keys, used by both the results page and the AI interview report.
        ///
        /// Career Graph reads careerGraphCount. MCQ's results page rendered that card from
        /// currentProjectRelevanceCount, so it showed Current Project Relevance twice and the
        /// career-graph score never.
        /// </summary>
        public static List<ScoredCriterion> ScoredCriteria(AdvancedScreeningResponse r)
        {
            var list = new List<ScoredCriterion>();
            if (r == null)
            {
                return list;
            }

            var s = r.skillsSection;
            if (s != null)
            {
                Add(list, SectionSkills, "JD Title Match", "fa-bullseye", s.JD_Title_Match);
                Add(list, SectionSkills, "Primary Skills", "fa-star", s.primarySkillsMatch);
                Add(list, SectionSkills, "Secondary Skills", "fa-star-half-alt", s.secondarySkillsMatch);
                Add(list, SectionSkills, "Experience Level", "fa-layer-group", s.experienceLevelMatch);
                Add(list, SectionSkills, "Seniority Level", "fa-sitemap", s.seniorityLevelMatch);
            }

            var w = r.workExperienceSection;
            if (w != null)
            {
                Add(list, SectionWork, "Target Companies", "fa-chart-bar", w.targetCompanies);
                Add(list, SectionWork, "Verified Impact & Skill Credibility", "fa-shield-alt", w.verifiedImpactSkillCredibility);
                Add(list, SectionWork, "Current Project Relevance", "fa-project-diagram", w.currentProjectRelevanceCount);
                Add(list, SectionWork, "Career Graph", "fa-chart-line", w.careerGraphCount);
                Add(list, SectionWork, "Must-Haves in Current Work", "fa-check-double", w.mustHavesCurrentWork);
                Add(list, SectionWork, "Work Environment Fit", "fa-users", w.workEnvironmentFit);
                Add(list, SectionWork, "Relevant Industries", "fa-industry", w.relevantIndustries);
                Add(list, SectionWork, "Relevant Tools", "fa-tools", w.relevantTools);
                Add(list, SectionWork, "KPIs", "fa-tachometer-alt", w.KPIs);
                Add(list, SectionWork, "KRAs", "fa-tasks", w.KRAs);
                Add(list, SectionWork, "Stability Indicators", "fa-balance-scale", w.stabilityIndicators);
                Add(list, SectionWork, "Current Company Credentials Match", "fa-id-badge", w.currentCompanyCredentialsMatch);
                Add(list, SectionWork, "Job Functions", "fa-briefcase", w.jobFunctions);
            }

            var e = r.educationSection;
            if (e != null)
            {
                Add(list, SectionEducation, "Target Education Institution", "fa-university", e.targetGraduationInstitutions);
                Add(list, SectionEducation, "Highest Education Qualification", "fa-graduation-cap", e.highestEducationQualification);
                // The screener has answered this one under either key.
                Add(list, SectionEducation, "Achievement in Reputed Public Exam", "fa-award", e.educationType ?? e.achievementReputedPublicExam);
                Add(list, SectionEducation, "Education Specialization", "fa-book", e.educationSpecialization);
                Add(list, SectionEducation, "Additional Academic Qualifications", "fa-plus-square", e.additionalAcademicQualifications);
                Add(list, SectionEducation, "Industry Certifications", "fa-certificate", e.EducationCertification ?? e.industryCertifications);
            }

            return list;
        }

        private static void Add(List<ScoredCriterion> list, string section, string label, string icon, SectionsInfo info)
        {
            if (info != null)
            {
                list.Add(new ScoredCriterion { Section = section, Label = label, Icon = icon, Info = info });
            }
        }

        /// <summary>
        /// The advanced result in the V2 shape the AI interview report renders, so the report shows
        /// the latest screening rather than an older V2 one. The fitment percentage is the total
        /// out of 100; each scored criterion becomes one evaluation row with its own reason.
        /// </summary>
        public static CandidateResumeScreeningVM ToResumeEvaluation(AdvancedScreeningResponse r)
        {
            if (r == null)
            {
                return null;
            }

            return new CandidateResumeScreeningVM
            {
                TotalScore = ParsePercentage(r.overallfitmentpercentage),
                MaxScore = 100,
                Verdict = NormalizeVerdict(r.overallverdict),
                FitmentSummary = r.overallFitmentAnalysis,
                EvaluationCriteriaScores = ScoredCriteria(r)
                    .Select(c => new EvaluationCriteriaScore
                    {
                        CriterionId = c.Label,
                        TechnicalSkill = c.Section + " - " + c.Label,
                        Score = ParseInt(c.Info.actualscore),
                        MaxScore = ParseInt(c.Info.maxscore),
                        Reason = c.Info.reasonOrObservation
                    })
                    .ToList()
            };
        }

        private static int ParseInt(string value)
        {
            return decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                ? (int)Math.Round(d, MidpointRounding.AwayFromZero)
                : 0;
        }
    }
}
