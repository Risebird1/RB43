/*
================================================================================
usp_QCActivity_HighRisk — the High-Risk interviewers for an IST window, for QC
Activity coverage and follow-through.

The list is exactly the High-Risk Interviewers tab's default list (IRF Audit):
usp_GetInterviewerRiskOverview with its default filter (3+ completed interviews
and pooled bad ratio >= 25% or any P0), all channels, @IdsOnly = 1.

Fills the CALLER's temp table (create it first):

    CREATE TABLE #QCHighRisk (
        InterviewerId    INT           NOT NULL PRIMARY KEY,
        CodedId          NVARCHAR(200) NULL,
        InterviewerName  NVARCHAR(310) NULL,
        InterviewerEmail VARCHAR(200)  NULL,
        OverallRiskRatio DECIMAL(10,4) NULL,
        WorstSeverity    VARCHAR(4)    NULL,
        PriorityTier     VARCHAR(20)   NULL,
        TotalCompleted   INT           NULL,
        TotalBad         INT           NULL,
        Touched          BIT           NOT NULL DEFAULT 0
    );

Touched = 1 when #QCActivityRows (if the caller has it) holds any call, coaching
session or action about the interviewer, by anyone, in the window (backfill runs
are per IRF and never count).

Must not be called from inside an INSERT ... EXEC (it uses one itself).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_QCActivity_HighRisk]
    @FromIst DATE,
    @ToIst   DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'tempdb..#QCHighRisk') IS NULL
        RETURN;
    IF OBJECT_ID(N'dbo.usp_GetInterviewerRiskOverview', N'P') IS NULL
        RETURN;

    CREATE TABLE #HrList (
        InterviewerId    INT           NOT NULL,
        CodedId          NVARCHAR(200) NULL,
        InterviewerName  NVARCHAR(310) NULL,
        InterviewerEmail VARCHAR(200)  NULL,
        OverallRiskRatio DECIMAL(10,4) NULL,
        WorstSeverity    VARCHAR(4)    NULL,
        PriorityTier     VARCHAR(20)   NULL,
        TotalCompleted   INT           NULL,
        TotalBad         INT           NULL
    );

    INSERT INTO #HrList (InterviewerId, CodedId, InterviewerName, InterviewerEmail, OverallRiskRatio,
                         WorstSeverity, PriorityTier, TotalCompleted, TotalBad)
    EXEC dbo.usp_GetInterviewerRiskOverview
        @FromIst = @FromIst,
        @ToIst = @ToIst,
        @OwnerEmail = NULL,
        @PendingReviewOnly = 0,
        @IdsOnly = 1;

    INSERT INTO #QCHighRisk (InterviewerId, CodedId, InterviewerName, InterviewerEmail, OverallRiskRatio,
                             WorstSeverity, PriorityTier, TotalCompleted, TotalBad, Touched)
    SELECT h.InterviewerId, MAX(h.CodedId), MAX(h.InterviewerName), MAX(LTRIM(RTRIM(h.InterviewerEmail))),
           MAX(h.OverallRiskRatio), MAX(h.WorstSeverity), MAX(h.PriorityTier), MAX(h.TotalCompleted), MAX(h.TotalBad), 0
    FROM #HrList h
    WHERE NOT EXISTS (SELECT 1 FROM #QCHighRisk x WHERE x.InterviewerId = h.InterviewerId)
    GROUP BY h.InterviewerId;

    IF OBJECT_ID(N'tempdb..#QCActivityRows') IS NOT NULL
    BEGIN
        -- Distinct touched ids / emails first, so each High-Risk interviewer is a seek, not a scan of every row.
        CREATE TABLE #TouchedId (UserId INT NOT NULL PRIMARY KEY);
        CREATE TABLE #TouchedEmail (Email VARCHAR(200) NOT NULL PRIMARY KEY);

        INSERT INTO #TouchedId (UserId)
        SELECT DISTINCT r.InterviewerUserId
        FROM #QCActivityRows r
        WHERE r.Metric NOT IN ('AuditBackfill', 'TrialBackfill') AND r.InterviewerUserId IS NOT NULL;

        INSERT INTO #TouchedEmail (Email)
        SELECT DISTINCT LTRIM(RTRIM(r.InterviewerEmail))
        FROM #QCActivityRows r
        WHERE r.Metric NOT IN ('AuditBackfill', 'TrialBackfill') AND r.InterviewerUserId IS NULL
          AND r.InterviewerEmail IS NOT NULL;

        UPDATE hr SET Touched = 1
        FROM #QCHighRisk hr
        WHERE EXISTS (SELECT 1 FROM #TouchedId t WHERE t.UserId = hr.InterviewerId)
           OR (hr.InterviewerEmail <> '' AND EXISTS (SELECT 1 FROM #TouchedEmail e WHERE e.Email = hr.InterviewerEmail));

        DROP TABLE IF EXISTS #TouchedEmail;
        DROP TABLE IF EXISTS #TouchedId;
    END

    DROP TABLE IF EXISTS #HrList;
END
GO
