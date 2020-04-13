using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerCertificate
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string CertificateID { get; set; }
        public string CertificateName { get; set; }
    }
}
