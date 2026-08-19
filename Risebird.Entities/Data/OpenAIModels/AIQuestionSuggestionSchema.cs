using System.Collections.Generic;

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
        public string Question { get; set; }
        public string SkillName { get; set; }
        public string IdealAnswer { get; set; }
    }

    public class AIQuestionAnswerRequest
    {
        public int IrfId { get; set; }
        public List<AISuggestedQuestion> Questions { get; set; }
    }
}
