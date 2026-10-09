using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// The schema and <see cref="AIInterviewEvaluation"/> describe the same payload from two sides:
    /// one constrains what the model may emit, the other reads it. If they drift, the model is held
    /// to a shape the parser does not accept - and the failure shows up as an empty report rather
    /// than an exception, which is the exact silence this schema exists to remove.
    /// </summary>
    public class OpenAIResponseSchemasTests
    {
        private static JObject Schema() =>
            JObject.Parse(JsonConvert.SerializeObject(OpenAIResponseSchemas.AIInterviewEvaluation()));

        private static IEnumerable<string> JsonNamesOf<T>() =>
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName)
                .Where(n => !string.IsNullOrEmpty(n));

        private static JObject PropertiesAt(JObject schema, params string[] path)
        {
            var node = schema;
            foreach (var step in path)
            {
                node = (JObject)node["properties"][step];
                if (node["type"]?.ToString() == "array") node = (JObject)node["items"];
            }
            return (JObject)node["properties"];
        }

        [Fact]
        public void Covers_every_field_the_evaluation_model_reads()
        {
            var declared = PropertiesAt(Schema()).Properties().Select(p => p.Name).ToList();
            foreach (var name in JsonNamesOf<AIInterviewEvaluation>())
                Assert.Contains(name, declared);
        }

        [Fact]
        public void Covers_every_field_a_rubric_entry_reads()
        {
            var declared = PropertiesAt(Schema(), "rubric_score_summary").Properties().Select(p => p.Name).ToList();
            foreach (var name in JsonNamesOf<RubricScoreSummary>())
                Assert.Contains(name, declared);
        }

        [Fact]
        public void Covers_every_field_a_question_rating_reads()
        {
            var declared = PropertiesAt(Schema(), "question_ratings").Properties().Select(p => p.Name).ToList();
            foreach (var name in JsonNamesOf<QuestionRating>())
                Assert.Contains(name, declared);
        }

        /// <summary>
        /// Strict mode rejects a schema where any object omits additionalProperties:false or leaves a
        /// declared property out of required. The API returns a 400 for the whole call, so an
        /// interview that reached the report stage would produce nothing at all.
        /// </summary>
        [Fact]
        public void Every_object_satisfies_strict_mode()
        {
            foreach (var obj in Schema().Descendants().OfType<JObject>()
                         .Where(o => o["type"]?.ToString() == "object"))
            {
                Assert.False(obj["additionalProperties"]?.Value<bool>() ?? true);

                var required = obj["required"].Values<string>().ToList();
                foreach (var declared in ((JObject)obj["properties"]).Properties().Select(p => p.Name))
                    Assert.Contains(declared, required);
            }
        }

        [Fact]
        public void Constrains_the_verdict_to_the_four_canonical_values()
        {
            var verdicts = PropertiesAt(Schema())["final_verdict"]["enum"].Values<string>().ToList();

            Assert.Equal(4, verdicts.Count);
            foreach (var verdict in verdicts)
                Assert.Equal(verdict, AIInterviewVerdict.Normalize(verdict));
        }

        /// <summary>
        /// The field name really does contain a space. It is the name the parser expects, so a
        /// well-meant correction to an underscore would silently drop the reasoning from every report.
        /// </summary>
        [Fact]
        public void Keeps_the_communication_reasoning_field_name_verbatim()
        {
            Assert.NotNull(PropertiesAt(Schema())["overall_communication_score reasoning"]);
            Assert.Null(PropertiesAt(Schema())["overall_communication_score_reasoning"]);
        }
    }
}
