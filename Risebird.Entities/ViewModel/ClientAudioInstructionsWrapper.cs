using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientAudioInstructionsWrapper
    {
        public IEnumerable<ClientAudioInstructions> ClientAudioInstructionsList { get; set; }
        public string Profile { get; set; }

    }
}
