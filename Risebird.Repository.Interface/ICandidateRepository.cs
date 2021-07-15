using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ICandidateRepository
    {
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null);
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null);
        Task<IEnumerable<CandidateLastStatus>> GetCandidateLastStatus();
        Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid);
        Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail);
        Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid);
        Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid);
        Task<Candidate> GetCandidatesDetails(int irfId, int candidateId);
        Task<bool> AddResume(List<CandidateResume> resumeList);
        Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId);
        Task<bool> DeleteCandidateResume(int id);
    }
}
