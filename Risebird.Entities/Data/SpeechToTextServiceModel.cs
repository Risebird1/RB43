using Microsoft.WindowsAzure.Storage.Table;

namespace Risebird.Entities.Data
{
    public class SpeechToTextServiceModel : TableEntity
    {
        public int InterviewId { get; set; }


        public string AudioFileUrl { get; set; }


        public string KeyWords { get; set; }
        public string ConvertedText { get; set; }
    }
}
