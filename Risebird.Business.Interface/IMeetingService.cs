using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IMeetingService
    {
        List<MeetingVM> GetMeetingList();
        bool AddMeeting(MeetingVM meeting);
        List<MeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate);
        bool UpdateMeeting(MeetingVM meetingToUpdate);
        List<MeetingVM> MultipleCandidateInvites();
        MeetingVM GetMeeting(int id);
        List<Meeting> GetAllOpenInterviews(string emailID);
        List<Meeting> GetAllQCInterviews(string emailID);
        List<MeetingVM> GetAllClientInterviews(string emailID);

        string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate);
        bool UnassignYourself(int meetingid, string email, bool isPenalty);
        List<Meeting> GetAllAssignedInterviews(string emailID);
        bool UpdateComments(InterviewComments comments);
        InterviewComments GetInterviewComments(int interviewId);
        Task<InterviewReport> GetInteviewReport(int interviewId, int irfId);
        Task<bool> AddInterviewReport(InterviewReport reportModel, string emailId);
        IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        SelectList GetIRFList();
        IEnumerable<TransClientUser> GetClientsByRoles(string IRF);
        List<MeetingVM> GetClientMeetingList(string emailId);
        bool PostApproval(string imid, string value, string comment);
        bool PostSelectionStatus(string imid, string value, string comment);
        bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP, string clientEmailId);
        List<Meeting> GetQualityControlInterviews(string emailID);
        bool SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled);
        List<Meeting> GetQCOpenInterviews(string emailId);
        string QCAssignYourself(int meetingid, string interviewerMail);
        bool QCUnassignYourself(int meetingid, string email);
        List<Meeting> GetCandidateInterviews(string emailID, string status);
        bool CancelInterview(int meetingid, string interviewDate, string candidateEmail);
        List<string> GetClientHRContactList(string emailId, List<MeetingVM> meetingList);
        List<string> GetEndClientList(string emailId, List<MeetingVM> meetingList);
        List<string> GetIRFList(string emailId, List<MeetingVM> meetingList);
        VerifyCandidateAudio GetCandidateAudios(int candidateId, int irfId);
        string VerifyCandidateVoice(string userId, string fileUrl, string phrase, int meetingId, string type);

        MeetingVM GetMeetingbyid(int id, string Email);
        Task<List<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid, bool IsClientCurated = false);
        MeetingVM VedioLike(string value, string vedioid, string email);
        Task AddTotalViewCount(int id, string emailaddress, string flag);
        //Task<string> VedioLikeAsync(string value, string vedioid, string email);
        string VedioShare(string value, string vedioid);
        string VedioShared(string value, string vedioid, string Userid);
        string VedioComment(string value, string vedioid, string email);
        List<VedioComments> GetvedioComments(int id);
        EmailDetails GetvedioShortList(string vedioid, string email);
        string CandidateSuitable(int id, string email, string Comment);
        VedioComments Intrestreaction(string vedioid, string Status);
        string ShortListreaction(string vedioid, string Status);
        List<Meeting> ShortListedClient(string emailid, string IRFID);
        List<Meeting> InterestListClient(string emailid, string IRFID);
        string Fetchusername(string emailid);
        Task<IEnumerable<VideoCountdetail>> GetTotalViewCount(int id);

        List<MeetingVM> IRFMultipleCandidateInvites(string IRFId);
        Task<bool> DeleteInterview(int imid);
        bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink);
        Task<string> Broadcast(BroadcastModel model);
        IEnumerable<Meeting> GetHROpenInterviews();
        string HRAssignYourself(int meetingid, string hrMailId);
        IEnumerable<Meeting> GetHRAssignedInterviews(string emailID);
        bool HRUnassignYourself(int meetingid);
        bool SaveHRComment(int id, string comment);
        Task<List<MeetingVM>> GetCPMeetings(int clientId, string profile, string Emailid);
        IEnumerable<int> GetCuratedGroupIRFs(int curatedGroupId);
        Task<string> AddSharedVideoDetails(InterviewReport reportModel);
        Task<int> SharedGetInterviewId(int videoid);
        Task<InterviewReport> GetSharedInteviewReport(int interviewId, int irfId, int id);
        IEnumerable<VideoDetails> GetVideoSumaryDetails(int Interviewid);
        Task AddTimeSpend(int id, string emailaddress, string flag, TimeSpan TimeSpend, string CurrentTime);
        IEnumerable<VideoDetails> GetallVideoSumaryDetails();
        string AddVedioShare(string email, string vedioid, string Emailids);
        int CheckVedioShare(string vedioid, string Emailid);
        string AddVideoIntrestDetails(string videoid, string UrlType, string Emailid);
        IEnumerable<VideoDetails> GetVideoIntrestDetails();
        IEnumerable<string> FetchShareEmails(int Interviewid);
        IEnumerable<VideoDetails> GetVideoCandidateSumaryDetails(int Interviewid, string emailid);
        string UpdateCandidateStatus(int videoid, string Status);
        Task<SentimentPercentage> GetSentimentAnalysis(int imid);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetings();
        Task<List<QuestionDetails>> FetchQuestionDetails(int interviewid);
        string UpdateQuestionDetails(List<QuestionDetails> _question);


        Task<string> BroadcastIRFmembers(BroadcastModel model);

        IEnumerable<MeetingVM> GetClientJdIRFs(int Clientjdid, string emailId);
        Task<List<MeetingVM>> GetRiseTubeMeetingsByScore(int clientId, string profile, string Emailid, bool IsClientCurated = false);
        Task<List<MeetingVM>> GetRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern, string profileName = null);
        Task<List<MeetingVM>> GetRiseTubeMeetingsForCuratedUserByIRF(string Emailid, string actionStatus, int irfId);
        SurveyCandidateInterviewer FetchCanddiateInterviewer(int interviewid);
        string CandidateSuitableJd(int id, string email, string Comment,int Clientjdid);

        Task<List<MeetingVM>> GetRiseTubeMeetingsPaged(string profile, string Emailid, int Page, string searchValue=null, int clientId=0, bool IsClientCurated = false);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetingsPaged(int page, string searchValue = null, string profileName = null);
        Task<int> GetSharedIndexMeetingsCount(string searchValue = null, string profileName = null);
        IEnumerable<SurveyDetails> FetchSurveyInterviewer(List<SurveyDetails> _lst);
        Task<List<MeetingVM>> GetSharedRiseTubeMeetingsForClientPagedFiltered(int page, string primarySkills, string heuristicLevels, string searchValue = null);
        Task<int> GetSharedIndexMeetingsCountforClientFiltered(string primarySkills, string heuristicLevels, string searchValue = null);
        Task<int> GetRisetubeIndexMeetingsCount(string profile, string Emailid, string searchValue = null, int clientId = 0, bool IsClientCurated = false);
        Task<List<MeetingVM>> GetRiseTubeMeetingsPagedFiltered(string profile, string Emailid, int Page, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0);
        Task<int> GetRisetubeIndexMeetingsFilteredCount(string profile, string Emailid, DateTime startDate, DateTime endDate, string searchValue = null, int clientId = 0);
    }
}
