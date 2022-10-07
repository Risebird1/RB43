using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerSkillsService
    {
        IEnumerable<InterviewerSkillsChannelModel> GetAllInterviewerSkillsChannel(string profileName, string emailId);
        Task<bool> AddInterviewerSkillsDetails(InterviewerSkillsVM InterviewerSkillsDetails);
        IEnumerable<Users> GetRiseBirdHR();
        IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix();
        IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix();
        string GetInterviewerSkills(int id);
        IEnumerable<Interviewer> GetInterviewers(string skills);
        bool CheckUniqueness(string value);
        Task<InterviewerSkillsVM> GetEditInterviewerSkills(int id);
        bool UpdateInterviewerSkills(InterviewerSkillsVM model);
        bool AddUnApprovedInterviewer(IEnumerable<UnApprovedInterviewer> model, int iscId);
        IEnumerable<UnApprovedInterviewer> GetUnApprovedInterviewerList(int id);
        Task<bool> DeleteInterviewerSkillsChannel(int id);
        //Task<bool> UnArchiveDeleteInterviewerSkills(int id);
        IEnumerable<ISCInterviewer> GetISCInterviewers(int id);
        int GetUserId(string emailId, string profile);
        bool AddInterviewer(InterviewerSkillsVM model);
        bool PauseInterviewerSkills(int InterviewerSkillId, string InterviewerSkillsName);
        bool PlayInterviewerSkills(int InterviewerSkillId, string InterviewerSkillsName);
        Task<bool> DeleteISCInterviewer(int id);
        Task<bool> UnArchiveISC(int id);
        Task<bool> SendInvite(int iscId, string email, int interviewerProfileId);
        Task<bool> CancelInvite(int iscId, string email);
        //IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix();GetAllInterviewerSkills
        //IEnumerable<Skill> GetTechSkills(int id);
        //bool AddSkillComments(TechSkillComments skillComments);
        //List<string> GetListOfInterviwerBasedOnInterviewerSkillId(string InterviewerSkillId);
        //List<MeetingVM> GetInterviewerSkillsInterviewMeetings(int id, string profileName, string email);
        //string GetQCSkills(int id);
        //SelectList GetQualityController();
        //IList<SkillComment> GetTechSkillComments(int id);

        //string GetQuestionSkills(string id);
        //IEnumerable<Users> GetClientUser(int id);
        //string GetClientUserRole(int id);
        //Task<string> AddCandidate(Candidate obj, string emailId, string profile);
        //Task<string> AddMultipleCandidates(List<Candidate> candidateList, string emailId, string profile);
        //Candidate GetCandidate(int id, int InterviewerSkillId);
        //bool UpdateCandidate(Candidate obj);
        //bool SendInvite(int InterviewerSkillId, string email);
        //InterviewPlanner GetInterviewPlanner(int id);
        //Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model);
        //bool CancelInvite(int InterviewerSkillId, string email);
        //IEnumerable<Candidate> GetCandidateInterviewerSkillsInvites(string emailId);
        //IRFFunnel GetFunnelData(int InterviewerSkillId, string InterviewerSkillsName);
        //InvoiceModel GetInvoiceModel(int InterviewerSkillId, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST, string discount, string clientAddress);
        //string GetRecruiterDomain(int userId);
        //List<InterviewVM> GetUpComingInterviewList();
        //List<InterviewerSkillsVM> GetUpComingInterviewerSkillsList();
        Task<List<MeetingVM>> GetInterviewerRiseTubeMeetingsPaged(int Page, string searchvalue);
        Task<int> GetInterviewerRiseTubeMeetingsCount(string searchvalue);
        Task<MeetingVM> GetMeetingbyid(int id, string Email);
        Task<List<MeetingVM>> GetInterviewerRiseTubeMeetingsByJD(int clientId, string Emailid, string JDpattern);
        Task<List<string>> FetchAdminList();
        Task<List<MeetingVM>> GetInterviewerSkillInterviewMeetings(int id);
        IEnumerable<Interviewer> GetInterviewersByAssessmentGroup(string assessmentgroupid);
    }
}
