using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClientFeedbackCommentVM
    {
        public ClientFeedbackCommentVM()
        {
            ClientCommentsList = new List<ClientCommentsForReport>();
        }
        public ClientCommentsForReport ClientComments { get; set; }
        public IEnumerable<ClientCommentsForReport> ClientCommentsList { get; set; }
        public string Profile { get; set; }

    }
}
