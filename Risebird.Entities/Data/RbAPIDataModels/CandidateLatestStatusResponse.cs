using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class CandidateLatestStatusResponse
    {
        public string skillChannelId { get; set; }
        public string candidateEmailId { get; set; }
        public string requestedBy { get; set; }
        public string currentStatus { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
        public string retrievedAt { get; set; }
        public string interviewDate { get; set; }
    }
    public class CandidateInfoResponse
    {
        public CandidateInfoResponse()
        {
            result = new List<CandidateStatusResponse>();
        }
        public string requestedBy { get; set; }
        public string retrievedAt { get; set; }
        public List<CandidateStatusResponse> result { get; set; }
    }
    public class CandidateStatusResponse
    {
        public CandidateStatusResponse()
        {
            skills = new List<SkillDetails>();
        }
        public string skillChannelId { get; set; }
        public string candidateEmailId { get; set; }
        public string currentStatus { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public string interviewDate { get; set; }
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
        public string videoUrl { get; set; }
        public int communicationScore { get; set; }
        public List<SkillDetails> skills { get; set; }
    }
    public class SkillDetails
    {
        public SkillDetails()
        {
            questions = new List<QuestionDetails>();
        }
        public string skillName { get; set; }
        public bool isPrimary { get; set; }
        public string status { get; set; }
        public int techscore { get; set; }
        public string interviewerComment { get; set; }
        public List<QuestionDetails> questions { get; set; }
    }
    public class QuestionDetails
    {
        public string question { get; set; }
        public string rating { get; set; }
    }
    
}
