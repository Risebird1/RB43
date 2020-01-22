using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateAudio
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public int UploadedByUserId { get; set; }
        public string AudioFileLink { get; set; }

        [Required]
        [DisplayName("Phrase Detail")]
        public string PhraseDetail { get; set; }
        [DisplayName("Candidate's Name")]
        public string CandidateName { get; set; }
        [DisplayName("IRF")]
        public string IRFName { get; set; }

        [DisplayName("Voice Biometric")]
        public bool IsVoiceBiometric { get; set; }

        [Required]
        [DisplayName("Audio File")]
        public IFormFile AudioFile { get; set; }
        public string Description { get; set; }

    }
}
