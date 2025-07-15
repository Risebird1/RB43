using System.Collections.Generic;
using System;

namespace Risebird.Entities.Data
{
    public static class OpenAIInterviewerAssistants
    {
        public static readonly string Assistant1 = "asst_feVto3eUoHe1s3qAcEIafhIC";
        public static readonly string Assistant2 = "asst_feVto3eUoHe1s3qAcEIafhIC";    //change this id to the new assistant that you will create

        private static readonly List<string> _allValues = new List<string> { Assistant1, Assistant2 };

        public static string GetRandomValue()
        {
            Random rand = new Random();
            int index = rand.Next(_allValues.Count);
            return _allValues[index];
        }
    }
}
