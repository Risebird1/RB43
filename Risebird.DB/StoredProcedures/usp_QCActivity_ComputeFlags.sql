/*
================================================================================
usp_QCActivity_ComputeFlags — QC Activity flags (part 2: authentic QC work).

Reads the CALLER's #QCActivityRows (filled by usp_QCActivity_CollectRows for ALL
actors, with the authenticity columns) and fills the caller's two tables:

    CREATE TABLE #QCActivityFlags (
        Flag        VARCHAR(30)   NOT NULL,
        ActorEmail  VARCHAR(150)  NOT NULL,
        [Count]     INT           NOT NULL,
        FirstAt     DATETIME      NULL,
        LastAt      DATETIME      NULL,
        Detail      NVARCHAR(500) NULL,
        Metrics     VARCHAR(200)  NULL     -- metric keys for the drill-down
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

A flag is a prompt for the QC lead to look, not a verdict. Flags and thresholds
(dbo.QCActivitySetting; defaults in brackets):

  Burst            BurstCount (10)+ actions of the same type by one person within
                   BurstMinutes (10) minutes.
  ReviewedNoCall   Moved to Reviewed (Total Interviews or TRM) with no connected call
                   (answered, MinCallSeconds+) or coaching to that interviewer, by
                   anyone, in the window.
  ReversedByOther  The person's Reviewed / Blocked / Suspicious / Inactivated was
                   undone (Under review / Unblocked / Suspicious undone /
                   Reactivated) by a different person within ReversalDays (7) days
                   (both inside the window).
  ShortCalls       More than ShortCallPct (50)% of the person's dialled calls with
                   a known duration lasted under ShortCallSeconds (30) seconds.
  KeepDespiteP0    IRF audit or trial Keep on an interviewer with a P0 graded
                   interview in the same window.
  NoEvidence       Actions on an interviewer with no evidence in the window (no
                   graded issue, AI penalty, open flag or red marker).

@FromIst / @ToIst: the same window the rows were collected for.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_QCActivity_ComputeFlags]
    @FromIst DATE,
    @ToIst   DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'tempdb..#QCActivityRows') IS NULL
       OR OBJECT_ID(N'tempdb..#QCActivityFlags') IS NULL
       OR OBJECT_ID(N'tempdb..#QCActivityFlagItems') IS NULL
        RETURN;
    IF COL_LENGTH('tempdb..#QCActivityRows', 'HasEvidence') IS NULL
        RETURN;

    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));

    DECLARE @BurstCount INT = 10, @BurstMinutes INT = 10, @ReversalDays INT = 7,
            @ShortCallSeconds INT = 30, @ShortCallPct INT = 50;
    IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
        EXEC sp_executesql N'
            SELECT @BurstCount       = ISNULL(MAX(CASE WHEN Name = ''BurstCount''       THEN TRY_CAST(Value AS INT) END), @BurstCount),
                   @BurstMinutes     = ISNULL(MAX(CASE WHEN Name = ''BurstMinutes''     THEN TRY_CAST(Value AS INT) END), @BurstMinutes),
                   @ReversalDays     = ISNULL(MAX(CASE WHEN Name = ''ReversalDays''     THEN TRY_CAST(Value AS INT) END), @ReversalDays),
                   @ShortCallSeconds = ISNULL(MAX(CASE WHEN Name = ''ShortCallSeconds'' THEN TRY_CAST(Value AS INT) END), @ShortCallSeconds),
                   @ShortCallPct     = ISNULL(MAX(CASE WHEN Name = ''ShortCallPct''     THEN TRY_CAST(Value AS INT) END), @ShortCallPct)
            FROM dbo.QCActivitySetting',
            N'@BurstCount INT OUTPUT, @BurstMinutes INT OUTPUT, @ReversalDays INT OUTPUT, @ShortCallSeconds INT OUTPUT, @ShortCallPct INT OUTPUT',
            @BurstCount = @BurstCount OUTPUT, @BurstMinutes = @BurstMinutes OUTPUT, @ReversalDays = @ReversalDays OUTPUT,
            @ShortCallSeconds = @ShortCallSeconds OUTPUT, @ShortCallPct = @ShortCallPct OUTPUT;
    IF @BurstCount < 2 SET @BurstCount = 2;
    IF @BurstMinutes < 1 SET @BurstMinutes = 1;

    /* Working copy with a row id and the interviewer resolved to UserList.id + email. */
    CREATE TABLE #A (
        RowId             INT IDENTITY(1,1) PRIMARY KEY,
        ActorEmail        VARCHAR(150)  NOT NULL,
        Metric            VARCHAR(30)   NOT NULL,
        ActionAtIst       DATETIME      NULL,
        InterviewId       INT           NULL,
        IrfId             INT           NULL,
        InterviewerUserId INT           NULL,
        InterviewerEmail  VARCHAR(200)  NULL,
        Amount            INT           NULL,
        Detail            NVARCHAR(500) NULL,
        SourceId          INT           NULL,
        HasEvidence       BIT           NULL,
        IsAuthentic       BIT           NULL,
        IsScored          BIT           NOT NULL
    );

    EXEC sp_executesql N'
        INSERT INTO #A (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Amount, Detail, SourceId, HasEvidence, IsAuthentic, IsScored)
        SELECT r.ActorEmail, r.Metric, r.ActionAtIst, r.InterviewId, r.IrfId,
               COALESCE(r.InterviewerUserId, u.id),
               COALESCE(NULLIF(LTRIM(RTRIM(r.InterviewerEmail)), ''''), e.EmailId),
               r.Amount, r.Detail, r.SourceId, r.HasEvidence, r.IsAuthentic,
               CASE WHEN r.Metric IN (''PlivoCall'', ''Coaching'', ''AuditBackfill'', ''TrialBackfill'') THEN 0 ELSE 1 END
        FROM #QCActivityRows r
        OUTER APPLY (SELECT TOP 1 x.id FROM dbo.UserList x
                     WHERE r.InterviewerUserId IS NULL AND x.EmailId = LTRIM(RTRIM(r.InterviewerEmail)) ORDER BY x.id DESC) u
        OUTER APPLY (SELECT TOP 1 LTRIM(RTRIM(x.EmailId)) AS EmailId FROM dbo.UserList x
                     WHERE r.InterviewerUserId IS NOT NULL AND x.id = r.InterviewerUserId) e';

    CREATE INDEX IX_A_Actor_Metric_At ON #A (ActorEmail, Metric, ActionAtIst);
    CREATE INDEX IX_A_User ON #A (InterviewerUserId);

    /* ---- Burst ---- */
    CREATE TABLE #BurstRows (RowId INT NOT NULL PRIMARY KEY);
    INSERT INTO #BurstRows (RowId)
    SELECT DISTINCT x.RowId
    FROM #A a
    INNER JOIN #A x ON x.ActorEmail = a.ActorEmail AND x.Metric = a.Metric
                   AND x.ActionAtIst >= a.ActionAtIst
                   AND x.ActionAtIst <= DATEADD(MINUTE, @BurstMinutes, a.ActionAtIst)
    WHERE a.IsScored = 1 AND a.ActionAtIst IS NOT NULL
      AND (SELECT COUNT(1) FROM #A y
           WHERE y.ActorEmail = a.ActorEmail AND y.Metric = a.Metric
             AND y.ActionAtIst >= a.ActionAtIst
             AND y.ActionAtIst <= DATEADD(MINUTE, @BurstMinutes, a.ActionAtIst)) >= @BurstCount;

    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'Burst', a.ActorEmail, COUNT(1), MIN(a.ActionAtIst), MAX(a.ActionAtIst),
           LEFT(CAST(COUNT(1) AS NVARCHAR(10)) + N' ' + a.Metric + N' actions in bursts of '
                + CAST(@BurstCount AS NVARCHAR(10)) + N'+ within ' + CAST(@BurstMinutes AS NVARCHAR(10)) + N' minutes', 500),
           a.Metric
    FROM #A a INNER JOIN #BurstRows b ON b.RowId = a.RowId
    GROUP BY a.ActorEmail, a.Metric;

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'Burst', a.ActorEmail, a.ActionAtIst, a.Metric, a.InterviewId, a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           LEFT(N'One of ' + CAST(w.Cnt AS NVARCHAR(10)) + N' actions of this type by the same person within '
                + CAST(@BurstMinutes AS NVARCHAR(10)) + N' minutes either side (burst limit '
                + CAST(@BurstCount AS NVARCHAR(10)) + N' in ' + CAST(@BurstMinutes AS NVARCHAR(10)) + N' minutes)', 300)
    FROM #A a INNER JOIN #BurstRows b ON b.RowId = a.RowId
    CROSS APPLY (SELECT COUNT(1) AS Cnt FROM #A y
                 WHERE y.ActorEmail = a.ActorEmail AND y.Metric = a.Metric
                   AND y.ActionAtIst >= DATEADD(MINUTE, -@BurstMinutes, a.ActionAtIst)
                   AND y.ActionAtIst <= DATEADD(MINUTE, @BurstMinutes, a.ActionAtIst)) w;

    /* ---- Reviewed with no call ---- */
    -- Interviewers reached by a connected call or coaching (connected calls only), by id and by email.
    CREATE TABLE #ReachedId (UserId INT NOT NULL PRIMARY KEY);
    CREATE TABLE #ReachedEmail (Email VARCHAR(200) NOT NULL PRIMARY KEY);
    INSERT INTO #ReachedId (UserId)
    SELECT DISTINCT c.InterviewerUserId FROM #A c
    WHERE (c.Metric = 'Coaching' OR (c.Metric = 'PlivoCall' AND c.IsAuthentic = 1)) AND c.InterviewerUserId IS NOT NULL;
    INSERT INTO #ReachedEmail (Email)
    SELECT DISTINCT c.InterviewerEmail FROM #A c
    WHERE (c.Metric = 'Coaching' OR (c.Metric = 'PlivoCall' AND c.IsAuthentic = 1)) AND c.InterviewerEmail IS NOT NULL;

    CREATE TABLE #NoCall (RowId INT NOT NULL PRIMARY KEY);
    INSERT INTO #NoCall (RowId)
    SELECT a.RowId
    FROM #A a
    WHERE a.Metric IN ('TrmReviewed', 'TiReviewed')
      AND (a.InterviewerUserId IS NOT NULL OR a.InterviewerEmail IS NOT NULL)
      AND NOT (   (a.InterviewerUserId IS NOT NULL AND EXISTS (SELECT 1 FROM #ReachedId c WHERE c.UserId = a.InterviewerUserId))
               OR (a.InterviewerEmail IS NOT NULL AND EXISTS (SELECT 1 FROM #ReachedEmail c WHERE c.Email = a.InterviewerEmail)));

    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'ReviewedNoCall', a.ActorEmail, COUNT(1), MIN(a.ActionAtIst), MAX(a.ActionAtIst),
           LEFT(CAST(COUNT(1) AS NVARCHAR(10)) + N' interviewer(s) moved to Reviewed with no connected call or coaching in the window', 500),
           NULL   -- filled below with the distinct metrics
    FROM (SELECT DISTINCT a.ActorEmail, a.Metric, a.ActionAtIst, a.RowId FROM #A a INNER JOIN #NoCall n ON n.RowId = a.RowId) a
    GROUP BY a.ActorEmail;

    UPDATE f SET Metrics = (SELECT STRING_AGG(m.Metric, ',') FROM (SELECT DISTINCT a.Metric FROM #A a
                                                                   INNER JOIN #NoCall n ON n.RowId = a.RowId
                                                                   WHERE a.ActorEmail = f.ActorEmail) m)
    FROM #QCActivityFlags f WHERE f.Flag = 'ReviewedNoCall';

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'ReviewedNoCall', a.ActorEmail, a.ActionAtIst, a.Metric, a.InterviewId, a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           N'No connected call or coaching to this interviewer in the window'
    FROM #A a INNER JOIN #NoCall n ON n.RowId = a.RowId;

    /* ---- Reversed by another QC ---- */
    CREATE TABLE #Rev (RowId INT NOT NULL, ReversedBy VARCHAR(150) NOT NULL, ReversedAt DATETIME NULL, ReversedMetric VARCHAR(30) NOT NULL);
    INSERT INTO #Rev (RowId, ReversedBy, ReversedAt, ReversedMetric)
    SELECT o.RowId, v.ActorEmail, v.ActionAtIst, v.Metric
    FROM #A o
    CROSS APPLY (
        SELECT TOP 1 v.ActorEmail, v.ActionAtIst, v.Metric
        FROM #A v
        WHERE v.ActorEmail <> o.ActorEmail
          AND v.ActionAtIst > o.ActionAtIst
          AND v.ActionAtIst <= DATEADD(DAY, @ReversalDays, o.ActionAtIst)
          AND v.InterviewerUserId = o.InterviewerUserId
          AND (   (o.Metric = 'TrmReviewed' AND v.Metric = 'TrmUnderReview' AND ISNULL(v.IrfId, 0) = ISNULL(o.IrfId, 0))
               OR (o.Metric = 'TiReviewed'  AND v.Metric = 'TiUnderReview')
               OR (o.Metric = 'Blocked'     AND v.Metric = 'Unblocked')
               OR (o.Metric = 'Suspicious'  AND v.Metric = 'SuspiciousUndone')
               OR (o.Metric = 'Inactivated' AND v.Metric = 'Reactivated'))
        ORDER BY v.ActionAtIst
    ) v
    WHERE o.Metric IN ('TrmReviewed', 'TiReviewed', 'Blocked', 'Suspicious', 'Inactivated')
      AND o.InterviewerUserId IS NOT NULL;

    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'ReversedByOther', a.ActorEmail, COUNT(1), MIN(a.ActionAtIst), MAX(a.ActionAtIst),
           LEFT(CAST(COUNT(1) AS NVARCHAR(10)) + N' action(s) undone by another person within '
                + CAST(@ReversalDays AS NVARCHAR(10)) + N' days', 500),
           NULL
    FROM #A a INNER JOIN #Rev r ON r.RowId = a.RowId
    GROUP BY a.ActorEmail;

    UPDATE f SET Metrics = (SELECT STRING_AGG(m.Metric, ',') FROM (SELECT DISTINCT a.Metric FROM #A a
                                                                   INNER JOIN #Rev r ON r.RowId = a.RowId
                                                                   WHERE a.ActorEmail = f.ActorEmail) m)
    FROM #QCActivityFlags f WHERE f.Flag = 'ReversedByOther';

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'ReversedByOther', a.ActorEmail, a.ActionAtIst, a.Metric, a.InterviewId, a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           LEFT(N'Undone (' + r.ReversedMetric + N') by ' + r.ReversedBy + N' on ' + CONVERT(NVARCHAR(16), r.ReversedAt, 120), 300)
    FROM #A a INNER JOIN #Rev r ON r.RowId = a.RowId;

    /* ---- Short calls ---- */
    ;WITH C AS (
        SELECT a.ActorEmail,
               COUNT(1) AS Known,
               SUM(CASE WHEN a.Amount < @ShortCallSeconds THEN 1 ELSE 0 END) AS Short,
               MIN(a.ActionAtIst) AS FirstAt, MAX(a.ActionAtIst) AS LastAt
        FROM #A a
        WHERE a.Metric = 'PlivoCall' AND a.Amount IS NOT NULL
        GROUP BY a.ActorEmail
    )
    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'ShortCalls', c.ActorEmail, c.Short, c.FirstAt, c.LastAt,
           LEFT(CAST(c.Short AS NVARCHAR(10)) + N' of ' + CAST(c.Known AS NVARCHAR(10)) + N' dialled calls ('
                + CAST(CAST(ROUND(100.0 * c.Short / c.Known, 0) AS INT) AS NVARCHAR(10)) + N'%) under '
                + CAST(@ShortCallSeconds AS NVARCHAR(10)) + N' seconds (limit ' + CAST(@ShortCallPct AS NVARCHAR(10)) + N'%)', 500),
           'PlivoCall'
    FROM C c
    WHERE c.Known > 0 AND c.Short * 100.0 / c.Known > @ShortCallPct;

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'ShortCalls', a.ActorEmail, a.ActionAtIst, a.Metric, a.InterviewId, a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           LEFT(N'Call lasted ' + CAST(a.Amount AS NVARCHAR(10)) + N' seconds', 300)
    FROM #A a
    WHERE a.Metric = 'PlivoCall' AND a.Amount IS NOT NULL AND a.Amount < @ShortCallSeconds
      AND EXISTS (SELECT 1 FROM #QCActivityFlags f WHERE f.Flag = 'ShortCalls' AND f.ActorEmail = a.ActorEmail);

    /* ---- Keep despite P0 ---- */
    -- The latest P0 interview of the interviewer in the window is kept so the panel can link to it.
    CREATE TABLE #P0 (RowId INT NOT NULL PRIMARY KEY, P0InterviewId INT NULL, P0At DATETIME NULL);
    INSERT INTO #P0 (RowId, P0InterviewId, P0At)
    SELECT a.RowId, p0.id, p0.interviewDate
    FROM #A a
    CROSS APPLY (SELECT TOP 1 im.id, im.interviewDate
                 FROM dbo.InterviewMeetings im
                 INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
                 WHERE im.interviewer = a.InterviewerEmail
                   AND im.interviewDate >= @FromIstDt AND im.interviewDate < @ToIstDt
                   AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) = 'P0'
                 ORDER BY im.interviewDate DESC, im.id DESC) p0
    WHERE a.Metric IN ('AuditKept', 'TrialKept')
      AND a.InterviewerEmail IS NOT NULL;

    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'KeepDespiteP0', a.ActorEmail, COUNT(1), MIN(a.ActionAtIst), MAX(a.ActionAtIst),
           LEFT(CAST(COUNT(1) AS NVARCHAR(10)) + N' Keep decision(s) on interviewers with a P0 interview in the window', 500),
           NULL
    FROM #A a INNER JOIN #P0 p ON p.RowId = a.RowId
    GROUP BY a.ActorEmail;

    UPDATE f SET Metrics = (SELECT STRING_AGG(m.Metric, ',') FROM (SELECT DISTINCT a.Metric FROM #A a
                                                                   INNER JOIN #P0 p ON p.RowId = a.RowId
                                                                   WHERE a.ActorEmail = f.ActorEmail) m)
    FROM #QCActivityFlags f WHERE f.Flag = 'KeepDespiteP0';

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'KeepDespiteP0', a.ActorEmail, a.ActionAtIst, a.Metric, COALESCE(a.InterviewId, p.P0InterviewId), a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           LEFT(N'Interviewer has a P0 graded interview in the window: #' + CAST(p.P0InterviewId AS NVARCHAR(20))
                + ISNULL(N' on ' + CONVERT(NVARCHAR(16), p.P0At, 120), N''), 300)
    FROM #A a INNER JOIN #P0 p ON p.RowId = a.RowId;

    /* ---- Actions without evidence ---- */
    INSERT INTO #QCActivityFlags (Flag, ActorEmail, [Count], FirstAt, LastAt, Detail, Metrics)
    SELECT 'NoEvidence', a.ActorEmail, COUNT(1), MIN(a.ActionAtIst), MAX(a.ActionAtIst),
           LEFT(CAST(COUNT(1) AS NVARCHAR(10)) + N' action(s) on interviewers with no graded issue, AI penalty, open flag or red marker in the window', 500),
           'NoEvidence'
    FROM #A a
    WHERE a.IsScored = 1 AND ISNULL(a.HasEvidence, 0) = 0
      AND (a.InterviewerUserId IS NOT NULL OR a.InterviewerEmail IS NOT NULL)
    GROUP BY a.ActorEmail;

    INSERT INTO #QCActivityFlagItems (Flag, ActorEmail, ActionAtIst, Metric, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, SourceId, Note)
    SELECT 'NoEvidence', a.ActorEmail, a.ActionAtIst, a.Metric, a.InterviewId, a.IrfId, a.InterviewerUserId, a.InterviewerEmail, a.Detail, a.SourceId,
           N'No evidence for this interviewer in the window'
    FROM #A a
    WHERE a.IsScored = 1 AND ISNULL(a.HasEvidence, 0) = 0
      AND (a.InterviewerUserId IS NOT NULL OR a.InterviewerEmail IS NOT NULL);

    DROP TABLE IF EXISTS #P0;
    DROP TABLE IF EXISTS #Rev;
    DROP TABLE IF EXISTS #NoCall;
    DROP TABLE IF EXISTS #ReachedEmail;
    DROP TABLE IF EXISTS #ReachedId;
    DROP TABLE IF EXISTS #BurstRows;
    DROP TABLE IF EXISTS #A;
END
GO
