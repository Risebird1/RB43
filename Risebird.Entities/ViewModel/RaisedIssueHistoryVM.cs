using System;

namespace Risebird.Entities.ViewModel
{
    public class RaisedIssueHistoryVM
    {
        public int id { get; set; }
        public int RaisedIssueId { get; set; }
        /// <summary>Created | Closed | Reopened</summary>
        public string Action { get; set; }
        public string Comment { get; set; }
        public string ActionBy { get; set; }
        public string ActionByName { get; set; }
        public DateTime ActionOn { get; set; }
    }
}
