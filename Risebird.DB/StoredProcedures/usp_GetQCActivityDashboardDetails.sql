/*
================================================================================
usp_GetQCActivityDashboardDetails — QC Activity dashboard drill-down.

Lists the records behind one number on the dashboard: one user (@ActorEmail),
one metric key, one IST date range. Reads the same rows as the summary
(usp_QCActivity_CollectRows), newest first, top 2000.

Metric keys: ReportReviewed, PenaltyApplied, SlaApproved, SlaNotApproved,
QCForm, TrialKept, TrialRemoved, Trial, AuditBackfill, AuditKept,
AuditRemoved, AuditActions, TrmUnderReview, TrmReviewed, TrmRemoved, Trm,
TiUnderReview, TiReviewed, TiRemoved, Ti, PlivoCall, Coaching, ReportEdited,
Suspicious, SuspiciousUndone, Blocked, Unblocked, AccessRevoked,
AccessRestored, Inactivated, Reactivated, PaymentHold, PaymentHoldReleased,
All. Group keys return an action together with its undo: StatusActions (all
ten status metrics), SuspiciousAll, BlockedAll, AccessAll, ActiveAll,
PaymentHoldAll (the collector narrows the rows for these).
The web layer whitelists them; an unknown key returns no rows.

RefId is the ReportEditLog.Id for log-based ReportEdited rows (the page's
"View change" link) and NULL otherwise.

Part 2 (authentic QC work): atomic keys ProxyForm, RedoRequested, TrialBackfill;
group keys PageTotalInterviews, PageTrm, PageIrfAudit, PageTrial, PageSuspicious,
PageReportsSla (actions from that page), ScoredActions, AuthenticActions,
NotAuthentic, NoEvidence, ConnectedCall (answered calls of MinCallSeconds+),
Reached (connected calls + coaching), FollowThrough (actions on High-Risk
interviewers; FollowedUp = called or coached by anyone in the window).
Every row also returns Comment, HasEvidence, IsAuthentic and PageGroup.

Deploy after usp_QCActivity_CollectRows.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetQCActivityDashboardDetails]
    @ActorEmail VARCHAR(150),
    @Metric     VARCHAR(40),
    @FromIst    DATE,
    @ToIst      DATE
AS
BEGIN
    SET NOCOUNT ON;

    SET @ActorEmail = NULLIF(LTRIM(RTRIM(@ActorEmail)), '');
    SET @Metric     = NULLIF(LTRIM(RTRIM(@Metric)), '');

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

    -- FollowThrough needs the High-Risk list and calls / coaching by anyone.
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
    IF @Metric = 'FollowThrough' AND OBJECT_ID(N'dbo.usp_QCActivity_HighRisk', N'P') IS NOT NULL
        EXEC dbo.usp_QCActivity_HighRisk @FromIst = @FromIst, @ToIst = @ToIst;

    -- A drill-down is always for one user and one known metric.
    IF @ActorEmail IS NOT NULL AND @Metric IS NOT NULL
        EXEC dbo.usp_QCActivity_CollectRows
            @FromIst    = @FromIst,
            @ToIst      = @ToIst,
            @ActorEmail = @ActorEmail,
            @Metric     = @Metric;

    CREATE TABLE #Coached (Email VARCHAR(200) NOT NULL PRIMARY KEY);
    IF @Metric = 'FollowThrough' AND OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Coached (Email)
            SELECT DISTINCT LTRIM(RTRIM(s.InterviewerEmail))
            FROM dbo.InterviewerCoachingSession s
            WHERE NULLIF(LTRIM(RTRIM(s.InterviewerEmail)), '''') IS NOT NULL
              AND s.CreatedAtUtc >= DATEADD(MINUTE, -330, CAST(@FromIst AS DATETIME))
              AND s.CreatedAtUtc <  DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@ToIst AS DATETIME)))',
            N'@FromIst DATE, @ToIst DATE', @FromIst = @FromIst, @ToIst = @ToIst;

    SELECT TOP (2000)
        r.Metric,
        r.ActorEmail,
        r.ActionAtIst,
        r.InterviewId,
        r.IrfId,
        irf.IRFId                                              AS IrfName,
        COALESCE(byId.EmailId, r.InterviewerEmail)             AS InterviewerEmail,
        COALESCE(byId.FullName, byEmail.FullName)              AS InterviewerName,
        COALESCE(NULLIF(r.Phone, ''),
                 -- MobileNumber is text in production ('+91...'): no numeric comparison
                 NULLIF(NULLIF(LTRIM(RTRIM(CAST(ISNULL(byId.MobileNumber, byEmail.MobileNumber) AS NVARCHAR(200)))), N''), N'0')) AS Phone,
        r.Amount,
        r.Detail,
        r.[Status],
        r.RecordingUrl,
        r.SourceId,
        r.RefId,
        r.Comment,
        r.HasEvidence,
        r.IsAuthentic,
        r.PageGroup,
        CAST(CASE WHEN @Metric <> 'FollowThrough' THEN NULL
                  WHEN EXISTS (SELECT 1 FROM dbo.CallRecordingData c
                               WHERE c.CallRecipient = COALESCE(byId.EmailId, r.InterviewerEmail)
                                 AND c.DateCreated >= DATEADD(MINUTE, -330, CAST(@FromIst AS DATETIME))
                                 AND c.DateCreated <  DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@ToIst AS DATETIME))))
                    THEN 1
                  WHEN OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
                       AND EXISTS (SELECT 1 FROM #Coached cs WHERE cs.Email = COALESCE(byId.EmailId, r.InterviewerEmail))
                    THEN 1
                  ELSE 0 END AS BIT) AS FollowedUp
    FROM #QCActivityRows r
    LEFT JOIN dbo.IRF irf ON irf.id = r.IrfId
    OUTER APPLY (
        SELECT TOP 1
            ul.EmailId,
            ul.MobileNumber,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName
        FROM dbo.UserList ul
        WHERE r.InterviewerUserId IS NOT NULL AND ul.id = r.InterviewerUserId
    ) byId
    OUTER APPLY (
        SELECT TOP 1
            ul.id AS UserId,
            ul.MobileNumber,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName
        FROM dbo.UserList ul
        WHERE r.InterviewerUserId IS NULL
          AND r.InterviewerEmail IS NOT NULL
          AND ul.EmailId = r.InterviewerEmail
        ORDER BY ul.id DESC
    ) byEmail
    WHERE @Metric IN ('All', 'Trial', 'AuditActions', 'Trm', 'Ti',
                      'StatusActions', 'SuspiciousAll', 'BlockedAll', 'AccessAll', 'ActiveAll', 'PaymentHoldAll')
       OR r.Metric = @Metric
       OR (@Metric = 'PenaltyApplied' AND r.Metric = 'ReportReviewed' AND r.Amount > 0)
       -- Part 2 group keys
       OR (@Metric = 'PageTotalInterviews' AND r.PageGroup = 'TotalInterviews')
       OR (@Metric = 'PageTrm'             AND r.PageGroup = 'TRM')
       OR (@Metric = 'PageIrfAudit'        AND r.PageGroup = 'IrfAudit')
       OR (@Metric = 'PageTrial'           AND r.PageGroup = 'Trial')
       OR (@Metric = 'PageSuspicious'      AND r.PageGroup = 'Suspicious')
       OR (@Metric = 'PageReportsSla'      AND r.PageGroup = 'ReportsSla')
       OR (@Metric = 'ActionsTaken'        AND r.PageGroup IN ('TotalInterviews', 'TRM', 'IrfAudit', 'Trial', 'Suspicious', 'ReportsSla'))
       OR (@Metric = 'ScoredActions'       AND r.Metric <> 'PlivoCall' AND r.IsAuthentic IS NOT NULL)
       OR (@Metric = 'AuthenticActions'    AND r.Metric <> 'PlivoCall' AND r.IsAuthentic = 1)
       OR (@Metric = 'NotAuthentic'        AND r.Metric <> 'PlivoCall' AND r.IsAuthentic = 0)
       OR (@Metric = 'NoEvidence'          AND r.Metric <> 'PlivoCall' AND r.IsAuthentic IS NOT NULL AND ISNULL(r.HasEvidence, 0) = 0
                                           AND (r.InterviewerUserId IS NOT NULL OR r.InterviewerEmail IS NOT NULL))
       OR (@Metric = 'ConnectedCall'       AND r.Metric = 'PlivoCall' AND r.IsAuthentic = 1)
       OR (@Metric = 'Reached'             AND ((r.Metric = 'PlivoCall' AND r.IsAuthentic = 1) OR r.Metric = 'Coaching'))
       OR (@Metric = 'FollowThrough'       AND r.Metric <> 'PlivoCall' AND r.IsAuthentic IS NOT NULL
                                           AND EXISTS (SELECT 1 FROM #QCHighRisk hr
                                                       WHERE hr.InterviewerId = COALESCE(r.InterviewerUserId, byEmail.UserId)))
    ORDER BY r.ActionAtIst DESC, r.SourceId DESC;
END
GO
