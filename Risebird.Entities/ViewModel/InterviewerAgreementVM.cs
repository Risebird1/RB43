using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Interviewer service agreement details, stored on InterviewerProfile.</summary>
    public class InterviewerAgreementVM
    {
        public int InterviewerProfileId { get; set; }
        public bool IsAgreementSigned { get; set; }
        public string AgreementVersion { get; set; }
        public string AgreementSignerName { get; set; }
        public string AgreementFatherName { get; set; }
        public string AgreementAddress { get; set; }
        public DateTime? AgreementSignedOn { get; set; }
        public string AgreementSignatureUrl { get; set; }
    }

    /// <summary>What the interviewer entered when signing the agreement.</summary>
    public class InterviewerAgreementSignRequest
    {
        public string SignerName { get; set; }
        public string FatherName { get; set; }
        public string Address { get; set; }
        /// <summary>PNG of the drawn / uploaded signature.</summary>
        public byte[] SignatureImage { get; set; }
    }
}
