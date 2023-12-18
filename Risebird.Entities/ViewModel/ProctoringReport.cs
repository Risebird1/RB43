using System;
using System.ComponentModel;

namespace Risebird.Entities.ViewModel
{
    public class ProctoringReport
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public int EyesDirection { get; set; }
        public int EyesReflection { get; set; }
        public int HeadDirection { get; set; }
        public int LipsOverhead { get; set; }
        public int LipsDarklight { get; set; }
        public int LipsHeadphone { get; set; }
        public int LatencyJitter { get; set; }
        public int DesktopSuspiciousApps { get; set; }
        public int DesktopScreenSharing { get; set; }
        public int VoiceAudioSplitter { get; set; }
        public int VoiceToneExpression { get; set; }
        public int VoicePersonalUnsatisfactory { get; set; }
        public int VoiceSuspicious { get; set; }
        public int LinkedInPhotoVerification { get; set; }
        [DisplayName("Eye Movement")]
        public string EyeMovement { get; set; }

        [DisplayName("Lips Movement")]
        public string LipsMovement { get; set; }
        [DisplayName("Using Suspicious applications")]
        public string CandidateDesktop { get; set; }
        [DisplayName("Photo Verification")]
        public string LinkedinPhoto { get; set; }


        [DisplayName("Using Susipicous devices or techniques")]
        public string SusipicousDevices { get; set; }

        [DisplayName("Head Movement")]
        public string HeadMovement { get; set; }

        [DisplayName("Latency Check")]
        public string LatencyCheck { get; set; }

        [DisplayName("Suspicious Behavior")]
        public string VoiceImpostering { get; set; }

        [DisplayName("Suspicious Voice")]
        public string SuspiciousVoice { get; set; }

        [DisplayName("Suspicious Environment")]
        public string SuspiciousEnvironment { get; set; }
        public bool isProxyCandidate { get; set; }
        public bool IsQCdone { get; set; }
        public string type { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
