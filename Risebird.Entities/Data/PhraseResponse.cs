using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class PhraseResponse
    {
        public string message { get; set; }
        public int count { get; set; }
        public List<Phrase> phrases { get; set; }
    }

    public class Phrase
    {
        public string text { get; set; }
    }
}
