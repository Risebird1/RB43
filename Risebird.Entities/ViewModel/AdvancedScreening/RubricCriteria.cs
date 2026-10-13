using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel.AdvancedScreening
{
    /// <summary>One of the 27 fixed rubric criteria, as defined for every IRF.</summary>
    public sealed class RubricCriterionDefinition
    {
        public RubricCriterionDefinition(string key, string section, string label, string meaning, int defaultWeight)
        {
            Key = key;
            Section = section;
            Label = label;
            Meaning = meaning;
            DefaultWeight = defaultWeight;
        }

        public string Key { get; }
        public string Section { get; }
        public string Label { get; }

        /// <summary>What the criterion checks. Sent to the generator and shown as help text.</summary>
        public string Meaning { get; }

        public int DefaultWeight { get; }
    }

    /// <summary>
    /// The fixed catalog of 27 criteria. The UI, the rubric validator and both prompts read from
    /// this one list, so the keys cannot drift between them. Fixed keys also keep results
    /// comparable across IRFs.
    /// </summary>
    public static class RubricCriteria
    {
        public const string Skills = "Skills";
        public const string Work = "Work";
        public const string Education = "Education";

        /// <summary>Weights across all 27 criteria must add up to exactly this.</summary>
        public const int TotalWeight = 300;

        /// <summary>The expected text of a criterion switched off (weight 0). Never "unknown".</summary>
        public const string NotUsedText = "Not used for this role";

        public static readonly IReadOnlyList<string> Sections = new[] { Skills, Work, Education };

        public static readonly IReadOnlyList<RubricCriterionDefinition> All = new List<RubricCriterionDefinition>
        {
            new RubricCriterionDefinition("jdTitle", Skills, "JD Title", "Role specialization, e.g. Vue.js Front-end Developer", 10),
            new RubricCriterionDefinition("primarySkills", Skills, "Primary Skills", "2-5 must-have skills. These are elimination criteria", 50),
            new RubricCriterionDefinition("secondarySkills", Skills, "Secondary Skills", "3-6 nice-to-have skills", 20),
            new RubricCriterionDefinition("experienceYears", Skills, "Experience in Years", "Relevant years, as a range", 20),
            new RubricCriterionDefinition("seniorityLevel", Skills, "Seniority Level", "IC, lead or manager; scope of ownership", 10),

            new RubricCriterionDefinition("targetCompanies", Work, "Target Company Percentile", "Tier or type of past employers wanted", 10),
            new RubricCriterionDefinition("currentCompanyPercentile", Work, "Current Company Percentile", "Tier of the current employer", 5),
            new RubricCriterionDefinition("companyGenuineness", Work, "Company Genuineness", "The employer is real and verifiable (uses verification facts)", 10),
            new RubricCriterionDefinition("employmentType", Work, "Employment Type", "Full-time, contract or freelance, as required", 5),
            new RubricCriterionDefinition("verifiedImpact", Work, "Verified Impact & Skill Credibility", "Quantified outcomes (%, $, x, baselines) with primary skills shown in context", 15),
            new RubricCriterionDefinition("jobFunctions", Work, "Job Functions", "Overlap of functions, e.g. front-end, API integration", 10),
            new RubricCriterionDefinition("currentProjectRelevance", Work, "Current Project Relevance", "The most recent dated role or project applies the primary skills", 15),
            new RubricCriterionDefinition("workEnvironmentFit", Work, "Work Environment Fit", "Product vs services, startup vs enterprise, client-facing vs in-house", 5),
            new RubricCriterionDefinition("careerGraph", Work, "Career Graph", "Progression in titles, ownership and scope", 10),
            new RubricCriterionDefinition("relevantIndustries", Work, "Relevant Industries", "Domain match, e.g. FinTech or manufacturing IoT", 10),
            new RubricCriterionDefinition("mustHavesInCurrentWork", Work, "Must-Haves in Current Work", "Mandatory skills applied (not just listed) in the latest role", 15),
            new RubricCriterionDefinition("relevantTools", Work, "Relevant Tools", "Tools and platforms", 10),
            new RubricCriterionDefinition("kpis", Work, "KPIs", "Measurable targets owned are realistic for the role and level", 5),
            new RubricCriterionDefinition("kras", Work, "KRAs", "Key result areas match the role", 5),
            new RubricCriterionDefinition("companyCredentialsMatch", Work, "Current Company Credentials Match", "Product or service, size, geography", 5),
            new RubricCriterionDefinition("stability", Work, "Stability Indicators", "Tenure pattern (uses computed stability facts)", 10),

            new RubricCriterionDefinition("targetInstitutions", Education, "Target Education Institution", "Preferred institutions, if any", 10),
            new RubricCriterionDefinition("highestQualification", Education, "Highest Education Qualification", "Degree level", 10),
            new RubricCriterionDefinition("publicExamAchievement", Education, "Achievement in Reputed Public Exam", "e.g. JEE, GATE, CAT ranks", 5),
            new RubricCriterionDefinition("specialization", Education, "Education Specialization", "Field of study", 10),
            new RubricCriterionDefinition("additionalAcademics", Education, "Additional Academic Qualifications", "Courses, publications, patents", 5),
            new RubricCriterionDefinition("certifications", Education, "Industry Certifications", "Certifications that matter for the role", 5),
        };

        private static readonly Dictionary<string, RubricCriterionDefinition> ByKey =
            All.ToDictionary(c => c.Key, c => c, System.StringComparer.OrdinalIgnoreCase);

        public static RubricCriterionDefinition Find(string key)
        {
            return key != null && ByKey.TryGetValue(key, out var d) ? d : null;
        }

        /// <summary>A rubric of the 27 criteria at their default weights, with an empty expected text.</summary>
        public static ScreeningRubric DefaultRubric(string jdTitle = null)
        {
            return new ScreeningRubric
            {
                JdTitle = jdTitle,
                Criteria = All.Select(d => new RubricCriterion
                {
                    Key = d.Key,
                    Section = d.Section,
                    Label = d.Label,
                    Expected = "",
                    Weight = d.DefaultWeight
                }).ToList(),
                Dealbreakers = new List<string>(),
                TotalWeight = TotalWeight
            };
        }

        /// <summary>The section heading shown in the UI.</summary>
        public static string SectionTitle(string section)
        {
            return section == Work ? "Work Experience" : section;
        }
    }
}
