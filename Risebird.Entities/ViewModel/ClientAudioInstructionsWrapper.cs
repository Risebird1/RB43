using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClientAudioInstructionsWrapper
    {
        public IEnumerable<ClientAudioInstructions> ClientAudioInstructionsList { get; set; }
        public string Profile { get; set; }
        public int InterviewId { get; set; }
        public string CreatedBy { get; set; }

    }
}
