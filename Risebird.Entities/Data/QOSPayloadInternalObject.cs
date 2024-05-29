namespace Risebird.Entities.Data
{
    public class QOSPayloadInternalObject
    {
        public string uuid { get; set; }
        public string id { get; set; }//this is the meeting Id
        public QOSParticipantData participant { get; set; }
    }
}
