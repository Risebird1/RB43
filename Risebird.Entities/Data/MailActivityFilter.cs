namespace Risebird.Entities.Data
{
    public class MailActivityFilter : Filters
    {
        public string[] Activity { get; set; }
        public string[] SentBy { get; set; }
        public string[] Recipient { get; set; }
        public string RecipientType { get; set; }

    }
}
