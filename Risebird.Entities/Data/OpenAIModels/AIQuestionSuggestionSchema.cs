using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Risebird.Entities.Data.OpenAIModels
{
    public class AIQuestionSuggestionSchema
    {
        public List<AISuggestedQuestion> Questions { get; set; }
    }

    public class AIIdealAnswerSchema
    {
        public List<AISuggestedQuestion> Answers { get; set; }
    }

    public class AISuggestedQuestion
    {
        [JsonPropertyName("question")]
        public string Question { get; set; }
        [JsonPropertyName("skillName")]
        public string SkillName { get; set; }
        [JsonPropertyName("idealAnswer")]
        public string IdealAnswer { get; set; }
    }

    public class AIQuestionAnswerRequest
    {
        [JsonPropertyName("irfId")]
        public int IrfId { get; set; }
        [JsonPropertyName("questions")]
        public List<AISuggestedQuestion> Questions { get; set; }
    }
}
