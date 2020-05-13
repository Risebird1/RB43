using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerSkillsRepository
    {
        Task<IEnumerable<InterviewerSkillsModel>> GetAllInterviewerSkills(string profileName);
        IEnumerable<Level> GetLevels();
        IEnumerable<Skill> GetSkills();
        IEnumerable<Users> GetCientUsers();
        IEnumerable<Client> GetClients();
        IEnumerable<InterviewerVM> GetInterviewers();
        IEnumerable<ClientRole> GetClientRoles();
        Task<bool> AddInterviewerSkillsDetails(InterviewerSkillsVM InterviewerSkillsDetails);
        IEnumerable<LevelTech> GetLevelTech();
        IEnumerable<ResumeEvaluator> GetResumeEvaluators();
        IEnumerable<HR> GetHRs();
        IEnumerable<Recruiter> GetRecruiters();
        IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix();
        IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix();
        IEnumerable<Users> GetUsers();
        IEnumerable<Interviewer> GetInterviewerSkills();
        IEnumerable<Interviewer> GetInterviewerSkillofInterviewerSkills();
        Skill GetSkillById(int id);
        bool CheckUniqueness(string value);
        InterviewerSkillsModel EditInterviewerSkills(int InterviewerSkillId);
        IEnumerable<TransInterviewerSkillsofSkills> GetSkillsbyInterviewerSkillId(int id);
        IEnumerable<TransInterviewerSkillsofGeneralSkill> GetSkillsGeneralbyInterviewerSkillId(int id);
        IEnumerable<TransInterviewerSkillsLeadInterviewer> GetTransInterviewer(int id);
        IEnumerable<TransInterviewerSkillsSelectability> GetTransSelectability(int id);
        Task<bool> UpdateInterviewerSkillsDetails(InterviewerSkillsVM irfDetails);
        Task<IEnumerable<InterviewerSkillsVM>> GetUnApprovedInterviewerList(int id);
        Task<bool> DeleteInterviewerSkills(int InterviewerSkillId);
        //Task<bool> UnArchiveInterviewerSkills(int InterviewerSkillId);
        bool AddInterviewer(InterviewerSkillsVM model);
        bool AddInterviewer(int id, bool IsChecked);

        IEnumerable<InterviewerSkillsVM> GetInterviewerSkillsInterviewer(int id);
        int GetUserId(string emailId, string profile);
        bool PlayInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName);
        bool PauseInterviewerSkills(int InterviewerSkillId, string InterviewerSkillName);
        Task<bool> DeleteInterviewerList(int id);

        //Task<IEnumerable<MeetingVM>> UpdateMeetingStatus(string InterviewerSkillId);
        // IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix();
        //IEnumerable<Users> GetQualityController();
        //IEnumerable<InterviewerSkillsModel> GetAllInterviewerSkill(string emailid, string profileName);
        //IEnumerable<TransClientUser> GetTransClientUser(int id);
        //IEnumerable<TransJoinability> GetTransJoinability(int id);
        //IEnumerable<TransQualityController> GetTransQualityController(int id);
        //bool AddSkillComments(TechSkillComments obj);
        //Task<IEnumerable<string>> GetListOfInterviwerBasedOnInterviewerSkillId(string InterviewerSkillId);
        //IEnumerable<MeetingVM> GetIRFInterviewMeetings(int id);
        //string GetQCSkills(int userId);
        //IEnumerable<SkillComment> GetTechSkillComments(int id);
        //string GetQuestionSkills(string id);
        //IEnumerable<Users> GetClientUser(int id);
        //string GetClientUserRole(int userId);
        //string GetInterviewerSkillsNameBasedOnId(int InterviewerSkillId);
        //string GetInterviewerSkillsJDTitleBasedOnId(int InterviewerSkillId);
        //Task<string> AddCandidate(Candidate obj);
        //Task<string> AddMultipleCandidates(List<Candidate> candidateList, int userId);
        //bool UpdateCandidate(Candidate obj);
        //InterviewerSkillsModel GetInterviewerSkillsbyId(int id);
        //bool UpdateInterviewerSkillsInvites(int id, int totalInvites);
        //bool UpdateCandidateInvites(int id, int totalInvites);
        //InterviewPlanner GetInterviewPlanner(int id);
        //Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model);
        //IEnumerable<TransEmergencyInterviewer> GetTransEmergencyInterviewer(int id);
        //Task<IEnumerable<string>> GetEmergencyInterviwerBasedOnInterviewerSkillId(int InterviewerSkillId);
        //bool UpdateEmergencyFlag(int id, string emailId, DateTime interviewDate);
        //IEnumerable<Candidate> GetCandidateInterviewerSkillsInvites(string emailId);

        //IEnumerable<TransResumeEvaluator> GetTransResumeEvaluator(int id);
        //IEnumerable<TransHR> GetTransHR(int id);
        //IEnumerable<string> GetHRsMailIds(int InterviewerSkillId);
        //IEnumerable<string> GetResumeEvaluatorsMailIds(int InterviewerSkillId);
        //int InterviewMeetings(string irfName, string candidateEmail);
        //IEnumerable<InterviewComments> GetInterviewComments(string idList);
        //IEnumerable<string> GetInterviewerSkillsCandidatesEmail(string irfName);
        //string GetRecruiterDomain(int userId);
        //IEnumerable<TransRecruiter> GetTransRecruiter(int id);
        //IEnumerable<InterviewVM> GetUpComingInterviewList();
        //IEnumerable<IRFVm> GetUpComingInterviewerSkillsList();
        //IEnumerable<TransClientUser> GetTransClientUserRole(int id);
        //IEnumerable<MeetingVM> GetInterviewerSkillsInterviewMeetingsCountData(int id);
        ////bool AddPausedMeetings(int InterviewerSkillId, int meetingId);
        ////Task<IEnumerable<MeetingVM>> PlayIRF(string InterviewerSkillId);
        //IEnumerable<TransClientUser> GetTransClientUserRole();
    }
}
