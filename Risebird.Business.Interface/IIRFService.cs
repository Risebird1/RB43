using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
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
        Task<string> AddCandidate(Candidate obj);
        Task<string> AddMultipleCandidates(List<Candidate> candidateList);
        Task<bool> DeleteCandidate(int id);
        Candidate GetCandidate(int id, int irfId);
        bool UpdateCandidate(Candidate obj);
        bool SendInvite(int irfId, string email);
        InterviewPlanner GetInterviewPlanner(int id);
    }
}
