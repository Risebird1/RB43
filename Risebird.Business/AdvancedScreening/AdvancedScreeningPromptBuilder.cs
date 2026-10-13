using Newtonsoft.Json;
using Risebird.Entities.ViewModel.AdvancedScreening;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// Builds the input strings for the Advanced Resume Screening prompts. Each piece of data is
    /// wrapped in an XML-style tag the stored prompt knows how far to trust.
    ///
    /// Every wrapped value has the prompt's own tags removed first, opening and closing. Without
    /// that, a resume containing "&lt;/resume&gt;" followed by instructions would close its own data
    /// block and have the rest read as if it came from us.
    /// </summary>
    public static class AdvancedScreeningPromptBuilder
    {
        private static readonly string[] Tags =
        {
            "job_description", "ideal_candidate_profile", "recruiter_instructions", "criteria_catalog",
            "rubric", "verified_facts", "resume"
        };

        private static readonly Regex TagPattern = new Regex(
            @"<\s*/?\s*(" + string.Join("|", Tags) + @")\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Removes every opening or closing prompt tag from data that is about to be wrapped.</summary>
        public static string StripTags(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : TagPattern.Replace(value, string.Empty);
        }

        /// <summary>The catalog the generator works from: the 27 criteria with their meaning and default weight.</summary>
        public static string CriteriaCatalogJson()
        {
            return JsonConvert.SerializeObject(RubricCriteria.All.Select(c => new
            {
                key = c.Key,
                section = c.Section,
                label = c.Label,
                meaning = c.Meaning,
                defaultWeight = c.DefaultWeight
            }));
        }

        /// <param name="instructions">Pass null when the instructions are not applied to rubric generation.</param>
        public static string BuildRubricInput(string jdText, string idealCandidateProfile, string instructions)
        {
            var sb = new StringBuilder();
            sb.Append("<job_description>").Append(StripTags(jdText)).AppendLine("</job_description>");
            sb.Append("<ideal_candidate_profile>").Append(StripTags(idealCandidateProfile)).AppendLine("</ideal_candidate_profile>");
            sb.Append("<recruiter_instructions>").Append(StripTags(instructions)).AppendLine("</recruiter_instructions>");
            sb.Append("<criteria_catalog>").Append(CriteriaCatalogJson()).AppendLine("</criteria_catalog>");
            sb.Append("Return the rubric as JSON.");
            return sb.ToString();
        }

        /// <param name="instructions">Pass null when the instructions are not applied to scoring.</param>
        public static string BuildScoringInput(string rubricJson, string instructions, string factsJson, string resumeText)
        {
            var sb = new StringBuilder();
            sb.Append("<rubric>").Append(StripTags(rubricJson)).AppendLine("</rubric>");
            sb.Append("<recruiter_instructions>").Append(StripTags(instructions)).AppendLine("</recruiter_instructions>");
            sb.Append("<verified_facts>").Append(StripTags(factsJson)).AppendLine("</verified_facts>");
            sb.Append("<resume>").Append(StripTags(resumeText)).AppendLine("</resume>");
            sb.Append("Return the evaluation as JSON.");
            return sb.ToString();
        }

        public static string BuildWorkHistoryInput(string resumeText)
        {
            return "<resume>" + StripTags(resumeText) + "</resume>\nReturn the work history as JSON.";
        }

        /// <summary>
        /// The rubric as the scorer sees it: only what scoring needs, criteria at weight 0 left out
        /// (the scorer skips them anyway, and leaving them out keeps the input short).
        /// </summary>
        public static string RubricJsonForScoring(ScreeningRubric rubric)
        {
            return JsonConvert.SerializeObject(new
            {
                jdTitle = rubric.JdTitle,
                criteria = rubric.Criteria.Where(c => c.Weight > 0).Select(c => new
                {
                    key = c.Key,
                    section = c.Section,
                    label = c.Label,
                    expected = c.Expected,
                    weight = c.Weight
                }),
                dealbreakers = rubric.Dealbreakers ?? new System.Collections.Generic.List<string>()
            });
        }
    }
}
