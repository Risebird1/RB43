using System;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class QCFormVM
    {
        public int Interviewid { get; set; }
        public int Userid { get; set; }
        public string Emailid { get; set; }
        public string Profile { get; set; }
        public string Interviewer { get; set; }
        public string QCFormedBy { get; set; }
        public string IRFId { get; set; }
        public string Client { get; set; }
        public DateTime InterviewDate { get; set; }
        public int Redflags { get; set; }
        public bool Isedit { get; set; }
        public string Type { get; set; }

        //Video Quality Check
        [Display(Name = "Whether hands-on coding is present")]
        public bool IsCodingPresent { get; set; }
        [Display(Name = "Whether candidate video is clearly visible")]
        public bool IsVideoClearQuality { get; set; }
        [Display(Name = "Clarity in Interviewer’s communication")]
        public bool IsInterviewerCommunicationClear { get; set; }
        [Display(Name = "Whether video duration is sufficient")]
        public bool IsSufficientVideoDuration { get; set; }
        [Display(Name = "Is the interview properly structured")]
        public bool IsInterviewStructured { get; set; }

        //Feedback Report Quality Check
        [Display(Name = "Whether scoring rubrics is filled correctly")]
        public bool IsScoringrubriccorrect { get; set; }
        [Display(Name = "Whether questions are filled with timeline")]
        public bool IsQuestionswithtime { get; set; }
        [Display(Name = "Whether appropriate/fitting comments are filled")]
        public bool IsCommentfilled { get; set; }
        [Display(Name = "Whether appropriate meeting and interview status are filled")]
        public bool IsMeetingStatusfilled { get; set; }
        [Display(Name = "Relevance of the questions asked")]
        public bool IsRelevancequestions { get; set; }

        //Hygiene
        [Display(Name = "Delay in resuming an interview")]
        public bool IsDelayresuminginterview { get; set; }
        [Display(Name = "Background Noise")]
        public bool IsBackgroundnoise { get; set; }
        [Display(Name = "Poor interviewer infrastructure")]
        public bool IsPoorinterviewerinfrastructure { get; set; }

        //Candidate Handling
        [Display(Name = "Poor handling of candidate’s queries")]
        public bool IsPoorhandlingcandidatequery { get; set; }
        [Display(Name = "Asking personal questions or making personal remarks")]
        public bool IsPersonalquestions { get; set; }
        [Display(Name = "Getting into arguments")]
        public bool IsAugmenting { get; set; }
        [Display(Name = "Biased View")]
        public bool IsBiasedView { get; set; }
        [Display(Name = "Letting candidate speak")]
        public bool IsLettingcandidatespeak { get; set; }

        //Proctoring
        [Display(Name = "Suspicious app found")]
        public bool IsSuspiciousappfound { get; set; }
        [Display(Name = "Lip-sync issue noticed")]
        public bool IsLipsyncissue { get; set; }
    }
}
