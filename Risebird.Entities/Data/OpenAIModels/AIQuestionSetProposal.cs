using Newtonsoft.Json;
using System.Collections.Generic;

namespace Risebird.Entities.Data.OpenAIModels
{
    /// <summary>
    /// What the set-creation stored prompt answers with. It is a suggestion and nothing more: the
    /// only part that is acted on is which question id lands in which group. Set names, question
    /// order, set totals and each question's rubric parameter are all decided server-side, and the
    /// Validation and DifficultyMix blocks are parsed and then ignored.
    /// </summary>
    public class AIQuestionSetProposal
    {
        [JsonProperty("sets")]
        public IList<AIQuestionSetProposalSet> Sets { get; set; }

        [JsonProperty("validation")]
        public AIQuestionSetProposalValidation Validation { get; set; }

        /// <summary>
        /// The model may answer with an error instead of a grouping. It is surfaced to the user and
        /// nothing is persisted.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }
    }

    public class AIQuestionSetProposalSet
    {
        [JsonProperty("setName")]
        public string SetName { get; set; }

        /// <summary>
        /// Question ids arrive as strings, matching what the Responses API is given.
        /// </summary>
        [JsonProperty("questionIds")]
        public IList<string> QuestionIds { get; set; }

        [JsonProperty("totalMinutes")]
        public int? TotalMinutes { get; set; }

        [JsonProperty("totalPoints")]
        public int? TotalPoints { get; set; }

        [JsonProperty("difficultyMix")]
        public IList<string> DifficultyMix { get; set; }
    }

    public class AIQuestionSetProposalValidation
    {
        [JsonProperty("onePerParameterPerSet")]
        public bool? OnePerParameterPerSet { get; set; }

        [JsonProperty("eachQuestionUsedOnce")]
        public bool? EachQuestionUsedOnce { get; set; }

        [JsonProperty("setCount")]
        public int? SetCount { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }
}
