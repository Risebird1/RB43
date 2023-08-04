namespace Risebird.Entities.Data
{
    public class MeetingTranscriptModel
    {
        public int Total { get; set; }
        public string ConvertedText { get; set; }
        public string KeywordsFound { get; set; } // stores comma separated keywords found in the transcript
    }
}
