using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Layout of an agreement PDF, stored next to the PDF as "&lt;pdf name&gt;.layout.json".
    /// All positions are in PDF points (1/72 inch) from the TOP-LEFT of the page; pages are 1-based.
    /// A new agreement version only needs a new PDF + layout file, no code change.
    /// </summary>
    public class AgreementDocumentLayout
    {
        /// <summary>Areas painted white before upload (e.g. placeholder text replaced by fields).</summary>
        public List<AgreementLayoutRect> Clear { get; set; } = new List<AgreementLayoutRect>();
        /// <summary>Static text drawn before upload (e.g. re-flowed wording around the fields).</summary>
        public List<AgreementLayoutText> Text { get; set; } = new List<AgreementLayoutText>();
        /// <summary>Signature fields: Zoho Sign fields, or where the portal stamps the signer's input.</summary>
        public List<AgreementLayoutField> Fields { get; set; } = new List<AgreementLayoutField>();
        /// <summary>Portal signing: where the electronic-signature audit block is printed (Page 0 = last page).</summary>
        public AgreementLayoutAudit Audit { get; set; } = new AgreementLayoutAudit();
    }

    public class AgreementLayoutAudit
    {
        public int Page { get; set; }
        public float X { get; set; } = 77;
        /// <summary>Top of the audit block from the top of the page.</summary>
        public float Y { get; set; } = 610;
    }

    /// <summary>What the interviewer entered when signing in the portal, plus audit details.</summary>
    public class PortalSignatureStamp
    {
        public string FullName { get; set; }
        public string FatherName { get; set; }
        public string Address { get; set; }
        /// <summary>PNG of the drawn signature.</summary>
        public byte[] SignatureImage { get; set; }
        public string SignerEmail { get; set; }
        public int InterviewerProfileId { get; set; }
        public string IpAddress { get; set; }
        public string Reference { get; set; }
        /// <summary>Signing time, IST.</summary>
        public DateTime SignedOn { get; set; }
    }

    public class AgreementLayoutRect
    {
        public int Page { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
    }

    public class AgreementLayoutText
    {
        public int Page { get; set; }
        public float X { get; set; }
        /// <summary>Baseline position from the top of the page.</summary>
        public float Baseline { get; set; }
        public string Value { get; set; }
        public float FontSize { get; set; } = 12;
    }

    public class AgreementLayoutField : AgreementLayoutRect
    {
        /// <summary>Who fills it: "Interviewer" or "Company" (Risebird's authorised signatory).</summary>
        public string Signer { get; set; } = "Interviewer";
        /// <summary>Signature, Textfield, Date (signing date, auto-filled) or Name (signer's name, auto-filled).</summary>
        public string Type { get; set; }
        /// <summary>Field label; for Textfields it must match the *FieldLabel options so values can be read back.</summary>
        public string Label { get; set; }
        public float FontSize { get; set; } = 11;
        public bool Mandatory { get; set; } = true;
    }

    /// <summary>The agreement PDF prepared for signing (placeholders cleared, text re-flowed) plus its layout.</summary>
    public class AgreementDocument
    {
        public string FileName { get; set; }
        public byte[] Pdf { get; set; }
        public AgreementDocumentLayout Layout { get; set; }
        /// <summary>Page sizes in points, index 0 = page 1.</summary>
        public List<(float Width, float Height)> PageSizes { get; set; } = new List<(float Width, float Height)>();
    }
}
