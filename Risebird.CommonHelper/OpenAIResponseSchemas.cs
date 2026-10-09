using System.Collections.Generic;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// JSON schemas handed to the Responses API as <c>text.format.type = "json_schema"</c>.
    ///
    /// Without one, the API is told only "return valid JSON" (<c>json_object</c>), so a prompt that
    /// drifts - renaming a field, emitting percentages instead of 1-5 ratings, inventing a fifth
    /// verdict - still returns a 200 with well-formed JSON that nothing downstream can read. The
    /// report then renders empty with no error anywhere. A schema moves that failure from silent to
    /// impossible: the model is constrained to the shape at generation time.
    ///
    /// Strict mode has two rules that drive the shape below: every object must carry
    /// <c>additionalProperties: false</c>, and every property must be listed in <c>required</c>.
    /// There is no such thing as an optional field - a field that may be empty is required but may
    /// hold an empty string or array.
    /// </summary>
    public static class OpenAIResponseSchemas
    {
        /// <summary>The four canonical verdicts. Constraining these here is what stops the legacy
        /// "Recommended" / "select" / "borderline" labels reaching the normalizer at all.</summary>
        private static readonly string[] Verdicts =
        {
            "Strongly Selected", "Marginally Selected", "Marginally Rejected", "Strongly Rejected"
        };

        private static Dictionary<string, object> Obj(string[] required, Dictionary<string, object> properties) =>
            new Dictionary<string, object>
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["required"] = required,
                ["properties"] = properties
            };

        private static Dictionary<string, object> Str() =>
            new Dictionary<string, object> { ["type"] = "string" };

        private static Dictionary<string, object> Num() =>
            new Dictionary<string, object> { ["type"] = "number" };

        private static Dictionary<string, object> Arr(object items) =>
            new Dictionary<string, object> { ["type"] = "array", ["items"] = items };

        /// <summary>
        /// The shape <see cref="Risebird.Entities.ViewModel.AIInterviewEvaluation"/> parses and
        /// <c>AIScreeningReportMapper</c> scores from.
        ///
        /// Two details are deliberate and look like mistakes:
        /// "overall_communication_score reasoning" really does contain a space rather than an
        /// underscore - it is the existing field name and correcting it would break the parser; and
        /// no section totals or overall percentage appear here, because those are computed server
        /// side from the 1-5 ratings. A model asked to total them produces a second, conflicting
        /// set of numbers.
        /// </summary>
        public static object AIInterviewEvaluation() => Obj(
            new[]
            {
                "question_ratings", "rubric_score_summary", "overall_technical_score", "final_verdict",
                "final_comment", "overall_communication_score", "overall_communication_score reasoning",
                "llm_detection"
            },
            new Dictionary<string, object>
            {
                ["question_ratings"] = Arr(Obj(
                    new[] { "question", "verdict", "score", "comment", "communication_score", "communication_comment" },
                    new Dictionary<string, object>
                    {
                        ["question"] = Str(),
                        ["verdict"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = Verdicts },
                        ["score"] = Num(),
                        ["comment"] = Str(),
                        ["communication_score"] = Num(),
                        ["communication_comment"] = Str()
                    })),

                // rubric_item is matched against TransTechSkill.SkillName to place the score in a
                // section, so it has to be the parameter name verbatim. The schema cannot enforce
                // that - only the prompt can - which is why it is the first rule in the evaluator
                // prompt draft.
                ["rubric_score_summary"] = Arr(Obj(
                    new[] { "rubric_item", "expected_level", "observed_level", "score", "max_score" },
                    new Dictionary<string, object>
                    {
                        ["rubric_item"] = Str(),
                        ["expected_level"] = Str(),
                        ["observed_level"] = Str(),
                        ["score"] = Num(),
                        ["max_score"] = Num()
                    })),

                ["overall_technical_score"] = Num(),
                ["final_verdict"] = new Dictionary<string, object> { ["type"] = "string", ["enum"] = Verdicts },
                ["final_comment"] = Str(),
                ["overall_communication_score"] = Num(),
                ["overall_communication_score reasoning"] = Str(),

                ["llm_detection"] = Obj(
                    new[] { "llm_response_suspected", "evidence_comment", "suspicious_instances" },
                    new Dictionary<string, object>
                    {
                        ["llm_response_suspected"] = new Dictionary<string, object> { ["type"] = "boolean" },
                        ["evidence_comment"] = Str(),
                        ["suspicious_instances"] = Arr(Str())
                    })
            });
    }
}
