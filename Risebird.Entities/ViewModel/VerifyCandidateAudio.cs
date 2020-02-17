using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class VerifyCandidateAudio
    {
        public int MeetingId { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        [DisplayName("Candidate's Name")]
        public string CandidateName { get; set; }
        [DisplayName("IRF")]
        public string IRFName { get; set; }

        public IList<CandidateAudio> CandidateAudios;

        [Required]
        [DisplayName("Audio File")]
        public IFormFile AudioFile { get; set; }
        public string AudioFileLink { get; set; }
        public string VoiceItUserId { get; set; }
        public string PhraseDetail { get; set; }
    }
}
