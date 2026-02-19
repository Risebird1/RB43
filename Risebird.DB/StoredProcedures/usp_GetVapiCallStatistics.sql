-- =============================================
-- Author: Vapi Integration
-- Create date: 2026-01-29
-- Description: Gets comprehensive statistics for Vapi calls
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetVapiCallStatistics]
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @InterviewId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default to last 30 days if not specified
    IF @StartDate IS NULL
        SET @StartDate = DATEADD(day, -30, GETDATE());
    
    IF @EndDate IS NULL
        SET @EndDate = GETDATE();

    -- Overall statistics
    SELECT 
        COUNT(*) as TotalCalls,
        SUM(CASE WHEN CallStatus = 'ended' THEN 1 ELSE 0 END) as CompletedCalls,
        SUM(CASE WHEN CallStatus = 'ended' AND EndedReason NOT IN ('no-answer', 'busy', 'failed') THEN 1 ELSE 0 END) as SuccessfulCalls,
        SUM(CASE WHEN CallStatus IN ('failed', 'busy') OR EndedReason = 'no-answer' THEN 1 ELSE 0 END) as FailedCalls,
        SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) as InterviewsBooked,
        SUM(CASE WHEN CallStatus = 'ended' AND (InterviewBooked IS NULL OR InterviewBooked = 0) THEN 1 ELSE 0 END) as PendingFollowUp,
        AVG(CAST(DurationSeconds AS FLOAT)) as AvgDurationSeconds,
        SUM(CAST(CallCost AS FLOAT)) as TotalCost,
        MIN(CallStartedAt) as FirstCallDate,
        MAX(CallStartedAt) as LastCallDate,
        CAST(SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,2)) as ConversionRate
    FROM VapiCallLogs
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate
      AND (@InterviewId IS NULL OR InterviewId = @InterviewId);

    -- Calls by status
    SELECT 
        ISNULL(CallStatus, 'Unknown') as Status,
        COUNT(*) as Count,
        AVG(CAST(DurationSeconds AS FLOAT)) as AvgDuration,
        SUM(CAST(CallCost AS FLOAT)) as TotalCost
    FROM VapiCallLogs
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate
      AND (@InterviewId IS NULL OR InterviewId = @InterviewId)
    GROUP BY CallStatus
    ORDER BY Count DESC;

    -- Calls by ended reason
    SELECT 
        ISNULL(EndedReason, 'Unknown') as Reason,
        COUNT(*) as Count,
        AVG(CAST(DurationSeconds AS FLOAT)) as AvgDuration
    FROM VapiCallLogs
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate
      AND (@InterviewId IS NULL OR InterviewId = @InterviewId)
      AND EndedReason IS NOT NULL
    GROUP BY EndedReason
    ORDER BY Count DESC;

    -- Daily call trend
    SELECT 
        CAST(CreatedAt AS DATE) as CallDate,
        COUNT(*) as TotalCalls,
        SUM(CASE WHEN InterviewBooked = 1 THEN 1 ELSE 0 END) as BookingsCompleted,
        AVG(CAST(DurationSeconds AS FLOAT)) as AvgDuration,
        SUM(CAST(CallCost AS FLOAT)) as DailyCost
    FROM VapiCallLogs
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate
      AND (@InterviewId IS NULL OR InterviewId = @InterviewId)
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY CallDate DESC;

    -- Most recent calls
    SELECT TOP 20
        Id,
        VapiCallId,
        RecipientName,
        RecipientPhone,
        CallStatus,
        InterviewBooked,
        DurationSeconds,
        CallCost,
        CreatedAt
    FROM VapiCallLogs
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate
      AND (@InterviewId IS NULL OR InterviewId = @InterviewId)
    ORDER BY CreatedAt DESC;
END
GO
