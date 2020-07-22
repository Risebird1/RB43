using System;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerCertificate
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string CertificateID { get; set; }
        public string CertificateName { get; set; }
        public string Version { get; set; }
        [DataType(DataType.Date)]
        public DateTime? Expirydate { get; set; }
    }
}
