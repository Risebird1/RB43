using System.Collections.Generic;

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
