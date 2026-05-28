using System;

namespace Risebird.Entities.ViewModel
{
    public class VoiceInstructionBriefVm
    {
        public string AudioUrl { get; set; }
        public string Transcript { get; set; }
        public DateTime? TranscriptUpdatedOn { get; set; }
        public string TranscriptUpdatedBy { get; set; }
    }
}
