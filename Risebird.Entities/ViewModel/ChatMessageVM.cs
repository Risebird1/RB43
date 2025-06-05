using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class ChatMessage
    {
        public string Role { get; set; }      // Identifies if message is from 'user' or 'assistant'
        public string Content { get; set; }    // The actual message text
        public DateTime Timestamp { get; set; } // When the message was sent
    }
}
