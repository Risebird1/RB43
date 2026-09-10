namespace Risebird.Entities.Data
{
    /// <summary>
    /// Outcome of asking Synthesia to start a render. Carries the reason a request was refused
    /// rather than collapsing every failure into a null id, which left callers unable to say
    /// anything more useful than "no video id came back".
    /// </summary>
    public class SynthesiaCreateResult
    {
        public string VideoId { get; set; }

        /// <summary>Why the render could not be started, null when it was.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>HTTP status Synthesia replied with, 0 when the call never completed.</summary>
        public int HttpStatusCode { get; set; }

        /// <summary>Synthesia's raw response body, for logging.</summary>
        public string RawResponse { get; set; }

        public bool IsSuccess => !string.IsNullOrWhiteSpace(VideoId);
    }
}
