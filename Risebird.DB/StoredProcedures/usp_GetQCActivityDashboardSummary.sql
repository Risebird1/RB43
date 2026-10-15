/*
================================================================================
usp_GetQCActivityDashboardSummary — QC Activity dashboard (/QCActivity).

Result set 1: one row per user (Quality Controllers, plus anyone else who
              acted when @OnlyQualityController = 0), aggregated from
              usp_QCActivity_CollectRows. Active Quality Controllers with no
              activity are listed too (TotalActions = 0).
Result set 2: "Open right now" backlog (not date-filtered).
Result set 3: high-risk coverage totals (HighRiskTotal, HighRiskTouched, CoveragePct).
Result set 4: High-Risk interviewers nobody touched in the window (top 50).

Part 2 (authentic QC work) per-person columns: ConnectedCalls, DialledCalls,
ConnectedTalkSeconds, InterviewersReached (connected calls + coaching), actions per
page (ActionsTotalInterviews, ActionsTrm, ActionsIrfAudit, ActionsTrial,
ActionsSuspicious, ActionsReportsSla), ProxyForms, RedoRequests, TrialBackfills,
ScoredActions, AuthenticActions, NoEvidenceActions, FollowThroughDone /
FollowThroughOf (High-Risk interviewers the person actioned who were also called or
coached by anyone in the window), FlagCount (usp_QCActivity_ComputeFlags).
The rows are collected for everyone first (follow-through and the flags need other
people's calls); @ActorEmail then narrows them before the existing aggregation.

@ActorEmail limits result set 1 to that single user (Quality Controller
viewing their own row).

Status changes and report edits: ReportsEdited, MarkedSuspicious /
SuspiciousUndone, Blocked / Unblocked, AccessRevoked / AccessRestored,
Inactivated / Reactivated, PaymentHolds / PaymentHoldsReleased. A removal
from Total Interviews also counts as Inactivated or Blocked (by design).

Deploy after usp_QCActivity_CollectRows.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetQCActivityDashboardSummary]
    @FromIst               DATE,
    @ToIst                 DATE,
    @OnlyQualityController BIT          = 1,
    @ActorEmail            VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @ActorEmail = NULLIF(LTRIM(RTRIM(@ActorEmail)), '');
    SET @OnlyQualityController = ISNULL(@OnlyQualityController, 1);

    CREATE TABLE #QCActivityRows (
        ActorEmail        VARCHAR(150)  NOT NULL,
        Metric            VARCHAR(30)   NOT NULL,
        ActionAtIst       DATETIME      NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Amount            INT           NULL,
        Detail            NVARCHAR(500) NULL,
        [Status]          VARCHAR(150)  NULL,
        Phone             VARCHAR(30)   NULL,
        RecordingUrl      VARCHAR(1000) NULL,
        SourceId          INT           NULL,
        RefId             INT           NULL,
        Comment           NVARCHAR(500) NULL,
        HasEvidence       BIT           NULL,
        IsAuthentic       BIT           NULL,
        PageGroup         VARCHAR(30)   NULL
    );

    -- Everyone's rows: follow-through and the flags count calls / reversals by other people.
    EXEC dbo.usp_QCActivity_CollectRows
        @FromIst    = @FromIst,
        @ToIst      = @ToIst,
        @ActorEmail = NULL,
        @Metric     = NULL;

    /* ---- Part 2: flags, High-Risk list, per-person authenticity ---- */
    CREATE TABLE #QCActivityFlags (
        Flag        VARCHAR(30)   NOT NULL,
        ActorEmail  VARCHAR(150)  NOT NULL,
        [Count]     INT           NOT NULL,
        FirstAt     DATETIME      NULL,
        LastAt      DATETIME      NULL,
        Detail      NVARCHAR(500) NULL,
        Metrics     VARCHAR(200)  NULL
    );
    CREATE TABLE #QCActivityFlagItems (
        Flag              VARCHAR(30)   NOT NULL,
        ActorEmail        VARCHAR(150)  NOT NULL,
        ActionAtIst       DATETIME      NULL,
        Metric            VARCHAR(30)   NOT NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Detail            NVARCHAR(500) NULL,
        SourceId          INT           NULL,
        Note              NVARCHAR(300) NULL
    );
    -- Part 2 extras never block the counts: on any error they are left empty (flags 0, no coverage).
    BEGIN TRY
        IF OBJECT_ID(N'dbo.usp_QCActivity_ComputeFlags', N'P') IS NOT NULL
            EXEC dbo.usp_QCActivity_ComputeFlags @FromIst = @FromIst, @ToIst = @ToIst;
    END TRY
    BEGIN CATCH
        DELETE FROM #QCActivityFlagItems;
        DELETE FROM #QCActivityFlags;
    END CATCH

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
    BEGIN TRY
        IF OBJECT_ID(N'dbo.usp_QCActivity_HighRisk', N'P') IS NOT NULL
            EXEC dbo.usp_QCActivity_HighRisk @FromIst = @FromIst, @ToIst = @ToIst;
    END TRY
    BEGIN CATCH
        DELETE FROM #QCHighRisk;
    END CATCH

    -- Interviewer key per row (UserList.id), used for reach and follow-through.
    SELECT r.ActorEmail, r.Metric, r.IsAuthentic, r.HasEvidence, r.PageGroup, r.Amount,
           COALESCE(r.InterviewerUserId, u.id) AS IvUserId
    INTO #QCK
    FROM #QCActivityRows r
    OUTER APPLY (SELECT TOP 1 x.id FROM dbo.UserList x
                 WHERE r.InterviewerUserId IS NULL AND x.EmailId = LTRIM(RTRIM(r.InterviewerEmail)) ORDER BY x.id DESC) u;

    SELECT
        k.ActorEmail AS Email,
        SUM(CASE WHEN k.Metric = 'PlivoCall' THEN 1 ELSE 0 END)                                         AS DialledCalls,
        SUM(CASE WHEN k.Metric = 'PlivoCall' AND k.IsAuthentic = 1 THEN 1 ELSE 0 END)                   AS ConnectedCalls,
        SUM(CASE WHEN k.Metric = 'PlivoCall' AND k.IsAuthentic = 1 THEN ISNULL(k.Amount, 0) ELSE 0 END) AS ConnectedTalkSeconds,
        COUNT(DISTINCT CASE WHEN (k.Metric = 'PlivoCall' AND k.IsAuthentic = 1) OR k.Metric = 'Coaching'
                            THEN k.IvUserId END)                                                     AS InterviewersReached,
        SUM(CASE WHEN k.PageGroup = 'TotalInterviews' THEN 1 ELSE 0 END)                               AS ActionsTotalInterviews,
        SUM(CASE WHEN k.PageGroup = 'TRM' THEN 1 ELSE 0 END)                                           AS ActionsTrm,
        SUM(CASE WHEN k.PageGroup = 'IrfAudit' THEN 1 ELSE 0 END)                                      AS ActionsIrfAudit,
        SUM(CASE WHEN k.PageGroup = 'Trial' THEN 1 ELSE 0 END)                                         AS ActionsTrial,
        SUM(CASE WHEN k.PageGroup = 'Suspicious' THEN 1 ELSE 0 END)                                    AS ActionsSuspicious,
        SUM(CASE WHEN k.PageGroup = 'ReportsSla' THEN 1 ELSE 0 END)                                    AS ActionsReportsSla,
        SUM(CASE WHEN k.Metric = 'ProxyForm' THEN 1 ELSE 0 END)                                        AS ProxyForms,
        SUM(CASE WHEN k.Metric = 'RedoRequested' THEN 1 ELSE 0 END)                                    AS RedoRequests,
        SUM(CASE WHEN k.Metric = 'TrialBackfill' THEN 1 ELSE 0 END)                                    AS TrialBackfills,
        SUM(CASE WHEN k.Metric <> 'PlivoCall' AND k.IsAuthentic IS NOT NULL THEN 1 ELSE 0 END)         AS ScoredActions,
        SUM(CASE WHEN k.Metric <> 'PlivoCall' AND k.IsAuthentic = 1 THEN 1 ELSE 0 END)                 AS AuthenticActions,
        SUM(CASE WHEN k.Metric <> 'PlivoCall' AND k.IsAuthentic IS NOT NULL AND ISNULL(k.HasEvidence, 0) = 0
                  AND k.IvUserId IS NOT NULL THEN 1 ELSE 0 END)                                          AS NoEvidenceActions
    INTO #PX
    FROM #QCK k
    GROUP BY k.ActorEmail;

    -- Follow-through: High-Risk interviewers this person actioned, and of those, how many were
    -- called (connected) or coached by anyone in the window.
    SELECT a.ActorEmail AS Email,
           COUNT(DISTINCT a.IvUserId) AS FollowThroughOf,
           COUNT(DISTINCT CASE WHEN c.IvUserId IS NOT NULL THEN a.IvUserId END) AS FollowThroughDone
    INTO #FT
    FROM #QCK a
    INNER JOIN #QCHighRisk hr ON hr.InterviewerId = a.IvUserId
    LEFT JOIN (SELECT DISTINCT IvUserId FROM #QCK
               WHERE IvUserId IS NOT NULL AND (Metric = 'Coaching' OR (Metric = 'PlivoCall' AND IsAuthentic = 1))) c
           ON c.IvUserId = a.IvUserId
    WHERE a.IsAuthentic IS NOT NULL AND a.Metric <> 'PlivoCall'
    GROUP BY a.ActorEmail;

    SELECT ActorEmail AS Email, COUNT(1) AS FlagCount INTO #FC FROM #QCActivityFlags GROUP BY ActorEmail;

    -- A Quality Controller viewing their own row: narrow to their rows before the existing aggregation.
    IF @ActorEmail IS NOT NULL
        DELETE FROM #QCActivityRows WHERE ActorEmail <> @ActorEmail;

    CREATE TABLE #Users (
        Email       VARCHAR(150) NOT NULL PRIMARY KEY,
        FullName    VARCHAR(310) NULL,
        ProfileName VARCHAR(150) NULL,
        IsActive    BIT          NULL
    );

    -- Active Quality Controllers (so users with no activity still show).
    INSERT INTO #Users (Email, FullName, ProfileName, IsActive)
    SELECT x.Email, x.FullName, x.ProfileName, x.IsActive
    FROM (
        SELECT
            LTRIM(RTRIM(ul.EmailId)) AS Email,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName,
            p.[Name] AS ProfileName,
            ul.IsActive,
            ROW_NUMBER() OVER (PARTITION BY LTRIM(RTRIM(ul.EmailId)) ORDER BY ISNULL(ul.IsActive, 0) DESC, ul.id DESC) AS rn
        FROM dbo.UserList ul
        INNER JOIN dbo.[Profile] p ON p.id = ul.idProfile
        WHERE p.[Name] = 'Quality Controller'
          AND ISNULL(ul.IsActive, 1) = 1
          AND NULLIF(LTRIM(RTRIM(ul.EmailId)), '') IS NOT NULL
          AND (@ActorEmail IS NULL OR ul.EmailId = @ActorEmail)
    ) x
    WHERE x.rn = 1;

    -- Everyone else who acted in the range (inactive QCs, or any profile when the filter is off).
    INSERT INTO #Users (Email, FullName, ProfileName, IsActive)
    SELECT a.ActorEmail, u.FullName, u.ProfileName, u.IsActive
    FROM (SELECT DISTINCT ActorEmail FROM #QCActivityRows) a
    OUTER APPLY (
        SELECT TOP 1
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName,
            p.[Name] AS ProfileName,
            ul.IsActive
        FROM dbo.UserList ul
        LEFT JOIN dbo.[Profile] p ON p.id = ul.idProfile
        WHERE ul.EmailId = a.ActorEmail
        ORDER BY ISNULL(ul.IsActive, 0) DESC, ul.id DESC
    ) u
    WHERE NOT EXISTS (SELECT 1 FROM #Users x WHERE x.Email = a.ActorEmail)
      AND (@OnlyQualityController = 0 OR u.ProfileName = 'Quality Controller');

    /* Result set 1: per-user aggregates */
    SELECT
        u.Email                                       AS ActorEmail,
        ISNULL(u.FullName, u.Email)                   AS ActorName,
        ISNULL(u.ProfileName, 'Unknown')              AS ProfileName,
        CAST(ISNULL(u.IsActive, 0) AS BIT)            AS IsActive,

        SUM(CASE WHEN r.Metric = 'ReportReviewed' THEN 1 ELSE 0 END)                         AS ReportsReviewed,
        SUM(CASE WHEN r.Metric = 'ReportReviewed' AND r.Amount > 0 THEN 1 ELSE 0 END)        AS PenaltiesApplied,
        CAST(AVG(CASE WHEN r.Metric = 'ReportReviewed' AND r.Amount > 0
                      THEN CAST(r.Amount AS DECIMAL(9, 2)) END) AS DECIMAL(9, 2))            AS AvgPenaltyPercent,
        SUM(CASE WHEN r.Metric = 'SlaApproved' THEN 1 ELSE 0 END)                            AS SlaApproved,
        SUM(CASE WHEN r.Metric = 'SlaNotApproved' THEN 1 ELSE 0 END)                         AS SlaNotApproved,
        SUM(CASE WHEN r.Metric = 'QCForm' THEN 1 ELSE 0 END)                                 AS QcForms,
        SUM(CASE WHEN r.Metric = 'QCForm' THEN ISNULL(r.Amount, 0) ELSE 0 END)               AS QcFormRedFlags,

        SUM(CASE WHEN r.Metric = 'TrialKept' THEN 1 ELSE 0 END)                              AS TrialKept,
        SUM(CASE WHEN r.Metric = 'TrialRemoved' THEN 1 ELSE 0 END)                           AS TrialRemoved,

        COUNT(DISTINCT CASE WHEN r.Metric IN ('AuditBackfill', 'AuditKept', 'AuditRemoved') THEN r.IrfId END) AS AuditIrfs,
        SUM(CASE WHEN r.Metric = 'AuditBackfill' THEN 1 ELSE 0 END)                          AS AuditBackfills,
        SUM(CASE WHEN r.Metric = 'AuditKept' THEN 1 ELSE 0 END)                              AS AuditKept,
        SUM(CASE WHEN r.Metric = 'AuditRemoved' THEN 1 ELSE 0 END)                           AS AuditRemoved,

        SUM(CASE WHEN r.Metric = 'TrmUnderReview' THEN 1 ELSE 0 END)                         AS TrmUnderReview,
        SUM(CASE WHEN r.Metric = 'TrmReviewed' THEN 1 ELSE 0 END)                            AS TrmReviewed,
        SUM(CASE WHEN r.Metric = 'TrmRemoved' THEN 1 ELSE 0 END)                             AS TrmRemoved,

        SUM(CASE WHEN r.Metric = 'TiUnderReview' THEN 1 ELSE 0 END)                          AS TiUnderReview,
        SUM(CASE WHEN r.Metric = 'TiReviewed' THEN 1 ELSE 0 END)                             AS TiReviewed,
        SUM(CASE WHEN r.Metric = 'TiRemoved' THEN 1 ELSE 0 END)                              AS TiRemoved,

        SUM(CASE WHEN r.Metric = 'PlivoCall' THEN 1 ELSE 0 END)                              AS PlivoCalls,
        COUNT(DISTINCT CASE WHEN r.Metric = 'PlivoCall' THEN r.InterviewerEmail END)         AS PlivoInterviewers,
        SUM(CASE WHEN r.Metric = 'PlivoCall' THEN ISNULL(r.Amount, 0) ELSE 0 END)            AS PlivoTalkSeconds,
        SUM(CASE WHEN r.Metric = 'PlivoCall' AND r.Amount IS NOT NULL THEN 1 ELSE 0 END)     AS PlivoCallsWithDuration,
        SUM(CASE WHEN r.Metric = 'Coaching' THEN 1 ELSE 0 END)                               AS CoachingSessions,

        SUM(CASE WHEN r.Metric = 'ReportEdited' THEN 1 ELSE 0 END)                           AS ReportsEdited,
        SUM(CASE WHEN r.Metric = 'Suspicious' THEN 1 ELSE 0 END)                             AS MarkedSuspicious,
        SUM(CASE WHEN r.Metric = 'SuspiciousUndone' THEN 1 ELSE 0 END)                       AS SuspiciousUndone,
        SUM(CASE WHEN r.Metric = 'Blocked' THEN 1 ELSE 0 END)                                AS Blocked,
        SUM(CASE WHEN r.Metric = 'Unblocked' THEN 1 ELSE 0 END)                              AS Unblocked,
        SUM(CASE WHEN r.Metric = 'AccessRevoked' THEN 1 ELSE 0 END)                          AS AccessRevoked,
        SUM(CASE WHEN r.Metric = 'AccessRestored' THEN 1 ELSE 0 END)                         AS AccessRestored,
        SUM(CASE WHEN r.Metric = 'Inactivated' THEN 1 ELSE 0 END)                            AS Inactivated,
        SUM(CASE WHEN r.Metric = 'Reactivated' THEN 1 ELSE 0 END)                            AS Reactivated,
        SUM(CASE WHEN r.Metric = 'PaymentHold' THEN 1 ELSE 0 END)                            AS PaymentHolds,
        SUM(CASE WHEN r.Metric = 'PaymentHoldReleased' THEN 1 ELSE 0 END)                    AS PaymentHoldsReleased,
        SUM(CASE WHEN r.Metric = 'DeductionReview' THEN 1 ELSE 0 END)                        AS DeductionReviews,

        COUNT(r.Metric)                                                                      AS TotalActions,
        MAX(r.ActionAtIst)                                                                   AS LastActionAtIst,

        -- Part 2: authentic QC work
        ISNULL(MAX(px.ConnectedCalls), 0)          AS ConnectedCalls,
        ISNULL(MAX(px.DialledCalls), 0)            AS DialledCalls,
        ISNULL(MAX(px.ConnectedTalkSeconds), 0)    AS ConnectedTalkSeconds,
        ISNULL(MAX(px.InterviewersReached), 0)     AS InterviewersReached,
        ISNULL(MAX(px.ActionsTotalInterviews), 0)  AS ActionsTotalInterviews,
        ISNULL(MAX(px.ActionsTrm), 0)              AS ActionsTrm,
        ISNULL(MAX(px.ActionsIrfAudit), 0)         AS ActionsIrfAudit,
        ISNULL(MAX(px.ActionsTrial), 0)            AS ActionsTrial,
        ISNULL(MAX(px.ActionsSuspicious), 0)       AS ActionsSuspicious,
        ISNULL(MAX(px.ActionsReportsSla), 0)       AS ActionsReportsSla,
        ISNULL(MAX(px.ProxyForms), 0)              AS ProxyForms,
        ISNULL(MAX(px.RedoRequests), 0)            AS RedoRequests,
        ISNULL(MAX(px.TrialBackfills), 0)          AS TrialBackfills,
        ISNULL(MAX(px.ScoredActions), 0)           AS ScoredActions,
        ISNULL(MAX(px.AuthenticActions), 0)        AS AuthenticActions,
        ISNULL(MAX(px.NoEvidenceActions), 0)       AS NoEvidenceActions,
        ISNULL(MAX(ft.FollowThroughDone), 0)       AS FollowThroughDone,
        ISNULL(MAX(ft.FollowThroughOf), 0)         AS FollowThroughOf,
        ISNULL(MAX(fc.FlagCount), 0)               AS FlagCount
    FROM #Users u
    LEFT JOIN #QCActivityRows r ON r.ActorEmail = u.Email
    LEFT JOIN #PX px ON px.Email = u.Email
    LEFT JOIN #FT ft ON ft.Email = u.Email
    LEFT JOIN #FC fc ON fc.Email = u.Email
    GROUP BY u.Email, u.FullName, u.ProfileName, u.IsActive
    ORDER BY
        CASE WHEN u.ProfileName = 'Quality Controller' THEN 0 ELSE 1 END,
        ISNULL(u.ProfileName, 'Unknown'),
        COUNT(r.Metric) DESC,
        ISNULL(u.FullName, u.Email);

    /* Result set 2: open right now (not date-filtered) */
    DECLARE @PendingReports INT = (
        SELECT COUNT(*) FROM dbo.PaymentSLADetails WHERE ISNULL(IsApproved, 0) = 0);

    DECLARE @PendingTrialFlags INT = 0;
    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
        EXEC sp_executesql
            N'SELECT @n = COUNT(*) FROM dbo.TrialFlags WHERE LOWER(LTRIM(RTRIM(ReviewStatus))) = ''pending''',
            N'@n INT OUTPUT', @n = @PendingTrialFlags OUTPUT;

    DECLARE @PendingAuditInterviewers INT = 0;
    IF COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL
        EXEC sp_executesql
            N'SELECT @n = COUNT(DISTINCT UserId) FROM dbo.TransInterviewer
              WHERE IsActive = 1 AND NULLIF(LTRIM(RTRIM(ISNULL(AutoQcVerdict, ''''))), '''') IS NOT NULL',
            N'@n INT OUTPUT', @n = @PendingAuditInterviewers OUTPUT;

    DECLARE @TrmUnderReview INT = (
        SELECT COUNT(DISTINCT InterviewerId) FROM dbo.TransInterviewerRedMarker WHERE RedMarkerStatus = 1);

    DECLARE @PendingDeductionReviews INT = 0;
    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL
        EXEC sp_executesql
            N'SELECT @n = COUNT(*) FROM dbo.DeductionReviewRequest WHERE Status = ''Open''',
            N'@n INT OUTPUT', @n = @PendingDeductionReviews OUTPUT;

    SELECT
        ISNULL(@PendingReports, 0)           AS PendingReports,
        ISNULL(@PendingTrialFlags, 0)        AS PendingTrialFlags,
        ISNULL(@PendingAuditInterviewers, 0) AS PendingAuditInterviewers,
        ISNULL(@TrmUnderReview, 0)           AS TrmUnderReview,
        ISNULL(@PendingDeductionReviews, 0)  AS PendingDeductionReviews;

    /* Result set 3: high-risk coverage (page level, everyone, the window) */
    SELECT
        (SELECT COUNT(1) FROM #QCHighRisk) AS HighRiskTotal,
        (SELECT COUNT(1) FROM #QCHighRisk WHERE Touched = 1) AS HighRiskTouched,
        CAST(CASE WHEN (SELECT COUNT(1) FROM #QCHighRisk) = 0 THEN NULL
                  ELSE 100.0 * (SELECT COUNT(1) FROM #QCHighRisk WHERE Touched = 1) / (SELECT COUNT(1) FROM #QCHighRisk) END
             AS DECIMAL(6,1)) AS CoveragePct;

    /* Result set 4: High-Risk interviewers nobody touched (top 50, worst first) */
    SELECT TOP (50)
        h.InterviewerId, h.CodedId, h.InterviewerName, h.InterviewerEmail, h.OverallRiskRatio,
        h.WorstSeverity, h.PriorityTier, h.TotalCompleted, h.TotalBad
    FROM #QCHighRisk h
    WHERE h.Touched = 0
    ORDER BY CASE h.WorstSeverity WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2 WHEN 'P3' THEN 3
                                  WHEN 'P4' THEN 4 WHEN 'P5' THEN 5 ELSE 9 END,
             ISNULL(h.OverallRiskRatio, 0) DESC, h.InterviewerId;
END
GO
