/*
================================================================================
usp_GetInterviewerQCView — Interviewer QC View (/QCActivity/Interviewers).

One row per interviewer: everything QC did about them in the IST range plus
what is open right now. Built on usp_QCActivity_CollectRows, so its numbers
match the QC Activity page.

Who is listed (top 3000 by attention score):
  - interviewers a human QC action or call or coaching session touched in the range
  - interviewers the AI review penalised in the range (PaymentSLADetailsHistory, CreatedBy = AISystem)
  - anyone with an open flag now (pending trial flag, auto-QC verdict on an active
    channel, unreviewed red markers, Total Interviews under review)
@InterviewerEmail returns exactly that interviewer (even with no activity).

Attention = penalised x2 + AI penalised + open flags x3 + red markers
          + (trial + audit removals) x2 + 5 when the interviewer needs a call.
Needs call = an issue in the range or an open flag, and no QC call or
coaching session in the range.

Notes: InterviewMeetings.interviewDate is compared as IST (as the Total Red
Marker pages do). Deploy after usp_QCActivity_CollectRows.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerQCView]
    @FromIst          DATE,
    @ToIst            DATE,
    @Search           NVARCHAR(150) = NULL,
    @InterviewerEmail VARCHAR(150)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Search           = NULLIF(LTRIM(RTRIM(@Search)), N'');
    -- Search text is literal: [, % and _ typed by the user are not LIKE wildcards.
    DECLARE @SearchPattern NVARCHAR(500) = CASE WHEN @Search IS NULL THEN NULL
        ELSE N'%' + REPLACE(REPLACE(REPLACE(@Search, N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;
    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');

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
        SourceId          INT           NULL
    );

    -- One interviewer (side panel headline): collect only their rows instead of everyone's.
    EXEC dbo.usp_QCActivity_CollectRows @FromIst = @FromIst, @ToIst = @ToIst, @ActorEmail = NULL, @Metric = NULL,
        @InterviewerEmail = @InterviewerEmail;

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
    SELECT u.id, a.Interviewid
    FROM AiSaves a
    INNER JOIN dbo.InterviewMeetings im ON im.id = a.Interviewid
    CROSS APPLY (SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = im.interviewer ORDER BY ul.id DESC) u
    WHERE a.rn = 1 AND a.PaymentDeduction > 0;

    /* Open right now, per interviewer. */
    CREATE TABLE #Open (UserId INT NOT NULL, Kind VARCHAR(10) NOT NULL, Cnt INT NOT NULL);

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
        EXEC sp_executesql N'
            INSERT INTO #Open (UserId, Kind, Cnt)
            SELECT iv.UserId, ''Trial'', COUNT(*)
            FROM dbo.TrialFlags tf
            INNER JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
            WHERE LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''pending''
            GROUP BY iv.UserId';

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
        INSERT INTO #P (UserId)
        SELECT TOP 1 ul.id FROM dbo.UserList ul WHERE ul.EmailId = @InterviewerEmail ORDER BY ul.id DESC;
    END
    ELSE
    BEGIN
        INSERT INTO #P (UserId)
        SELECT UserId FROM #R
        UNION SELECT UserId FROM #Ai
        UNION SELECT UserId FROM #Open;
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

    /* Work. */
    SELECT t.UserId, COUNT(*) AS ActiveIrfs
    INTO #Irfs
    FROM dbo.TransInterviewer t INNER JOIN #P p ON p.UserId = t.UserId
    WHERE t.IsActive = 1
    GROUP BY t.UserId;

    SELECT p.UserId, COUNT(DISTINCT im.id) AS CompletedInterviews
    INTO #Done
    FROM #P p
    INNER JOIN dbo.UserList ul ON ul.id = p.UserId
    INNER JOIN dbo.InterviewMeetings im ON im.interviewer = ul.EmailId
    INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
    WHERE ic.MeetingStatus IN (4, 6, 7, 8)
      AND im.interviewDate >= @FromIstDt
      AND im.interviewDate <  @ToIstDt
    GROUP BY p.UserId;

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
            ISNULL(d.CompletedInterviews, 0)                               AS CompletedInterviews,
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
        LEFT JOIN #Done    d  ON d.UserId  = p.UserId
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
               b.Penalised + b.AiPenalised + b.TrialRemoved + b.AuditRemoved + b.TrmRemoved + b.TiRemoved AS IssuesInRange
        FROM Base b
    ),
    Flagged AS (
        SELECT s.*,
               CAST(CASE WHEN (s.IssuesInRange > 0 OR s.OpenFlags > 0) THEN 1 ELSE 0 END AS BIT) AS HasIssue,
               CAST(CASE WHEN (s.IssuesInRange > 0 OR s.OpenFlags > 0) AND s.Calls = 0 AND s.CoachingSessions = 0 THEN 1 ELSE 0 END AS BIT) AS NeedsCall,
               CAST(CASE WHEN s.IsActive = 0 OR s.TiStatus = 3
                           OR s.TrialRemoved + s.AuditRemoved + s.TrmRemoved + s.TiRemoved > 0 THEN 1 ELSE 0 END AS BIT) AS RemovedOrInactive
        FROM Scored s
    )
    SELECT TOP (3000)
        f.*,
        f.Penalised * 2 + f.AiPenalised + f.OpenFlags * 3 + f.RedMarkers
            + (f.TrialRemoved + f.AuditRemoved) * 2
            + CASE WHEN f.NeedsCall = 1 THEN 5 ELSE 0 END AS AttentionScore
    FROM Flagged f
    ORDER BY AttentionScore DESC, f.LastActionAtIst DESC, f.InterviewerEmail;
END
GO
