namespace Risebird.Entities.Data
{
    public class PoorQOSZoomData
    {
        //       [id][int] IDENTITY(1,1) NOT NULL,

        //   [ZoomMeetingId] [varchar] (200) NOT NULL,

        //   [participantEmail] [varchar] (100) NULL,
        //[qosType][varchar] (50) NULL,
        //[jitter][varchar] (15) NULL,
        //[quality][varchar] (15) NULL,
        //[quality_score][varchar] (15) NULL,
        //[latency][varchar] (15) NULL,
        //[avg_loss][varchar] (15) NULL,
        //[max_loss][varchar] (15) NULL,

        public int id { get; set; }
        public string ZoomMeetingId { get; set; }
        public string participantEmail { get; set; }
        public string qosType { get; set; }
        //public string jitter { get; set; }
        //public string quality { get; set; }
        //public string quality_score { get; set; }
        //public string latency { get; set; }
        //public string avg_loss { get; set; }
        //public string max_loss { get; set; }

        public string avg_jitter { get; set; }
        public string max_jitter { get; set; }
        public string min_jitter { get; set; }
        public string quality_score { get; set; }
        public string quality { get; set; }
        public string max_latency { get; set; }
        public string avg_loss { get; set; }
        public string max_loss { get; set; }
        public string user_name { get; set; }
    }
}
