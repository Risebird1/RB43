using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Conversation
    {
        public int id { get; set; }
        public int Senderid { get; set; }
        public int Reciverid { get; set; }
        public string Sender { get; set; }
        public string SenderName { get; set; }
        public string Reciver { get; set; }

        public string message { get; set; }
        public string Status { get; set; }
        public DateTime Created_at { get; set; }
        public string MessageInfo { get; set; }
        public int IsOnline { get; set; }
        public int self { get; set; }
        public string WhoSend { get; set; }
    }
    public class GroupDetails
    {
        public int id { get; set; }
        public string GroupName { get; set; }
        public string Emailid { get; set; }
    }
}
