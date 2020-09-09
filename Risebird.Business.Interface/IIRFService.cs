using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IIRFService
    {
        Task<bool> AddIRFDetails(IRFVm irfDetails);
        IEnumerable<Users> GetRiseBirdHR();
        string GetInterviewerSkills(int id);
        IEnumerable<Skill> GetTechSkills(int id);
        bool AddSkillComments(TechSkillComments skillComments);
        List<string> GetListOfInterviwerBasedOnIRFId(string irfId);
        Task<bool> DeleteIRF(int id);
        List<MeetingVM> GetIRFInterviewMeetings(int id, string profileName, string email);
        string GetQCSkills(int id);
        SelectList GetQualityController();
        Task<IRFVm> GetEditIRF(int id);
        bool UpdateIRF(IRFVm model);
        IList<SkillComment> GetTechSkillComments(int id);
        bool CheckUniqueness(string value);

        IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix();
        IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix();
        IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix();
        string GetQuestionSkills(string id);
        IEnumerable<Users> GetClientUser(int id);
        string GetClientUserRole(int id);
        IEnumerable<Interviewer> GetInterviewers(string skills);
        IEnumerable<Candidate> GetIRFCandidates(int id);
        Task<string> AddCandidate(Candidate obj, string emailId, string profile);
        Task<string> AddMultipleCandidates(List<Candidate> candidateList, string emailId, string profile);
        Task<bool> DeleteCandidate(int id);
        Candidate GetCandidate(int id, int irfId);
        bool UpdateCandidate(Candidate obj);
        bool SendInvite(int irfId, string email);
        InterviewPlanner GetInterviewPlanner(int id);
        Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model);
        Task<bool> UnArchiveIRF(int id);
        bool CancelInvite(int irfId, string email);
        IEnumerable<Candidate> GetCandidateIRFInvites(string emailId);
        IRFFunnel GetFunnelData(int irfId, string irfName);
        InvoiceModel GetInvoiceModel(int irfId, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST, string discount, string clientAddress);
        int GetUserId(string emailId, string profile);
        string GetRecruiterDomain(int userId);
        List<InterviewVM> GetUpComingInterviewList();
        List<IRFVm> GetUpComingIRFList();
        bool PauseIRF(int irfId, string irfName);
        bool PlayIRF(int irfId, string irfName);
        IEnumerable<IRF> GetAllIRFs(string emailId, string profileName);
        Task<bool> SubscribeCuratedGroup(int id, string emailId, string profile);
        Task<bool> UnSubscribeCuratedGroup(int id, string emailId, string profile);
        bool SaveCandidateStatus(int candidateId, int status);
        IEnumerable<Interviewer> GetInterviewersForIRF(string skillNames);
        IEnumerable<Users> GetAllHRs();
        bool AddCandidateCuratedScore(int IrfId, List<CandidateCuratedScore> candidateCuratedScore);
        IList<CandidateCuratedScore> GetCandidateCuratedScore(int id);
        IEnumerable<CuratedGroup> GetAllCuratedGroups();
        IEnumerable<CuratedIRFMapping> GetIRFListForCuratedMapping();
        Task<bool> AddCuratedGroup(int[] irfList, string cgName);
        CuratedGroup GetCuratedGroupMappingsById(int curatedGroupId);
        bool UpdateCuratedGroup(int[] irfList, string cgName, int cgId);
        CuratedGroupUsersVM GetCuratedGroupUsers(int curatedGroupId);
        bool AddUpdateCuratedGroupUsers(int[] users, int curatedGroupId);
        IEnumerable<CuratedUserGroupData> GetCuratedGroupsForCuratedUser(string emailId);
        Task<IEnumerable<IRFInterviewerMembers>> GetIRFInterviewerMembers(int irfId);
    }
}
