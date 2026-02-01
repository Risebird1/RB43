using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class OpenAIAssistantCostSummary
    {
        public string AssistantId { get; set; }
        public string AssistantName { get; set; }
        public string Model { get; set; }
        public string PrimaryOperationType { get; set; }
        
        // Call statistics
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public int CompletedRuns { get; set; }
        
        // Token usage
        public long TotalPromptTokens { get; set; }
        public long TotalCompletionTokens { get; set; }
        public long TotalTokens { get; set; }
        
        // Cost tracking
        public decimal TotalCostUSD { get; set; }
        public decimal? AvgCostPerCall { get; set; }
        public decimal? AvgCostPerRun { get; set; }
        
        // Usage tracking
        public DateTime? FirstUsedDate { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public int? DaysActive { get; set; }
        
        // Performance metrics
        public int? AvgResponseTimeMs { get; set; }
        public long? TotalResponseTimeMs { get; set; }
        
        // Metadata
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class AssistantCostDetails
    {
        public OpenAIAssistantCostSummary Summary { get; set; }
        public List<AssistantCostByOperation> CostByOperation { get; set; }
        public List<AssistantCostByModel> CostByModel { get; set; }
        public List<AssistantDailyCost> DailyCosts { get; set; }
    }
    
    public class AssistantCostByOperation
    {
        public string OperationType { get; set; }
        public int TotalCalls { get; set; }
        public int CompletedRuns { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AvgCost { get; set; }
    }
    
    public class AssistantCostByModel
    {
        public string Model { get; set; }
        public int TotalCalls { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalCost { get; set; }
    }
    
    public class AssistantDailyCost
    {
        public DateTime Date { get; set; }
        public int Calls { get; set; }
        public int Runs { get; set; }
        public long Tokens { get; set; }
        public decimal Cost { get; set; }
    }
}
