# OpenAI Cost Logging System

## Overview

This document describes the comprehensive OpenAI API cost logging system implemented across the Risebird solution. The system automatically tracks and logs every OpenAI API call, including token usage, costs, response times, and contextual information.

## Features

✅ **Automatic Logging**: All OpenAI API calls are automatically logged  
✅ **Token Tracking**: Captures input tokens, output tokens, and total tokens  
✅ **Cost Calculation**: Estimates costs based on model pricing  
✅ **Performance Monitoring**: Records response times for each API call  
✅ **Error Tracking**: Logs failed API calls with error messages  
✅ **Contextual Information**: Links API calls to candidates, interviewers, IRFs, meetings, and sessions  
✅ **Analytics & Reporting**: Provides summaries by date, operation type, and model  

## Database Schema

### Table: `OpenAIApiLogs`

| Column | Type | Description |
|--------|------|-------------|
| Id | BIGINT | Primary key |
| LogDate | DATETIME2(7) | Timestamp of the API call |
| ApiEndpoint | NVARCHAR(500) | OpenAI API endpoint called |
| ApiMethod | NVARCHAR(50) | HTTP method (GET, POST, etc.) |
| Model | NVARCHAR(100) | OpenAI model used (e.g., gpt-4, whisper-1) |
| AssistantId | NVARCHAR(100) | OpenAI Assistant ID |
| ThreadId | NVARCHAR(100) | OpenAI Thread ID |
| RunId | NVARCHAR(100) | OpenAI Run ID |
| FileId | NVARCHAR(100) | OpenAI File ID |
| SessionId | UNIQUEIDENTIFIER | Interview session ID |
| RequestPayload | NVARCHAR(MAX) | Request payload (JSON) |
| ResponsePayload | NVARCHAR(MAX) | Response payload (JSON) |
| PromptTokens | INT | Number of input/prompt tokens |
| CompletionTokens | INT | Number of output/completion tokens |
| TotalTokens | INT | Total tokens used |
| EstimatedCostUSD | DECIMAL(18,6) | Estimated cost in USD |
| ResponseTimeMs | INT | Response time in milliseconds |
| StatusCode | INT | HTTP status code |
| IsSuccess | BIT | Whether the API call succeeded |
| ErrorMessage | NVARCHAR(MAX) | Error message if failed |
| UserId | INT | User who initiated the call |
| CandidateId | INT | Associated candidate ID |
| InterviewerId | INT | Associated interviewer ID |
| IRFId | INT | Associated IRF ID |
| MeetingId | INT | Associated meeting ID |
| OperationType | NVARCHAR(100) | Type of operation (see below) |
| CreatedBy | NVARCHAR(100) | User who created the record |
| CreatedAt | DATETIME2(7) | Record creation timestamp |

### Operation Types

- **CandidateScreening**: Resume screening for candidates
- **InterviewerScreening**: Resume screening for interviewers
- **ResumeParsing**: Extracting structured data from resumes
- **TranscriptAnalysis**: Analyzing interview transcripts
- **ResumeAnalysis**: Analyzing resume content
- **ReportQC**: Quality checking interview reports
- **RubricGeneration**: Generating technical skill rubrics
- **AIInterview**: AI-powered interview sessions
- **SpeechToText**: Audio transcription (Whisper)
- **TextToSpeech**: Text-to-speech conversion
- **ThreadCreation**: Creating OpenAI threads
- **MessageRetrieval**: Retrieving messages from threads
- **RunRetrieval**: Retrieving run status
- **FileUpload**: Uploading files to OpenAI

## Architecture

### Components

1. **OpenAIApiLog** (Entity)
   - Location: `Risebird.Entities\Data\OpenAIApiLog.cs`
   - Contains data models for logs and summaries

2. **IOpenAIApiLogRepository** (Interface)
   - Location: `Risebird.Repository.Interface\IOpenAIApiLogRepository.cs`
   - Defines repository operations

3. **OpenAIApiLogRepository** (Implementation)
   - Location: `Risebird.Repository\OpenAIApiLogRepository.cs`
   - Implements data access using Dapper

4. **IOpenAIApiLogger** (Interface)
   - Location: `Risebird.CommonHelper.Interface\IOpenAIApiLogger.cs`
   - Defines logging operations

5. **OpenAIApiLogger** (Implementation)
   - Location: `Risebird.CommonHelper\OpenAIApiLogger.cs`
   - Implements automatic logging and cost calculation

### Integrated Services

The following services have been updated to use the logging system:

- ✅ **AssistantServiceHelper**: All screening, parsing, and analysis operations
- ✅ **OpenAIInterviewerAssistantHelper**: AI interview operations
- ✅ **OpenAISpeechToTextService**: Speech-to-text and text-to-speech operations

## Cost Calculation

The system uses the following pricing (as of 2024):

| Model | Input Cost (per 1K tokens) | Output Cost (per 1K tokens) |
|-------|----------------------------|----------------------------|
| GPT-4 | $0.03 | $0.06 |
| GPT-4 Turbo | $0.01 | $0.03 |
| GPT-3.5 Turbo | $0.0015 | $0.002 |
| Whisper | $0.006 per minute | - |
| TTS (Standard) | $0.015 per 1K characters | - |
| TTS (HD) | $0.030 per 1K characters | - |

**Note**: Update pricing in `OpenAIApiLogger.cs` as needed.

## Usage Examples

### Querying Logs

```csharp
// Get logs for a date range
var logs = await _openAIApiLogRepository.GetLogsByDateRangeAsync(startDate, endDate);

// Get total cost for a period
var totalCost = await _openAIApiLogRepository.GetTotalCostAsync(startDate, endDate);

// Get daily summary
var summary = await _openAIApiLogRepository.GetDailySummaryAsync(startDate, endDate);

// Get logs by operation type
var logs = await _openAIApiLogRepository.GetLogsByOperationTypeAsync("CandidateScreening");

// Get failed calls
var failedCalls = await _openAIApiLogRepository.GetFailedCallsAsync(startDate, endDate);

// Get logs for a specific candidate
var candidateLogs = await _openAIApiLogRepository.GetLogsByCandidateIdAsync(candidateId);
```

### Viewing Reports

Access the following URLs in your browser:

- **All Logs**: `/OpenAILogs/Index`
- **Summary Dashboard**: `/OpenAILogs/Summary`
- **By Operation Type**: `/OpenAILogs/ByOperationType?operationType=CandidateScreening`
- **Failed Calls**: `/OpenAILogs/Failed`
- **By Session**: `/OpenAILogs/BySession?sessionId={guid}`
- **By Candidate**: `/OpenAILogs/ByCandidate?candidateId={id}`
- **By Interviewer**: `/OpenAILogs/ByInterviewer?interviewerId={id}`
- **By IRF**: `/OpenAILogs/ByIRF?irfId={id}`
- **By Meeting**: `/OpenAILogs/ByMeeting?meetingId={id}`

### Stored Procedures

```sql
-- Get cost summary by date range
EXEC usp_GetOpenAICostSummaryByDateRange 
    @StartDate = '2024-01-01', 
    @EndDate = '2024-12-31';

-- Get top candidates by cost
EXEC usp_GetOpenAICostByCandidate 
    @StartDate = '2024-01-01', 
    @EndDate = '2024-12-31', 
    @Top = 100;

-- Get top interviewers by cost
EXEC usp_GetOpenAICostByInterviewer 
    @StartDate = '2024-01-01', 
    @EndDate = '2024-12-31', 
    @Top = 100;

-- Get top IRFs by cost
EXEC usp_GetOpenAICostByIRF 
    @StartDate = '2024-01-01', 
    @EndDate = '2024-12-31', 
    @Top = 100;
```

## Adding Context to Logs

To add contextual information when making OpenAI calls, use the `OpenAIApiLogContext` class:

```csharp
var context = new OpenAIApiLogContext
{
    SessionId = sessionId,
    CandidateId = candidateId,
    InterviewerId = interviewerId,
    IRFId = irfId,
    MeetingId = meetingId,
    OperationType = "CandidateScreening",
    CreatedBy = "System"
};

// The logging is now automatic in all OpenAI helper services
```

## Database Migration

To deploy the logging system:

1. **Create the table**:
   ```bash
   sqlcmd -S your-server -d your-database -i Risebird.DB\Tables\OpenAIApiLogs.sql
   ```

2. **Create stored procedures**:
   ```bash
   sqlcmd -S your-server -d your-database -i Risebird.DB\StoredProcedures\usp_GetOpenAICostSummaryByDateRange.sql
   sqlcmd -S your-server -d your-database -i Risebird.DB\StoredProcedures\usp_GetOpenAICostByCandidate.sql
   sqlcmd -S your-server -d your-database -i Risebird.DB\StoredProcedures\usp_GetOpenAICostByInterviewer.sql
   sqlcmd -S your-server -d your-database -i Risebird.DB\StoredProcedures\usp_GetOpenAICostByIRF.sql
   ```

## Monitoring & Alerts

Consider setting up:

1. **Daily Cost Alerts**: Monitor when daily costs exceed thresholds
2. **Failed Call Monitoring**: Track API failures
3. **Performance Monitoring**: Track response times
4. **Token Usage Trends**: Monitor token consumption patterns

### Example SQL Queries

```sql
-- Get today's total cost
SELECT SUM(EstimatedCostUSD) as TodayCost
FROM OpenAIApiLogs
WHERE CAST(LogDate AS DATE) = CAST(GETUTCDATE() AS DATE);

-- Get failed calls in the last 24 hours
SELECT COUNT(*) as FailedCalls
FROM OpenAIApiLogs
WHERE IsSuccess = 0 
  AND LogDate >= DATEADD(HOUR, -24, GETUTCDATE());

-- Get average response time by operation type
SELECT 
    OperationType,
    AVG(ResponseTimeMs) as AvgResponseTime,
    COUNT(*) as CallCount
FROM OpenAIApiLogs
WHERE LogDate >= DATEADD(DAY, -7, GETUTCDATE())
GROUP BY OperationType
ORDER BY AvgResponseTime DESC;
```

## Maintenance

### Data Retention

Consider implementing a data retention policy:

```sql
-- Archive old logs (older than 1 year)
-- Create archive table first, then:
INSERT INTO OpenAIApiLogs_Archive
SELECT * FROM OpenAIApiLogs
WHERE LogDate < DATEADD(YEAR, -1, GETUTCDATE());

-- Delete archived logs
DELETE FROM OpenAIApiLogs
WHERE LogDate < DATEADD(YEAR, -1, GETUTCDATE());
```

### Index Maintenance

Rebuild indexes periodically:

```sql
ALTER INDEX ALL ON OpenAIApiLogs REBUILD;
```

## Troubleshooting

### Issue: Logs not appearing

1. Check if the logging service is registered in DI container
2. Verify database connection string
3. Check for exceptions in application logs

### Issue: Incorrect cost calculations

1. Verify pricing constants in `OpenAIApiLogger.cs`
2. Check if token usage is being extracted correctly from API responses
3. Review the `CalculateCost` method logic

### Issue: Performance degradation

1. Check index fragmentation
2. Consider archiving old logs
3. Review query performance using execution plans

## Future Enhancements

- [ ] Real-time dashboard with charts and graphs
- [ ] Email alerts for cost thresholds
- [ ] Budget management and forecasting
- [ ] Cost allocation by department/project
- [ ] API rate limiting based on usage
- [ ] Integration with billing systems
- [ ] Export reports to Excel/PDF
- [ ] Cost optimization recommendations

## Support

For issues or questions about the OpenAI logging system, please contact the development team or create an issue in the project repository.

---

**Last Updated**: January 2026  
**Version**: 1.0
