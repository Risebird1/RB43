using System;

namespace Risebird.Entities.Data
{
    public class OpenAIApiLog
    {
        public long Id { get; set; }
        public DateTime LogDate { get; set; }
        public string ApiEndpoint { get; set; }
        public string ApiMethod { get; set; }
        public string Model { get; set; }
        public string AssistantId { get; set; }
        public string ThreadId { get; set; }
        public string RunId { get; set; }
        public string FileId { get; set; }
        public Guid? SessionId { get; set; }
        public string RequestPayload { get; set; }
        public string ResponsePayload { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        public int? TotalTokens { get; set; }
        public decimal? EstimatedCostUSD { get; set; }
        public int? ResponseTimeMs { get; set; }
        public int? StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int? UserId { get; set; }
        public int? CandidateId { get; set; }
        public int? InterviewerId { get; set; }
        public int? IRFId { get; set; }
        public int? MeetingId { get; set; }
        public string OperationType { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class OpenAIApiLogContext
    {
        public Guid? SessionId { get; set; }
        public int? UserId { get; set; }
        public int? CandidateId { get; set; }
        public int? InterviewerId { get; set; }
        public int? IRFId { get; set; }
        public int? MeetingId { get; set; }
        public string OperationType { get; set; }
        public string CreatedBy { get; set; }
    }

    public class OpenAIApiLogSummary
    {
        public DateTime Date { get; set; }
        public string OperationType { get; set; }
        public string Model { get; set; }
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public long TotalTokens { get; set; }
        public long TotalPromptTokens { get; set; }
        public long TotalCompletionTokens { get; set; }
        public decimal TotalCostUSD { get; set; }
        public int AvgResponseTimeMs { get; set; }
    }
}
