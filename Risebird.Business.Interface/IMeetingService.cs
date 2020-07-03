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
        Task<bool> AddInterviewReport(InterviewReport reportModel);
        IEnumerable<SkillComment> GetTechSkillComments(int IRFid);
        SelectList GetIRFList();
        IEnumerable<TransClientUser> GetClientsByRoles(string IRF);
        List<MeetingVM> GetClientMeetingList(string emailId);
        bool PostApproval(string imid, string value, string comment);
        bool PostSelectionStatus(string imid, string value, string comment);
        bool SaveClientFeedback(string imid, string isApproved, string approvalComment, string isSelected, string selectionComment, string joiningStatus, string joiningComment, string offerStatus, string offerComment);
        List<Meeting> GetQualityControlInterviews(string emailID);
        bool SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled);
        List<Meeting> GetQCOpenInterviews(string emailId);
        string QCAssignYourself(int meetingid, string interviewerMail);
        bool QCUnassignYourself(int meetingid, string email);
        List<Meeting> GetCandidateInterviews(string emailID,string status);
        bool CancelInterview(int meetingid, string interviewDate, string candidateEmail);
        List<string> GetClientHRContactList(string emailId, List<MeetingVM> meetingList);
        List<string> GetEndClientList(string emailId, List<MeetingVM> meetingList);
        List<string> GetIRFList(string emailId, List<MeetingVM> meetingList);
        VerifyCandidateAudio GetCandidateAudios(int candidateId, int irfId);
        string VerifyCandidateVoice(string userId, string fileUrl, string phrase, int meetingId, string type);

        MeetingVM GetMeetingbyid(int id, string Email);
        List<MeetingVM> GetRiseTubeMeetings(int clientId, string profile, string Emailid);
        MeetingVM VedioLike(string value, string vedioid,string email);
        string AddTotalViewCount(int id,string emailaddress);
        //Task<string> VedioLikeAsync(string value, string vedioid, string email);
        string VedioShare(string value, string vedioid);
        string VedioShared(string value, string vedioid,string Userid);
        string VedioComment(string value, string vedioid, string email);
        List<VedioComments> GetvedioComments(int id);
        EmailDetails GetvedioShortList(string vedioid, string email);
        string CandidateSuitable(int id,string email,string Comment);
        VedioComments Intrestreaction(string vedioid, string Status);
        string ShortListreaction(string vedioid, string Status);
        List<Meeting> ShortListedClient(string emailid, string IRFID);
        List<Meeting> InterestListClient(string emailid, string IRFID);
        string Fetchusername(string emailid);
        List<VideoCountdetail> GetTotalViewCount(int id);

        List<MeetingVM> IRFMultipleCandidateInvites(string IRFId);
        Task<bool> DeleteInterview(int imid);
        bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink);
        Task<string> Broadcast(BroadcastModel model);
        IEnumerable<Meeting> GetHROpenInterviews();
        string HRAssignYourself(int meetingid, string hrMailId);
        IEnumerable<Meeting> GetHRAssignedInterviews(string emailID);

    }
}
