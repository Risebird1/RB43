using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Interviewer service agreement details, stored on InterviewerProfile.
    /// </summary>
    public class InterviewerAgreementVM
    {
        public int InterviewerProfileId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string City { get; set; }
        public bool IsAgreementSigned { get; set; }
        public string AgreementStatus { get; set; }
        public string AgreementVersion { get; set; }
        public string AgreementZohoRequestId { get; set; }
        public string AgreementZohoActionId { get; set; }
        // Filled in by the interviewer on the agreement in Zoho Sign; stored after signing.
        public string AgreementSignerName { get; set; }
        public string AgreementFatherName { get; set; }
        public string AgreementAddress { get; set; }
        public DateTime? AgreementSentOn { get; set; }
        public DateTime? AgreementSignedOn { get; set; }
        public DateTime? AgreementCompletedOn { get; set; }
        public string AgreementDocumentUrl { get; set; }
        public string AgreementDeclineReason { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();

        // A request that is still open in Zoho Sign and can be signed.
        public bool HasOpenRequest =>
            !string.IsNullOrEmpty(AgreementZohoRequestId)
            && (AgreementStatus == InterviewerAgreementStatus.Sent || AgreementStatus == InterviewerAgreementStatus.Viewed);
    }

    public static class InterviewerAgreementStatus
    {
        public const string Sent = "Sent";
        public const string Viewed = "Viewed";
        // Interviewer has signed; waiting on the remaining signer(s), e.g. Risebird's signatory.
        public const string InterviewerSigned = "InterviewerSigned";
        public const string Completed = "Completed";
        public const string Declined = "Declined";
        public const string Expired = "Expired";
        public const string Recalled = "Recalled";
    }

    public class ZohoSignAgreementRequest
    {
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        // Used for the redirect after embedded signing.
        public string ReturnUrl { get; set; }
    }

    public class ZohoSignRequestResult
    {
        public string RequestId { get; set; }
        public string ActionId { get; set; }
        public string RequestStatus { get; set; }
    }

    public class ZohoSignRequestDetails
    {
        public string RequestId { get; set; }
        // Zoho request_status: inprogress, completed, declined, expired, recalled, ...
        public string RequestStatus { get; set; }
        // Zoho action_status of the interviewer's action: NOACTION, UNOPENED, VIEWED, SIGNED, ...
        public string ActionStatus { get; set; }
        public DateTime? SignedOn { get; set; }
        public string DeclineReason { get; set; }
    }
}
