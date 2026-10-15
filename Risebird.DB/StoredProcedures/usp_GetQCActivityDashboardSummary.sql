/*
================================================================================
usp_GetQCActivityDashboardSummary — QC Activity dashboard (/QCActivity).

Result set 1: one row per user (Quality Controllers, plus anyone else who
              acted when @OnlyQualityController = 0), aggregated from
              usp_QCActivity_CollectRows. Active Quality Controllers with no
              activity are listed too (TotalActions = 0).
Result set 2: "Open right now" backlog (not date-filtered).

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
        RefId             INT           NULL
    );

    EXEC dbo.usp_QCActivity_CollectRows
        @FromIst    = @FromIst,
        @ToIst      = @ToIst,
        @ActorEmail = @ActorEmail,
        @Metric     = NULL;

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
        MAX(r.ActionAtIst)                                                                   AS LastActionAtIst
    FROM #Users u
    LEFT JOIN #QCActivityRows r ON r.ActorEmail = u.Email
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
END
GO
