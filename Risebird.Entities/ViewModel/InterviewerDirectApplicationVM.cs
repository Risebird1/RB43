using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerDirectApplicationVM
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        [Required]
        public string EmailId { get; set; }

        [Required]
        public string Phone { get; set; }
        public string ResumeLink { get; set; }
        public string EncryptedIrfId { get; set; }

        [Required]
        public IFormFile ResumeFile { get; set; }
    }
}
