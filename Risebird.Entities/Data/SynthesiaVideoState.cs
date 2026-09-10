namespace Risebird.Entities.Data
{
    /// <summary>
    /// Where a Synthesia render has got to. Only the download link is treated as meaningful:
    /// the raw status is carried purely so callers can report and log what Synthesia said.
    /// </summary>
    public class SynthesiaVideoState
    {
        /// <summary>Raw Synthesia status as reported, null when the call could not be read.</summary>
        public string Status { get; set; }

        /// <summary>Set only once the render has finished.</summary>
        public string DownloadUrl { get; set; }

        public bool IsComplete => !string.IsNullOrWhiteSpace(DownloadUrl);
    }
}
