using Risebird.Entities.Storage;

namespace Risebird.Entities.Data
{
    public class SpeechToTextServiceModel : RbTableEntity
    {
        public int InterviewId { get; set; }


        public string AudioFileUrl { get; set; }


        public string KeyWords { get; set; }
        public string ConvertedText { get; set; }
    }
}
