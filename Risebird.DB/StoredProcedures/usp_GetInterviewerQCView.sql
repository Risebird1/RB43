/*
================================================================================
usp_GetInterviewerQCView — Interviewer QC View (/QCActivity/Interviewers).

One row per interviewer: everything QC did about them in the IST range, how
their interviews went in the range, and what is open right now. Built on
usp_QCActivity_CollectRows, so its numbers match the QC Activity page.

Who is listed (top 3000 by attention score):
  1. interviewers a QC acted on in the range: a review with a penalty, a
     Payment SLA "not approved", a QC form with red flags, a trial / IRF audit
     decision, a Total Red Marker / Total Interviews move, a QC call or a
     coaching session, a suspicious mark, a block, portal access turned off,
     an inactivation or a payment hold. A routine no-penalty review, an
     approval, a QC form with no red flags, a report edit or an undo
     (SuspiciousUndone, Unblocked, AccessRestored, Reactivated,
     PaymentHoldReleased) does not list anyone on its own.
  2. interviewers with at least @MinInterviews completed interviews in the
     range (InterviewComments.MeetingStatus 4, 6, 7, 8) and a bad interview
     (RiskCategory P0 to P5) or an AI review penalty in the range.
  3. only when @IncludeOpenFlags = 1: anyone with an older open flag now
     (pending trial flag, auto-QC verdict on an active channel, unreviewed red
     markers on an active channel, Total Interviews under review).
@InterviewerEmail returns exactly that interviewer (even with no activity).

HasIssue (quality problem) =
     (CompletedInterviews >= @MinInterviews AND (BadInterviews >= 2 OR BadPct >= 25))
  OR any P0 / P1 interview, QC penalty or AI penalty in the range
  OR a trial or IRF audit removal in the range.
NeedsCall = HasIssue and no QC call or coaching session in the range.

Attention = P0/P1 x5 + bad x2 + BadPct/10 (only with enough volume)
          + penalised x2 + AI penalised + (trial + audit removals) x2
          + open flags (at most 5) + 5 when the interviewer needs a call.
Lifetime red markers are shown but no longer scored.

Completed interviews are mapped to the interviewer by email, or by coded name
(InterviewerProfile.NickName) for meetings that store it. The completed set is
built once for the range. If a 90-day range is slow, an index on
InterviewMeetings (interviewDate) INCLUDE (interviewer) helps.

Notes: InterviewMeetings.interviewDate is compared as IST (as the Total Red
Marker pages do). Deploy after usp_QCActivity_CollectRows.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerQCView]
    @FromIst          DATE,
    @ToIst            DATE,
    @Search           NVARCHAR(150) = NULL,
    @InterviewerEmail VARCHAR(150)  = NULL,
    @MinInterviews    INT           = 3,
    @IncludeOpenFlags BIT           = 0
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search           = NULLIF(LTRIM(RTRIM(@Search)), N'');
    -- Search text is literal: [, % and _ typed by the user are not LIKE wildcards.
    DECLARE @SearchPattern NVARCHAR(500) = CASE WHEN @Search IS NULL THEN NULL
        ELSE N'%' + REPLACE(REPLACE(REPLACE(@Search, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;
    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');

    IF @MinInterviews IS NULL OR @MinInterviews < 1 SET @MinInterviews = 1;
    IF @MinInterviews > 50 SET @MinInterviews = 50;
    SET @IncludeOpenFlags = ISNULL(@IncludeOpenFlags, 0);

    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));
    DECLARE @FromUtc   DATETIME = DATEADD(MINUTE, -330, @FromIstDt);
    DECLARE @ToUtc     DATETIME = DATEADD(MINUTE, -330, @ToIstDt);

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

    -- One interviewer (side panel headline): collect only their rows instead of everyone's.
    EXEC dbo.usp_QCActivity_CollectRows @FromIst = @FromIst, @ToIst = @ToIst, @ActorEmail = NULL, @Metric = NULL,
        @InterviewerEmail = @InterviewerEmail;

    /* The single interviewer asked for, and their coded name (meetings may store either). */
    DECLARE @OneUserId INT = NULL;
    DECLARE @OneNick   VARCHAR(100) = NULL;
    IF @InterviewerEmail IS NOT NULL
    BEGIN
        SET @OneUserId = (SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = @InterviewerEmail ORDER BY ul.id DESC);
        SET @OneNick   = (SELECT TOP 1 NULLIF(LTRIM(RTRIM(ip.NickName)), '') FROM dbo.InterviewerProfile ip
                          WHERE ip.UserId = @OneUserId ORDER BY ip.Id DESC);
    END

    /* Rows resolved to the interviewer's UserList.id (backfill runs are per IRF, not per interviewer). */
    CREATE TABLE #R (
        UserId      INT          NOT NULL,
        Metric      VARCHAR(30)  NOT NULL,
        Amount      INT          NULL,
        ActorEmail  VARCHAR(150) NOT NULL,
        ActionAtIst DATETIME     NULL
    );
    INSERT INTO #R (UserId, Metric, Amount, ActorEmail, ActionAtIst)
    SELECT COALESCE(r.InterviewerUserId, u.id), r.Metric, r.Amount, r.ActorEmail, r.ActionAtIst
    FROM #QCActivityRows r
    OUTER APPLY (
        SELECT TOP 1 ul.id FROM dbo.UserList ul
        WHERE r.InterviewerUserId IS NULL AND r.InterviewerEmail IS NOT NULL AND ul.EmailId = r.InterviewerEmail
        ORDER BY ul.id DESC
    ) u
    WHERE r.Metric <> 'AuditBackfill'
      AND COALESCE(r.InterviewerUserId, u.id) IS NOT NULL;

    /* Completed interviews in the range, built once and mapped to a user by email or coded name. */
    CREATE TABLE #Meet (
        InterviewId INT          NOT NULL PRIMARY KEY,
        Raw         VARCHAR(200) NOT NULL
    );
    INSERT INTO #Meet (InterviewId, Raw)
    SELECT im.id, LEFT(LTRIM(RTRIM(im.interviewer)), 200)
    FROM dbo.InterviewMeetings im
    WHERE im.interviewDate >= @FromIstDt
      AND im.interviewDate <  @ToIstDt
      AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL
      AND (@InterviewerEmail IS NULL
           OR LTRIM(RTRIM(im.interviewer)) = @InterviewerEmail
           OR (@OneNick IS NOT NULL AND LTRIM(RTRIM(im.interviewer)) = @OneNick))
      AND EXISTS (SELECT 1 FROM dbo.InterviewComments ic
                  WHERE ic.InterviewId = im.id AND ic.MeetingStatus IN (4, 6, 7, 8));
    CREATE INDEX IX_Meet_Raw ON #Meet (Raw);

    CREATE TABLE #Who (Raw VARCHAR(200) NOT NULL PRIMARY KEY, UserId INT NULL);
    INSERT INTO #Who (Raw) SELECT DISTINCT m.Raw FROM #Meet m;

    UPDATE w SET w.UserId = u.id
    FROM #Who w
    CROSS APPLY (SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = w.Raw ORDER BY ul.id DESC) u;

    UPDATE w SET w.UserId = n.UserId
    FROM #Who w
    CROSS APPLY (SELECT TOP 1 ip.UserId FROM dbo.InterviewerProfile ip
                 WHERE LTRIM(RTRIM(ip.NickName)) = w.Raw AND ip.UserId IS NOT NULL ORDER BY ip.Id DESC) n
    WHERE w.UserId IS NULL;

    CREATE TABLE #Iv (
        InterviewId INT        NOT NULL PRIMARY KEY,
        UserId      INT        NOT NULL,
        SevLevel    TINYINT    NULL     -- 0..5 for P0..P5, NULL when not a bad interview
    );
    INSERT INTO #Iv (InterviewId, UserId, SevLevel)
    SELECT m.InterviewId, w.UserId,
           CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
                THEN CAST(SUBSTRING(UPPER(LTRIM(RTRIM(rc.Severity))), 2, 1) AS TINYINT) END
    FROM #Meet m
    INNER JOIN #Who w ON w.Raw = m.Raw AND w.UserId IS NOT NULL
    LEFT JOIN dbo.RiskCategory rc ON rc.InterviewId = m.InterviewId;

    SELECT i.UserId,
           COUNT(*)                                                   AS CompletedInterviews,
           SUM(CASE WHEN i.SevLevel IS NOT NULL THEN 1 ELSE 0 END)    AS BadInterviews,
           SUM(CASE WHEN i.SevLevel IN (0, 1) THEN 1 ELSE 0 END)      AS SevereInterviews,
           MIN(i.SevLevel)                                            AS WorstLevel
    INTO #Work
    FROM #Iv i
    GROUP BY i.UserId;

    /* AI review penalties in the range: the latest AISystem save per interview with a deduction. */
    CREATE TABLE #Ai (UserId INT NOT NULL, InterviewId INT NOT NULL);
    ;WITH AiSaves AS (
        SELECT h.Interviewid, h.PaymentDeduction,
               ROW_NUMBER() OVER (PARTITION BY h.Interviewid ORDER BY h.CreatedOn DESC, h.id DESC) AS rn
        FROM dbo.PaymentSLADetailsHistory h
        WHERE LTRIM(RTRIM(h.CreatedBy)) = 'AISystem'
          AND h.CreatedOn >= @FromUtc AND h.CreatedOn < @ToUtc
          AND h.Interviewid IS NOT NULL
    )
    INSERT INTO #Ai (UserId, InterviewId)
    SELECT COALESCE(ue.id, un.UserId), a.Interviewid
    FROM AiSaves a
    INNER JOIN dbo.InterviewMeetings im ON im.id = a.Interviewid
    OUTER APPLY (SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = im.interviewer ORDER BY ul.id DESC) ue
    OUTER APPLY (SELECT TOP 1 ip.UserId FROM dbo.InterviewerProfile ip
                 WHERE ue.id IS NULL AND LTRIM(RTRIM(ip.NickName)) = LTRIM(RTRIM(im.interviewer)) AND ip.UserId IS NOT NULL
                 ORDER BY ip.Id DESC) un
    WHERE a.rn = 1 AND a.PaymentDeduction > 0
      AND COALESCE(ue.id, un.UserId) IS NOT NULL
      AND (@OneUserId IS NULL OR COALESCE(ue.id, un.UserId) = @OneUserId);

    /* Open right now, per interviewer. */
    CREATE TABLE #Open (UserId INT NOT NULL, Kind VARCHAR(10) NOT NULL, Cnt INT NOT NULL);

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Open (UserId, Kind, Cnt)
            SELECT iv.UserId, ''Trial'', COUNT(*)
            FROM dbo.TrialFlags tf
            INNER JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
            WHERE LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''pending''
              AND iv.UserId IS NOT NULL
            GROUP BY iv.UserId';

    -- AutoQcVerdict non-empty means flagged (same rule as DmIvBadQualityFlagHelper).
    IF COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Open (UserId, Kind, Cnt)
            SELECT t.UserId, ''Audit'', COUNT(*)
            FROM dbo.TransInterviewer t
            WHERE t.IsActive = 1
              AND t.UserId IS NOT NULL
              AND NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''''))), '''') IS NOT NULL
            GROUP BY t.UserId';

    -- Unreviewed red markers on a channel the interviewer is still active on (Pending or Under review).
    INSERT INTO #Open (UserId, Kind, Cnt)
    SELECT ip.UserId, 'Trm', COUNT(*)
    FROM dbo.TransInterviewerRedMarker trm
    INNER JOIN dbo.InterviewerProfile ip ON ip.Id = trm.InterviewerId
    INNER JOIN dbo.TransInterviewer t ON t.IRFId = trm.IRFId AND t.UserId = ip.UserId AND t.IsActive = 1
    WHERE trm.RedMarker > 0
      AND ISNULL(trm.RedMarkerStatus, 0) NOT IN (2, 3)
    GROUP BY ip.UserId;

    INSERT INTO #Open (UserId, Kind, Cnt)
    SELECT ip.UserId, 'Ti', 1
    FROM dbo.InterviewerProfile ip
    WHERE ip.RedMarkerStatus = 1;

    /* Population. */
    CREATE TABLE #P (UserId INT NOT NULL PRIMARY KEY);
    IF @InterviewerEmail IS NOT NULL
    BEGIN
        IF @OneUserId IS NOT NULL
            INSERT INTO #P (UserId) VALUES (@OneUserId);
    END
    ELSE
    BEGIN
        -- 1. A QC acted on them (routine no-penalty reviews, approvals and clean QC forms do not count;
        --    nor do report edits or undos: unmarking, unblocking, restoring access, reactivating, releasing a hold).
        INSERT INTO #P (UserId)
        SELECT DISTINCT r.UserId
        FROM #R r
        WHERE NOT (r.Metric = 'ReportReviewed' AND ISNULL(r.Amount, 0) = 0)
          AND r.Metric <> 'SlaApproved'
          AND NOT (r.Metric = 'QCForm' AND ISNULL(r.Amount, 0) = 0)
          AND r.Metric NOT IN ('ReportEdited', 'SuspiciousUndone', 'Unblocked', 'AccessRestored', 'Reactivated', 'PaymentHoldReleased');

        -- 2. Enough completed interviews, and a bad interview or an AI penalty in the range.
        INSERT INTO #P (UserId)
        SELECT w.UserId
        FROM #Work w
        WHERE w.CompletedInterviews >= @MinInterviews
          AND (w.BadInterviews > 0 OR EXISTS (SELECT 1 FROM #Ai a WHERE a.UserId = w.UserId))
          AND NOT EXISTS (SELECT 1 FROM #P p WHERE p.UserId = w.UserId);

        -- 3. Older open flags, only when asked for.
        IF @IncludeOpenFlags = 1
            INSERT INTO #P (UserId)
            SELECT DISTINCT o.UserId
            FROM #Open o
            WHERE NOT EXISTS (SELECT 1 FROM #P p WHERE p.UserId = o.UserId);
    END

    /* Range aggregates. */
    SELECT
        r.UserId,
        SUM(CASE WHEN r.Metric = 'ReportReviewed' THEN 1 ELSE 0 END)                       AS QcReviewed,
        SUM(CASE WHEN r.Metric = 'ReportReviewed' AND r.Amount > 0 THEN 1 ELSE 0 END)      AS Penalised,
        AVG(CASE WHEN r.Metric = 'ReportReviewed' AND r.Amount > 0 THEN CAST(r.Amount AS DECIMAL(9, 2)) END) AS AvgPenaltyPercent,
        SUM(CASE WHEN r.Metric = 'TrialKept' THEN 1 ELSE 0 END)                            AS TrialKept,
        SUM(CASE WHEN r.Metric = 'TrialRemoved' THEN 1 ELSE 0 END)                         AS TrialRemoved,
        SUM(CASE WHEN r.Metric = 'AuditKept' THEN 1 ELSE 0 END)                            AS AuditKept,
        SUM(CASE WHEN r.Metric = 'AuditRemoved' THEN 1 ELSE 0 END)                         AS AuditRemoved,
        SUM(CASE WHEN r.Metric = 'TrmRemoved' THEN 1 ELSE 0 END)                           AS TrmRemoved,
        SUM(CASE WHEN r.Metric = 'TiRemoved' THEN 1 ELSE 0 END)                            AS TiRemoved,
        SUM(CASE WHEN r.Metric = 'PlivoCall' THEN 1 ELSE 0 END)                            AS Calls,
        SUM(CASE WHEN r.Metric = 'PlivoCall' THEN ISNULL(r.Amount, 0) ELSE 0 END)          AS TalkSeconds,
        COUNT(DISTINCT CASE WHEN r.Metric = 'PlivoCall' THEN r.ActorEmail END)             AS DistinctCallers,
        MAX(CASE WHEN r.Metric = 'PlivoCall' THEN r.ActionAtIst END)                       AS LastCallAtIst,
        SUM(CASE WHEN r.Metric = 'Coaching' THEN 1 ELSE 0 END)                             AS CoachingSessions,
        MAX(CASE WHEN r.Metric = 'Coaching' THEN r.ActionAtIst END)                        AS LastCoachedAtIst,
        SUM(CASE WHEN r.Metric NOT IN ('PlivoCall', 'Coaching') THEN 1 ELSE 0 END)         AS QcActions,
        MAX(r.ActionAtIst)                                                                 AS LastActionAtIst
    INTO #Agg
    FROM #R r
    INNER JOIN #P p ON p.UserId = r.UserId
    GROUP BY r.UserId;

    SELECT a.UserId, COUNT(DISTINCT a.InterviewId) AS AiPenalised
    INTO #AiAgg
    FROM #Ai a INNER JOIN #P p ON p.UserId = a.UserId
    GROUP BY a.UserId;

    SELECT o.UserId,
           SUM(CASE WHEN o.Kind = 'Trial' THEN o.Cnt ELSE 0 END) AS TrialOpen,
           SUM(CASE WHEN o.Kind = 'Audit' THEN o.Cnt ELSE 0 END) AS AuditOpen,
           SUM(CASE WHEN o.Kind = 'Trm'   THEN o.Cnt ELSE 0 END) AS TrmOpen,
           SUM(CASE WHEN o.Kind = 'Ti'    THEN o.Cnt ELSE 0 END) AS TiOpen
    INTO #OpenAgg
    FROM #Open o INNER JOIN #P p ON p.UserId = o.UserId
    GROUP BY o.UserId;

    /* Active channels now. */
    SELECT t.UserId, COUNT(*) AS ActiveIrfs
    INTO #Irfs
    FROM dbo.TransInterviewer t INNER JOIN #P p ON p.UserId = t.UserId
    WHERE t.IsActive = 1
    GROUP BY t.UserId;

    SELECT ip.UserId,
           SUM(ISNULL(trm.RedMarker, 0))                                   AS RedMarkers,
           SUM(CASE WHEN trm.RedMarkerStatus = 1 THEN 1 ELSE 0 END)        AS TrmUnderReviewNow
    INTO #Trm
    FROM dbo.TransInterviewerRedMarker trm
    INNER JOIN dbo.InterviewerProfile ip ON ip.Id = trm.InterviewerId
    INNER JOIN #P p ON p.UserId = ip.UserId
    GROUP BY ip.UserId;

    ;WITH Base AS (
        SELECT
            p.UserId                                                       AS InterviewerUserId,
            ul.EmailId                                                     AS InterviewerEmail,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS InterviewerName,
            -- MobileNumber is text in production ('+91...'): never compare it with a number.
            NULLIF(NULLIF(LTRIM(RTRIM(CAST(ul.MobileNumber AS NVARCHAR(200)))), N''), N'0') AS Mobile,
            CAST(ISNULL(ul.IsActive, 0) AS BIT)                            AS IsActive,
            ipf.RedMarkerStatus                                            AS TiStatus,
            ISNULL(i.ActiveIrfs, 0)                                        AS ActiveIrfs,
            ISNULL(w.CompletedInterviews, 0)                               AS CompletedInterviews,
            ISNULL(w.BadInterviews, 0)                                     AS BadInterviews,
            ISNULL(w.SevereInterviews, 0)                                  AS SevereInterviews,
            CASE WHEN w.WorstLevel IS NULL THEN NULL
                 ELSE 'P' + CAST(w.WorstLevel AS VARCHAR(1)) END           AS WorstSeverity,
            CAST(CASE WHEN ISNULL(w.CompletedInterviews, 0) = 0 THEN NULL
                      ELSE ROUND(100.0 * w.BadInterviews / w.CompletedInterviews, 1) END AS DECIMAL(5, 1)) AS BadPct,
            ISNULL(g.QcReviewed, 0)                                        AS QcReviewed,
            ISNULL(g.Penalised, 0)                                         AS Penalised,
            CAST(g.AvgPenaltyPercent AS DECIMAL(9, 2))                     AS AvgPenaltyPercent,
            ISNULL(ai.AiPenalised, 0)                                      AS AiPenalised,
            ISNULL(o.TrialOpen, 0)                                         AS TrialOpen,
            ISNULL(g.TrialKept, 0)                                         AS TrialKept,
            ISNULL(g.TrialRemoved, 0)                                      AS TrialRemoved,
            ISNULL(o.AuditOpen, 0)                                         AS AuditOpen,
            ISNULL(g.AuditKept, 0)                                         AS AuditKept,
            ISNULL(g.AuditRemoved, 0)                                      AS AuditRemoved,
            ISNULL(t.RedMarkers, 0)                                        AS RedMarkers,
            ISNULL(t.TrmUnderReviewNow, 0)                                 AS TrmUnderReviewNow,
            ISNULL(o.TrmOpen, 0)                                           AS TrmOpen,
            ISNULL(g.TrmRemoved, 0)                                        AS TrmRemoved,
            ISNULL(o.TiOpen, 0)                                            AS TiOpen,
            ISNULL(g.TiRemoved, 0)                                         AS TiRemoved,
            ISNULL(g.Calls, 0)                                             AS Calls,
            ISNULL(g.TalkSeconds, 0)                                       AS TalkSeconds,
            ISNULL(g.DistinctCallers, 0)                                   AS DistinctCallers,
            g.LastCallAtIst,
            ISNULL(g.CoachingSessions, 0)                                  AS CoachingSessions,
            g.LastCoachedAtIst,
            ISNULL(g.QcActions, 0)                                         AS QcActions,
            g.LastActionAtIst
        FROM #P p
        INNER JOIN dbo.UserList ul ON ul.id = p.UserId
        OUTER APPLY (SELECT TOP 1 x.RedMarkerStatus FROM dbo.InterviewerProfile x WHERE x.UserId = p.UserId ORDER BY x.Id DESC) ipf
        LEFT JOIN #Irfs    i  ON i.UserId  = p.UserId
        LEFT JOIN #Work    w  ON w.UserId  = p.UserId
        LEFT JOIN #Agg     g  ON g.UserId  = p.UserId
        LEFT JOIN #AiAgg   ai ON ai.UserId = p.UserId
        LEFT JOIN #OpenAgg o  ON o.UserId  = p.UserId
        LEFT JOIN #Trm     t  ON t.UserId  = p.UserId
        WHERE @Search IS NULL
           OR ul.EmailId LIKE @SearchPattern
           OR (ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, '')) LIKE @SearchPattern
    ),
    Scored AS (
        SELECT b.*,
               b.TrialOpen + b.AuditOpen + b.TrmOpen + b.TiOpen AS OpenFlags,
               b.Penalised + b.AiPenalised + b.TrialRemoved + b.AuditRemoved + b.TrmRemoved + b.TiRemoved AS IssuesInRange,
               CAST(CASE WHEN b.CompletedInterviews >= @MinInterviews THEN 1 ELSE 0 END AS BIT) AS HasVolume
        FROM Base b
    ),
    Flagged AS (
        SELECT s.*,
               CAST(CASE WHEN (s.HasVolume = 1 AND (s.BadInterviews >= 2 OR s.BadPct >= 25))
                           OR s.SevereInterviews > 0
                           OR s.Penalised > 0 OR s.AiPenalised > 0
                           OR s.TrialRemoved + s.AuditRemoved > 0
                         THEN 1 ELSE 0 END AS BIT) AS HasIssue
        FROM Scored s
    ),
    Final AS (
        SELECT f.*,
               CAST(CASE WHEN f.HasIssue = 1 AND f.Calls = 0 AND f.CoachingSessions = 0 THEN 1 ELSE 0 END AS BIT) AS NeedsCall,
               CAST(CASE WHEN f.IsActive = 0 OR f.TiStatus = 3
                           OR f.TrialRemoved + f.AuditRemoved + f.TrmRemoved + f.TiRemoved > 0 THEN 1 ELSE 0 END AS BIT) AS RemovedOrInactive
        FROM Flagged f
    )
    SELECT TOP (3000)
        x.InterviewerUserId, x.InterviewerEmail, x.InterviewerName, x.Mobile, x.IsActive, x.TiStatus,
        x.ActiveIrfs, x.CompletedInterviews, x.BadInterviews, x.SevereInterviews, x.WorstSeverity, x.BadPct,
        x.QcReviewed, x.Penalised, x.AvgPenaltyPercent, x.AiPenalised,
        x.TrialOpen, x.TrialKept, x.TrialRemoved,
        x.AuditOpen, x.AuditKept, x.AuditRemoved,
        x.RedMarkers, x.TrmUnderReviewNow, x.TrmOpen, x.TrmRemoved,
        x.TiOpen, x.TiRemoved,
        x.Calls, x.TalkSeconds, x.DistinctCallers, x.LastCallAtIst,
        x.CoachingSessions, x.LastCoachedAtIst,
        x.QcActions, x.LastActionAtIst,
        x.OpenFlags, x.IssuesInRange, x.HasIssue, x.NeedsCall, x.RemovedOrInactive,
        x.SevereInterviews * 5
            + x.BadInterviews * 2
            + CASE WHEN x.HasVolume = 1 THEN CAST(ROUND(ISNULL(x.BadPct, 0) / 10.0, 0) AS INT) ELSE 0 END
            + x.Penalised * 2
            + x.AiPenalised
            + (x.TrialRemoved + x.AuditRemoved) * 2
            + CASE WHEN x.OpenFlags > 5 THEN 5 ELSE x.OpenFlags END
            + CASE WHEN x.NeedsCall = 1 THEN 5 ELSE 0 END AS AttentionScore
    FROM Final x
    ORDER BY AttentionScore DESC, x.LastActionAtIst DESC, x.InterviewerEmail;
END
GO
