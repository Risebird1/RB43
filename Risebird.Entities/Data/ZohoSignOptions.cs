namespace Risebird.Entities.Data
{
    /// <summary>
    /// Zoho Sign / interviewer service agreement settings (appsettings section "ZohoSign").
    /// Zoho Sign has its own Zoho API Console client and refresh token (scopes
    /// ZohoSign.documents.CREATE,ZohoSign.documents.READ,ZohoSign.documents.UPDATE), separate from Zoho Meeting.
    /// </summary>
    public class ZohoSignOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccountsBaseUrl { get; set; } = "https://accounts.zoho.in";
        public string ApiBaseUrl { get; set; } = "https://sign.zoho.in/api/v1";

        // How the interviewer signs:
        // "Portal":   entirely inside the Risebird portal - details, drawn signature and an audit block
        //             are stamped into the PDF by the portal (no Zoho Sign call, no credits).
        // "ZohoSign": through Zoho Sign (in-portal embedded signing needs the Professional plan).
        public string SigningMode { get; set; } = "Portal";
        public bool IsPortalSigning => !string.Equals(SigningMode, "ZohoSign", System.StringComparison.OrdinalIgnoreCase);

        // The agreement PDF (signed in the portal, or uploaded to Zoho Sign). Its field layout lives
        // next to it as "<DocumentName>.layout.json" (see AgreementDocumentLayout).
        // "Local": read from LocalDocumentFolder (relative to the web app's content root).
        // "Blob":  read from DocumentBlobContainer in Azure Storage.
        public string DocumentSource { get; set; } = "Local";
        public string DocumentName { get; set; } = "Interviewer_Service_Agreement_v2.pdf";
        public string LocalDocumentFolder { get; set; } = "AgreementDocuments";
        public string DocumentBlobContainer { get; set; } = "agreementfiles";
        public string InterviewerAgreementVersion { get; set; } = "v2";
        public string RequestName { get; set; } = "Risebird Service Agreement";
        public int ExpirationDays { get; set; } = 30;

        // Optional countersignature by Risebird's authorised signatory; signs after the interviewer.
        // Leave the email empty to have the interviewer as the only signer.
        public string CompanySignerName { get; set; } = string.Empty;
        public string CompanySignerEmail { get; set; } = string.Empty;

        // Labels of the Textfields the interviewer fills in while reading the agreement
        // (must match the labels in the layout file); read back after signing.
        public string NameFieldLabel { get; set; } = "Interviewer Name";
        public string FatherNameFieldLabel { get; set; } = "Father Name";
        public string AddressFieldLabel { get; set; } = "Address";

        // false: Zoho emails the signing link to the interviewer (works on the Standard plan).
        // true:  the interviewer signs inside the portal - needs Zoho Sign Professional or higher
        //        (Standard rejects embedded requests with error 8026 "Upgrade your Zoho Sign license").
        public bool UseEmbeddedSigning { get; set; } = false;

        // Secret configured on the Zoho Sign webhook; used to verify X-ZS-WEBHOOK-SIGNATURE.
        // Empty switches signature validation off (status is always re-read from Zoho anyway).
        public string WebhookSecret { get; set; } = string.Empty;

        public string SignedDocumentContainer { get; set; } = "agreementfiles";
    }
}
