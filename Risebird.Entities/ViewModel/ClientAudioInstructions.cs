using System;

namespace Risebird.Entities.ViewModel
{
    public class ClientAudioInstructions
    {
        public string AudioUrl { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public int InterviewId { get; set; }
    }
}
