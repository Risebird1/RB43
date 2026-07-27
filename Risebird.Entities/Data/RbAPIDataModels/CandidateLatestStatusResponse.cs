using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class CandidateLatestStatusResponse
    {
        public string skillChannelId { get; set; }
        public string candidateEmailId { get; set; }
        public string requestedBy { get; set; }
        //public string currentStatus { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
        public string retrievedAt { get; set; }
        public string interviewDate { get; set; }
        public string videoUrl { get; set; }
        public int communicationScore { get; set; }
        public string createdOn { get; set; }
        public List<SkillDetails> skills { get; set; }
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
        public string jobId { get; set; }
        public int CandidateId { get; set; }
        public int InterviewId { get; set; }
        public string candidateEmailId { get; set; }
        public string interviewDate { get; set; }
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
        public string videoUrl { get; set; }
        public string transcriptUrl { get; set; }
        public int communicationScore { get; set; }
        public string createdOn { get; set; }
        public string createdBy { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public List<SkillDetails> skills { get; set; }
    }
    public class SkillDetails
    {
        public SkillDetails()
        {
            questions = new List<QuestionCommentsDetails>();
            skillset = new List<SkillsSetsDetails>();
        }
        public string skillName { get; set; }
        public bool isPrimary { get; set; }
        public string status { get; set; }
        public string Cancellationsource { get; set; }
        public string Cancellationreason { get; set; }
        public int techscore { get; set; }
        public string interviewerComment { get; set; }
        public string CodeSnippet { get; set; }
        public List<QuestionCommentsDetails> questions { get; set; }
        public List<SkillsSetsDetails> skillset { get; set; }
    }
    public class QuestionCommentsDetails
    {
        public string question { get; set; }
        public string rating { get; set; }
        public string skillName { get; set; }
    }
    public class QuestionDetailsSet
    {
        public string question { get; set; }
        public int rating { get; set; }
        public string skillName { get; set; }
        public int skillSet { get; set; }
    }
    public class InterviewChannelDetails
    {
        public InterviewChannelDetails()
        {
            questions = new List<QuestionDetailsSet>();
            skillset = new List<SkillsSets>();
        }
        public int id { get; set; }
        public int irfintid { get; set; }
        public string IRFId { get; set; }
        public DateTime? interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string JDTiltle { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public string confirmationStatus { get; set; }
        public string InterviewerComment { get; set; }
        public string VideoLink { get; set; }
        public int CommunicationalProficiency { get; set; }
        public string CandidateLatestStatus { get; set; }
        public string ReportLink { get; set; }
        public string InterviewerCodedName { get; set; }

        public int? MeetingStatusTwo { get; set; }
        public int? InterviewStatusTwo { get; set; }
        public string InterviewerCommentTwo { get; set; }
        public string MeetingStatusTwoDesc { get; set; }
        public string InterviewStatusTwoDesc { get; set; }
        public int numberOfTechStacks { get; set; }

        public string JDTitleTwo { get; set; }
        public int TechScoreone { get; set; }
        public int TechScoretwo { get; set; }
        public List<QuestionDetailsSet> questions { get; set; }
        public DateTime? createdOn { get; set; }
        public DateTime? currentStatusUpdatedAt { get; set; }
        public string jobId { get; set; }
        public List<SkillsSets> skillset { get; set; }
        public string TranscriptLink { get; set; }
        public string CodeSnippet { get; set; }
        public string CodeSnippetTwo { get; set; }
        public string Cancellationsource { get; set; }
        public string Cancellationreason { get; set; }
        public string ClientHRAttemptContact { get; set; }
        public int CandidateId { get; set; }
        public bool IsAIInterview { get; set; }
        public string SessionId { get; set; }
    }
    public class SkillsSetsDetails
    {
        public string skillName { get; set; }
        public string EvaluatedAt { get; set; }
        public string score { get; set; }
        public string Evaluatedscore { get; set; }
    }
    public class SkillsSets
    {
        public string skillName { get; set; }
        public string EvaluatedAt { get; set; }
        public int WeightageActual { get; set; }
        public int WeightageRequired { get; set; }
        public int skillSet { get; set; }
    }
}
