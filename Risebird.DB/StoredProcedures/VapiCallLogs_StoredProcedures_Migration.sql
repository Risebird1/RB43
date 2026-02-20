-- =============================================
-- Migration script to update stored procedures after VapiCallLogs column rename
-- Author: System
-- Create date: 2026-01-29
-- Description: Updates all stored procedures that reference VapiCallLogs table
--              to use the new Recipient* column names instead of Candidate* names
-- =============================================

PRINT 'Starting stored procedure migration for VapiCallLogs table...';
PRINT '';
GO

-- =============================================
-- 1. Update usp_GetVapiCallStatistics
-- =============================================
PRINT 'Updating usp_GetVapiCallStatistics...';
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetVapiCallStatistics')
    DROP PROCEDURE [dbo].[usp_GetVapiCallStatistics];
GO

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

    -- Most recent calls (UPDATED: Using RecipientName and RecipientPhone)
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

PRINT 'usp_GetVapiCallStatistics updated successfully.';
PRINT '';
GO

-- =============================================
-- 2. Update usp_GetPendingCandidatesForVapiCalls
-- =============================================
PRINT 'Updating usp_GetPendingCandidatesForVapiCalls...';
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_GetPendingCandidatesForVapiCalls')
    DROP PROCEDURE [dbo].[usp_GetPendingCandidatesForVapiCalls];
GO

CREATE PROCEDURE [dbo].[usp_GetPendingCandidatesForVapiCalls]
    @InterviewId INT = NULL,
    @DaysAhead INT = 7,
    @MaxResults INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    -- Get candidates who:
    -- 1. Have interviews scheduled in next N days
    -- 2. Haven't been called yet via Vapi
    -- 3. Haven't confirmed their interview
    -- 4. Have valid phone numbers
    
    SELECT TOP (@MaxResults)
        i.InterviewId,
        c.CandidateId,
        c.CandidateName,
        c.CandidateEmail,
        c.MobileNumber as CandidatePhone,
        jd.JDTitle as JobTitle,
        i.InterviewDate,
        i.InterviewStatus,
        CASE 
            WHEN vcl.VapiCallId IS NOT NULL THEN 1
            ELSE 0
        END as HasBeenCalled,
        vcl.CallStatus as LastCallStatus,
        vcl.InterviewBooked as LastCallBooked
    FROM Interviews i
    INNER JOIN Candidate c ON i.CandidateId = c.CandidateId
    INNER JOIN IRF irf ON i.IRFId = irf.IRFId
    INNER JOIN JDDetails jd ON irf.JDId = jd.JDId
    LEFT JOIN (
        -- Get most recent call for each recipient (UPDATED: Using RecipientId)
        SELECT 
            RecipientId,
            VapiCallId,
            CallStatus,
            InterviewBooked,
            ROW_NUMBER() OVER (PARTITION BY RecipientId ORDER BY CreatedAt DESC) as RowNum
        FROM VapiCallLogs
    ) vcl ON c.CandidateId = vcl.RecipientId AND vcl.RowNum = 1
    WHERE 
        -- Filter by interview ID if provided
        (@InterviewId IS NULL OR i.InterviewId = @InterviewId)
        -- Interview is in the future
        AND i.InterviewDate BETWEEN GETDATE() AND DATEADD(day, @DaysAhead, GETDATE())
        -- Interview is not cancelled
        AND i.InterviewStatus NOT IN (4, 5) -- Assuming 4=Cancelled, 5=Rejected
        -- Candidate has phone number
        AND c.MobileNumber IS NOT NULL 
        AND LEN(c.MobileNumber) >= 10
        -- Either never called, or last call failed to book
        AND (vcl.VapiCallId IS NULL OR vcl.InterviewBooked IS NULL OR vcl.InterviewBooked = 0)
    ORDER BY i.InterviewDate ASC;
END
GO

PRINT 'usp_GetPendingCandidatesForVapiCalls updated successfully.';
PRINT '';
GO

-- =============================================
-- Migration Complete
-- =============================================
PRINT '========================================';
PRINT 'Stored procedure migration completed successfully!';
PRINT '';
PRINT 'Updated procedures:';
PRINT '  - usp_GetVapiCallStatistics';
PRINT '  - usp_GetPendingCandidatesForVapiCalls';
PRINT '';
PRINT 'Changes made:';
PRINT '  - CandidateName -> RecipientName';
PRINT '  - CandidatePhone -> RecipientPhone';
PRINT '  - CandidateId -> RecipientId (in JOINs)';
PRINT '========================================';
GO
