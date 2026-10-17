/*
================================================================================
usp_GetInterviewerQcCoverage — "has QC dealt with this person?" for the
High-Risk Interviewers rows (QC Tracking, Delta 2). One row per requested
interviewer (UserList.id).

  LastTouch*            latest human QC touch in the last @Days days: Plivo call
                        to the interviewer, coaching session, RedMarkerStatusDetails
                        status move (any page), InterviewerActionLog action,
                        suspicious mark, IRF audit / Monitoring Keep / Remove
                        (TransInterviewerAuditLog), trial Keep / Remove. Actors
                        AISystem / System / Admin are excluded. Label names the
                        action and its page ("High-Risk: under review").
  LastConnectedCall*    latest call to the interviewer with talk time of at least
                        QCActivitySetting.MinCallSeconds (default 120), answered.
  Touches30d            touches (same sources) in the last 30 days.
  Open items            TrialFlags pending; TransInterviewerRedMarker under review;
                        PaymentSLADetails awaiting a decision on interviews in the
                        last 30 days; DeductionReviewRequest Open; active
                        suspicious marks.

@UserIds  comma-separated UserList ids (the page sends at most 200).
@Days     look-back for the last touch / connected call (default 90).

Clocks: RedMarkerStatusDetails.CreatedOn is IST (shifted to UTC here); every
other source is UTC / server clock. Optional tables (InterviewerCoachingSession,
InterviewerActionLog, TransInterviewerAuditLog, TrialFlags,
DeductionReviewRequest) are read through sp_executesql behind OBJECT_ID checks.
Read-only. usp_GetInterviewerRiskOverview is not touched.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerQcCoverage]
    @UserIds NVARCHAR(MAX),
    @Days    INT = 90
AS
BEGIN
    SET NOCOUNT ON;

    IF @Days IS NULL OR @Days < 1 SET @Days = 90;
    IF @Days > 365 SET @Days = 365;

    DECLARE @NowUtc   DATETIME = GETUTCDATE();
    DECLARE @SinceUtc DATETIME = DATEADD(DAY, -@Days, @NowUtc);
    DECLARE @Since30  DATETIME = DATEADD(DAY, -30, @NowUtc);

    DECLARE @MinCallSeconds INT = 120;
    IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT @MinCall = ISNULL(MAX(CASE WHEN Name = ''MinCallSeconds'' THEN TRY_CAST(Value AS INT) END), @MinCall)
            FROM dbo.QCActivitySetting',
            N'@MinCall INT OUTPUT', @MinCall = @MinCallSeconds OUTPUT;

    CREATE TABLE #Iv (
        UserId     INT          NOT NULL PRIMARY KEY,
        Email      VARCHAR(200) NULL,
        ProfileId  INT          NULL
    );

    INSERT INTO #Iv (UserId)
    SELECT DISTINCT TRY_CAST(LTRIM(RTRIM(value)) AS INT)
    FROM STRING_SPLIT(ISNULL(@UserIds, N''), N',')
    WHERE TRY_CAST(LTRIM(RTRIM(value)) AS INT) > 0;

    UPDATE v SET Email = LTRIM(RTRIM(ul.EmailId))
    FROM #Iv v INNER JOIN dbo.UserList ul ON ul.id = v.UserId;

    UPDATE v SET ProfileId = (SELECT TOP 1 ip.Id FROM dbo.InterviewerProfile ip WHERE ip.UserId = v.UserId ORDER BY ip.Id DESC)
    FROM #Iv v;

    CREATE INDEX IX_Iv_Email ON #Iv (Email);

    CREATE TABLE #Touch (
        UserId INT           NOT NULL,
        AtUtc  DATETIME      NOT NULL,
        Label  NVARCHAR(150) NULL,
        Actor  VARCHAR(200)  NULL
    );

    -- Plivo / Twilio calls to the interviewer (the caller is the actor)
    INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
    SELECT v.UserId, c.DateCreated,
           N'Call' + CASE WHEN TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '') AS INT) IS NOT NULL
                          THEN N' (' + CAST(TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '') AS INT) AS NVARCHAR(20)) + N's)' ELSE N'' END,
           LTRIM(RTRIM(c.Caller))
    FROM #Iv v
    INNER JOIN dbo.CallRecordingData c ON c.CallRecipient = v.Email
    WHERE c.DateCreated >= @SinceUtc
      AND NULLIF(LTRIM(RTRIM(c.Caller)), '') IS NOT NULL
      AND LTRIM(RTRIM(c.Caller)) NOT IN ('AISystem', 'System', 'Admin');

    -- Status moves on any page (CreatedOn is IST)
    INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
    SELECT v.UserId, DATEADD(MINUTE, -330, r.CreatedOn),
           CASE r.[Type]
               WHEN 'IRF'            THEN N'Total Red Marker'
               WHEN 'TotalInterview' THEN N'Total Interviews'
               WHEN 'PotentialRisk'  THEN N'Potential Risk'
               WHEN 'HighRisk'       THEN N'High-Risk'
               WHEN 'PLI'            THEN N'PLI'
               WHEN 'NegativeAIVerdict' THEN N'Negative AI verdict'
               ELSE ISNULL(CAST(r.[Type] AS NVARCHAR(30)), N'Status') END
           + N': ' + CASE r.[Status] WHEN 1 THEN N'under review' WHEN 2 THEN N'reviewed' WHEN 3 THEN N'removed' ELSE N'status changed' END,
           LTRIM(RTRIM(r.CreatedBy))
    FROM #Iv v
    INNER JOIN dbo.RedMarkerStatusDetails r ON r.InterviewerUserid = v.UserId
    WHERE r.CreatedOn >= DATEADD(MINUTE, 330, @SinceUtc)
      AND NULLIF(LTRIM(RTRIM(r.CreatedBy)), '') IS NOT NULL
      AND LTRIM(RTRIM(r.CreatedBy)) NOT IN ('AISystem', 'System', 'Admin');

    -- Suspicious marks (Createddate is the app server clock, treated as UTC like QC Activity)
    INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
    SELECT v.UserId, s.Createddate, N'Marked suspicious', LTRIM(RTRIM(s.CreatedBy))
    FROM #Iv v
    INNER JOIN dbo.SuspiciousInterviewer s ON s.UserId = v.UserId
    WHERE s.Createddate >= @SinceUtc
      AND NULLIF(LTRIM(RTRIM(s.CreatedBy)), '') IS NOT NULL
      AND LTRIM(RTRIM(s.CreatedBy)) NOT IN ('AISystem', 'System', 'Admin');

    IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
            SELECT v.UserId, s.CreatedAtUtc, N''Coaching ('' + s.Outcome + N'')'', LTRIM(RTRIM(s.CoachEmail))
            FROM #Iv v
            INNER JOIN dbo.InterviewerCoachingSession s ON s.InterviewerUserId = v.UserId
            WHERE s.CreatedAtUtc >= @SinceUtc
              AND LTRIM(RTRIM(s.CoachEmail)) NOT IN (''AISystem'', ''System'', ''Admin'');',
            N'@SinceUtc DATETIME', @SinceUtc = @SinceUtc;

    IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
            SELECT v.UserId, l.ActedAtUtc,
                   CASE l.ActionType
                       WHEN ''SuspiciousUndone''    THEN N''Suspicious mark undone''
                       WHEN ''Inactivated''         THEN N''Made inactive''
                       WHEN ''Reactivated''         THEN N''Made active''
                       WHEN ''PaymentHold''         THEN N''Payment put on hold''
                       WHEN ''PaymentHoldReleased'' THEN N''Payment hold released''
                       WHEN ''AccessRevoked''       THEN N''Portal access turned off''
                       WHEN ''AccessRestored''      THEN N''Portal access turned on''
                       WHEN ''RedoRequested''       THEN N''Redo requested''
                       WHEN ''ProxyForm''           THEN N''Proxy form saved''
                       WHEN ''DeductionReview''     THEN N''Deduction review decided''
                       WHEN ''PotentialRiskReview'' THEN N''Report reviewed''
                       WHEN ''HighRiskReview''      THEN N''Report reviewed''
                       WHEN ''MonitoringRemoved''   THEN N''Removed from channel''
                       WHEN ''MonitoringShortlisted'' THEN N''Shortlisted on channel''
                       ELSE CAST(l.ActionType AS NVARCHAR(40)) END
                   + CASE WHEN l.Source LIKE ''%:PotentialRisk'' THEN N'' · Potential Risk''
                          WHEN l.Source LIKE ''%:HighRisk''      THEN N'' · High-Risk''
                          WHEN l.Source LIKE ''%:PLI''           THEN N'' · PLI''
                          WHEN l.Source LIKE ''%:Monitoring''    THEN N'' · Monitoring''
                          ELSE N'''' END,
                   LTRIM(RTRIM(l.ActedBy))
            FROM #Iv v
            INNER JOIN dbo.InterviewerActionLog l
                    ON l.InterviewerUserId = v.UserId
                    OR (l.InterviewerUserId IS NULL AND l.InterviewerEmail = v.Email)
            WHERE l.ActedAtUtc >= @SinceUtc
              AND NULLIF(LTRIM(RTRIM(l.ActedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(l.ActedBy)) NOT IN (''AISystem'', ''System'', ''Admin'');',
            N'@SinceUtc DATETIME', @SinceUtc = @SinceUtc;

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
            SELECT v.UserId, l.ChangedAt,
                   CASE l.EventType
                       WHEN ''RiskAuditKept''         THEN N''IRF audit: kept''
                       WHEN ''RiskAuditRemoval''      THEN N''IRF audit: removed''
                       WHEN ''MonitoringRemoved''     THEN N''Monitoring: removed from channel''
                       ELSE N''Monitoring: shortlisted'' END
                   + CASE WHEN LEFT(LTRIM(ISNULL(l.ReasonCategory, N'''')), 11) = N''[High-Risk]'' THEN N'' · High-Risk'' ELSE N'''' END,
                   LTRIM(RTRIM(l.ChangedBy))
            FROM #Iv v
            INNER JOIN dbo.TransInterviewer t ON t.UserId = v.UserId
            INNER JOIN dbo.TransInterviewerAuditLog l ON l.TransInterviewerId = t.id
            WHERE l.EventType IN (''RiskAuditKept'', ''RiskAuditRemoval'', ''MonitoringRemoved'', ''MonitoringShortlisted'')
              AND l.ChangedAt >= @SinceUtc
              AND NULLIF(LTRIM(RTRIM(l.ChangedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(l.ChangedBy)) NOT IN (''AISystem'', ''System'', ''Admin'');',
            N'@SinceUtc DATETIME', @SinceUtc = @SinceUtc;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Touch (UserId, AtUtc, Label, Actor)
            SELECT v.UserId, tf.ReviewedAtUtc,
                   CASE WHEN LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''kept'' THEN N''Trial: kept'' ELSE N''Trial: removed'' END,
                   LTRIM(RTRIM(tf.ReviewedBy))
            FROM #Iv v
            INNER JOIN dbo.Interviewer iv ON iv.UserId = v.UserId
            INNER JOIN dbo.TrialFlags tf ON tf.InterviewerId = iv.id
            WHERE LOWER(LTRIM(RTRIM(tf.ReviewStatus))) IN (''kept'', ''removed'')
              AND tf.ReviewedAtUtc >= @SinceUtc
              AND NULLIF(LTRIM(RTRIM(tf.ReviewedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(tf.ReviewedBy)) NOT IN (''AISystem'', ''System'', ''Admin'');',
            N'@SinceUtc DATETIME', @SinceUtc = @SinceUtc;

    -- Open items
    CREATE TABLE #Open (
        UserId               INT NOT NULL PRIMARY KEY,
        OpenTrialFlags       INT NOT NULL DEFAULT 0,
        OpenTrmUnderReview   INT NOT NULL DEFAULT 0,
        SlaPending           INT NOT NULL DEFAULT 0,
        OpenDeductionReviews INT NOT NULL DEFAULT 0,
        ActiveSuspicious     INT NOT NULL DEFAULT 0
    );
    INSERT INTO #Open (UserId) SELECT UserId FROM #Iv;

    UPDATE o SET OpenTrmUnderReview = x.n
    FROM #Open o
    INNER JOIN #Iv v ON v.UserId = o.UserId
    CROSS APPLY (SELECT COUNT(1) AS n FROM dbo.TransInterviewerRedMarker trm
                 INNER JOIN dbo.InterviewerProfile ip ON ip.Id = trm.InterviewerId
                 WHERE ip.UserId = v.UserId AND trm.RedMarkerStatus = 1) x;

    -- Payment SLA rows awaiting a decision on this interviewer's interviews in the last 30 days.
    UPDATE o SET SlaPending = x.n
    FROM #Open o
    INNER JOIN #Iv v ON v.UserId = o.UserId
    CROSS APPLY (SELECT COUNT(DISTINCT ps.Interviewid) AS n
                 FROM dbo.InterviewMeetings im
                 INNER JOIN dbo.PaymentSLADetails ps ON ps.Interviewid = im.id
                 WHERE im.interviewer = v.Email
                   AND im.interviewDate >= DATEADD(DAY, -30, DATEADD(MINUTE, 330, @NowUtc))
                   AND ISNULL(ps.IsApproved, 0) = 0) x
    WHERE v.Email IS NOT NULL;

    UPDATE o SET ActiveSuspicious = x.n
    FROM #Open o
    CROSS APPLY (SELECT COUNT(1) AS n FROM dbo.SuspiciousInterviewer s
                 WHERE s.UserId = o.UserId AND s.SuspiciousStatus = 1) x;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
        EXEC sp_executesql N'
            UPDATE o SET OpenTrialFlags = x.n
            FROM #Open o
            CROSS APPLY (SELECT COUNT(1) AS n FROM dbo.TrialFlags tf
                         INNER JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
                         WHERE iv.UserId = o.UserId AND LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''pending'') x;';

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NOT NULL
        EXEC sp_executesql N'
            UPDATE o SET OpenDeductionReviews = x.n
            FROM #Open o
            CROSS APPLY (SELECT COUNT(1) AS n FROM dbo.DeductionReviewRequest r
                         WHERE r.InterviewerUserId = o.UserId AND r.Status = ''Open'') x;';

    SELECT
        v.UserId,
        lt.AtUtc  AS LastTouchAtUtc,
        lt.Label  AS LastTouchLabel,
        lt.Actor  AS LastTouchBy,
        cc.DateCreated AS LastConnectedCallAtUtc,
        cc.Seconds     AS LastConnectedCallSeconds,
        cc.Caller      AS LastConnectedCallBy,
        ISNULL(t30.n, 0) AS Touches30d,
        o.OpenTrialFlags,
        o.OpenTrmUnderReview,
        o.SlaPending,
        o.OpenDeductionReviews,
        o.ActiveSuspicious,
        @Days AS LookbackDays
    FROM #Iv v
    INNER JOIN #Open o ON o.UserId = v.UserId
    OUTER APPLY (SELECT TOP 1 t.AtUtc, t.Label, t.Actor FROM #Touch t WHERE t.UserId = v.UserId ORDER BY t.AtUtc DESC) lt
    OUTER APPLY (SELECT COUNT(1) AS n FROM #Touch t WHERE t.UserId = v.UserId AND t.AtUtc >= @Since30) t30
    OUTER APPLY (
        SELECT TOP 1 c.DateCreated, TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '') AS INT) AS Seconds, LTRIM(RTRIM(c.Caller)) AS Caller
        FROM dbo.CallRecordingData c
        WHERE v.Email IS NOT NULL
          AND c.CallRecipient = v.Email
          AND c.DateCreated >= @SinceUtc
          AND ISNULL(TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '') AS INT), 0) >= @MinCallSeconds
          AND ISNULL(c.[Status], '') NOT LIKE '%busy%'
          AND ISNULL(c.[Status], '') NOT LIKE '%no-answer%'
          AND ISNULL(c.[Status], '') NOT LIKE '%NO_ANSWER%'
          AND ISNULL(c.[Status], '') NOT LIKE '%fail%'
          AND ISNULL(c.[Status], '') NOT LIKE '%cancel%'
        ORDER BY c.DateCreated DESC
    ) cc;

    DROP TABLE IF EXISTS #Open;
    DROP TABLE IF EXISTS #Touch;
    DROP TABLE IF EXISTS #Iv;
END
GO
