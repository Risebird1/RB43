using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IMeetingRespository
    {
        Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings();
        Task<IEnumerable<MeetingVM>> GetAllInterviewMeetings(DateTime startDate, DateTime endDate);
        Task<bool> AddMeeting(MeetingVM meeting);
        Task<MeetingVM> GetInterviewMeetingById(int id);
        Task<bool> UpdateMeeting(MeetingVM meeting);
        Task<IEnumerable<MeetingVM>> MultipleCandidateInvites();
        Task<IEnumerable<Meeting>> GetAllOpenInterviews(string emailId);
        Task<IEnumerable<Meeting>> GetAllQCInterviews(string emailId);
        Task<IEnumerable<MeetingVM>> GetAllClientInterviews(string emailId);
        Task<IEnumerable<MeetingVM>> GetClientMeetings(string emailId);
        string AssignYourself(int meetingid, string interviewerMail);
        Task<bool> UnassignYourself(int meetingid);
        Task<IEnumerable<Meeting>> GetAllAssignedInterview(string emailId);
        Task<bool> UpdateComments(InterviewComments interviewComments);
        Task<InterviewComments> GetInterviewComments(int interviewId);
        IEnumerable<TransJoinability> GetTransJoinability(int id);
        IEnumerable<TransSkill> GetSkillsbyIRFid(int id);
        IEnumerable<TransSkillGeneral> GetSkillsGeneralbyIRFid(int id);
        IEnumerable<SkillComment> GetTechSkillComments(int id);
        IEnumerable<Level> GetLevels();
        IEnumerable<Skill> GetSkills();
        Task<bool> AddInterviewReport(InterviewReport reportDetails);
        IEnumerable<TransSelectability> GetTransSelectability(int id);
        IEnumerable<InterviewReport> GetInterviewReportById(int id, int irfId = 0);

        IEnumerable<JoinabilityReport> GetJoinabilityReport(int id);
        IEnumerable<SelectabilityReport> GetSelectabilityReport(int id);
        IEnumerable<QuestionsDiscussed> GetQuestionsDiscussed(int id);
        IEnumerable<TechSkillReport> GetTechSkillReport(int id);
        IEnumerable<SkillGeneralReport> GetSkillGeneralReport(int id);
        ProctoringReport GetProctoringReport(int id);
        IEnumerable<LevelTech> GetLevelTech();
        IEnumerable<string> GetIRFName(int id);
        IRFDetails GetIRFDetails(int id);
        bool GetIRFbyId(int irfId);
        IEnumerable<IRF> GetIRFList();
        IEnumerable<TransClientUser> GetClientsByRoles(string IRF);
        IEnumerable<ClientUser> GetClientDetails(string emailId);

        Task<IEnumerable<MeetingVM>> GetClientInterviewMeetings();
        bool PostApproval(int imid, int value, string comment);
        bool PostSelectionStatus(int imid, int value, string comment);
        bool SaveClientFeedback(int imid, int isApproved, string approvalComment, int isSelected, string selectionComment, int joiningStatus, string joiningComment, int offerStatus, string offerComment, bool movetoCP);

        Task<IEnumerable<int>> CheckCompletedInterviews(string emailID, int clientId, int irfId);
        Task<IEnumerable<Meeting>> GetQualityControlInterview(string emailID);
        bool SaveQCFeedback(int imid, int qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled);
        Task<IEnumerable<Meeting>> GetQCOpenInterviews(string emailID);
        string QCAssignYourself(int meetingid, string interviewerMail);
        Task<bool> QCUnassignYourself(int meetingid);
        Task<IEnumerable<Meeting>> GetAllEmergencyOpenInterviews(string emailID);
        Task<IEnumerable<Meeting>> GetCandidateInterviews(string emailID, string status);
        bool CancelInterview(int meetingid);
        List<string> ListInterviewersMail(string candidateEmail, string interviewDate);
        int GetCandidateId(int id, string irfName);
        bool UpdateInterviewVerificationScore(int meetingId, double confidence);
        bool UpdateJoiningVerificationScore(int meetingId, double confidence);
        //Task<IEnumerable<MeetingVM>> GetClientHRContactList();

        Task<MeetingVM> GetMeetingbyid(int id, string Email);
        Task<IEnumerable<MeetingVM>> GetRiseTubeMeetings(int clientId, string profile, string Emailid);
        //string VedioLikeAsync(string value, string vedioid, string email);
        Task<MeetingVM> VedioLikeAsync(string value, string vedioid, string email);
        Task AddTotalViewCount(int id, string emailaddress, string flag);
        Task<string> VedioShare(string value, string vedioid);
        Task<string> VedioShared(string value, string vedioid, string Userid);
        Task<string> VedioComment(string value, string vedioid, string email);
        List<VedioComments> GetvedioComments(int id);
        EmailDetails GetvedioShortList(string vedioid, string email);
        string CandidateSuitable(int id, string email, string Comment);
        VedioComments Intrestreaction(string vedioid, string Status);
        string ShortListreaction(string vedioid, string Status);
        Task<IEnumerable<Meeting>> ShortListedClient(string emailid, string IRFID);
        Task<IEnumerable<Meeting>> InterestListClient(string emailid, string IRFID);
        string Fetchusername(string Emailid);
        Task<IEnumerable<VideoCountdetail>> GetTotalViewCount(int id);
        Task<IEnumerable<MeetingVM>> IRFMultipleCandidateInvites(string IRFId);
        Task DeleteInterview(int imid);
        bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink);
        Task<IEnumerable<string>> GetEmailsforBroadcast(string profile);
        Task<IEnumerable<Meeting>> GetHROpenInterviews();
        string HRAssignYourself(int meetingid, string hrMailId);
        Task<IEnumerable<Meeting>> GetHRAssignedInterviews(string emailID);
        bool HRUnassignYourself(int meetingid);
        bool SaveHRComment(int id, string comment);
        Task<IEnumerable<MeetingVM>> GetCPMeetings(int clientId, string profile, string Emailid);
        IEnumerable<int> GetCuratedGroupIRFs(int curatedGroupId);
        IEnumerable<string> GetClientUsers(string clientEmailId);
        Candidate GetCandidateDetails(int imid);
        Task<string> AddSharedVideoDetails(InterviewReport reportModel);
        Task<int> SharedGetInterviewId(int videoid);
        IEnumerable<TechSkillReport> GetSharedTechSkillReport(int id);
        IEnumerable<QuestionsDiscussed> GetSharedQuestionsDiscussed(int id);
        IEnumerable<VideoDetails> GetVideoSumaryDetails(int Interviewid);
        Task AddTimeSpend(int id, string emailaddress, string flag, TimeSpan TimeSpend, string CurrentTime);
        IEnumerable<VideoDetails> GetallVideoSumaryDetails();
        Candidate GetCandidateSharedDetails(int id);


    }
}
