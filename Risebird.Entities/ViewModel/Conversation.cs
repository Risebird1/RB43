using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Conversation
    {
        public Conversation()
        {
            Status = messageStatus.Sent;
        }
        public enum messageStatus
        {
            Sent,
            Delivered
        }
        public int id { get; set; }
        public int Senderid { get; set; }
        public int Reciverid { get; set; }
        public string Sender { get; set; }
        public string Reciver { get; set; }

        public string message { get; set; }
        public messageStatus Status { get; set; }
        public DateTime Created_at { get; set; }
        public string MessageInfo { get; set; }
        public int IsOnline { get; set; }
    }
}
