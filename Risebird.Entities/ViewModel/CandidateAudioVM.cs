using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateAudioVM
    {
        public CandidateAudioVM()
        {
            CandidateAudios = new List<CandidateAudio>();
        }
        public int id { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public string IRFName { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public List<CandidateAudio> CandidateAudios { get; set; }
    }
}
