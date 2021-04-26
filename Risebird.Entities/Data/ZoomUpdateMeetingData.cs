namespace Risebird.Entities.Data
{
    public class ZoomUpdateMeetingData
    {
        public string start_time { get; set; }
        public string timezone { get; set; }
        public string topic { get; set; }
        public int type { get; set; }

        public ZoomUpdateSettings settings { get; set; }
    }
}
