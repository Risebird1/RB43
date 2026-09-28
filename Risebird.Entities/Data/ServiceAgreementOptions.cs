namespace Risebird.Entities.Data
{
    /// <summary>Interviewer service agreement settings (appsettings section "ServiceAgreement").</summary>
    public class ServiceAgreementOptions
    {
        /// <summary>URL of the agreement PDF shown to interviewers (Azure Blob).</summary>
        public string DocumentUrl { get; set; } = string.Empty;
        /// <summary>Agreement version saved with each signature.</summary>
        public string Version { get; set; } = "v2";
        /// <summary>Blob container for the interviewers' signature images.</summary>
        public string SignatureContainer { get; set; } = "agreementfiles";
    }
}
