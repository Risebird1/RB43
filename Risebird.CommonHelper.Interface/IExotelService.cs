using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    /// <summary>
    /// Exotel Voice v1 REST API (HTTP Basic: ApiKey / ApiToken, scoped to the account SID and region subdomain).
    /// </summary>
    public interface IExotelService
    {
        /// <summary>
        /// Calls/connect: Exotel rings <paramref name="from"/> first (the agent's phone, or the agent's SIP user
        /// for the in-browser softphone), then dials <paramref name="to"/> and bridges, with the ExoPhone as caller ID.
        /// </summary>
        Task<ExotelConnectResult> ConnectCallAsync(string from, string to, string statusCallbackUrl);

        /// <summary>
        /// Calls/{Sid}: status, duration and recording URL of a call. Returns null when the lookup fails.
        /// </summary>
        Task<ExotelCallDetails> GetCallDetailsAsync(string callSid);

        /// <summary>
        /// Downloads a recording (Exotel recording URLs need the API credentials). Only *.exotel.com URLs are fetched.
        /// <paramref name="range"/> is the browser's Range header, forwarded so audio players can seek.
        /// The caller disposes the response. Returns null when the URL is not an Exotel URL.
        /// </summary>
        Task<System.Net.Http.HttpResponseMessage> GetRecordingAsync(string recordingUrl, string range);
    }

    public class ExotelConnectResult
    {
        public bool Success { get; set; }
        /// <summary>Exotel CallSid — stored as CallRecordingData.CallSid like the Twilio / Plivo call ids.</summary>
        public string Sid { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ExotelCallDetails
    {
        public string Sid { get; set; }
        public string Status { get; set; }
        public string RecordingUrl { get; set; }
        public int? DurationSeconds { get; set; }
    }
}
