using System;

namespace Risebird.Entities.Data
{
    public class SupportTicketHistory
    {
        public int      Id         { get; set; }
        public int      TicketId   { get; set; }
        public string   FromStatus { get; set; }
        public string   ToStatus   { get; set; }
        public string   ChangedBy  { get; set; }
        public DateTime ChangedAt  { get; set; }
        public string   EventType  { get; set; }
    }
}
