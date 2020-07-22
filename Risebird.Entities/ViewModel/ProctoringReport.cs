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
    }
}
