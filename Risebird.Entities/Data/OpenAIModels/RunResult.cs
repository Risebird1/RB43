using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.OpenAIModels
{
    public class RunResult
    {
        public string Object { get; set; }
        public List<Datum> Data { get; set; }
        public string FirstId { get; set; }
        public string LastId { get; set; }
        public bool HasMore { get; set; }
    }

    public class Datum
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long CreatedAt { get; set; }
        public string AssistantId { get; set; }
        public string ThreadId { get; set; }
        public string RunId { get; set; }
        public string Role { get; set; }
        public List<Content> Content { get; set; }
        public List<object> Attachments { get; set; }
        public Metadata Metadata { get; set; }
    }

    public class Content
    {
        public string Type { get; set; }
        public Text Text { get; set; }
    }

    public class Text
    {
        public string Value { get; set; }
        public List<object> Annotations { get; set; }
    }

    public class Metadata
    {
    }
}
