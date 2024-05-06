namespace Risebird.Entities.Data
{
    public class QOSParticipantData
    {
        //public string user_id { get; set; }
        //public string participant_user_id { get; set; }
        public string participant_id { get; set; }
        public string user_name { get; set; }
        public string id { get; set; }
        public string email { get; set; }
        public QOSType[] qos { get; set; }
    }
}
