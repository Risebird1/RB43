/*
================================================================================
usp_GetInterviewerQCTimeline — Interviewer QC View side panel.

Result set 1: the interviewer's IRFs (channel owner, on IRF, red markers,
              TRM status, auto-QC verdict, trial status).
Result set 2: dated timeline, newest first, top 1000. Merges
              - human QC actions, calls and coaching sessions from
                usp_QCActivity_CollectRows (same rows as the QC Activity page)
              - system events: AI review penalties, trial flags raised,
                auto-QC flags and removals.
              Kind: QC | Removal | Call | Coaching | AI | System

The headline numbers come from usp_GetInterviewerQCView @InterviewerEmail.
Deploy after usp_QCActivity_CollectRows.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetInterviewerQCTimeline]
    @InterviewerEmail VARCHAR(150),
    @FromIst          DATE,
    @ToIst            DATE
AS
BEGIN
    SET NOCOUNT ON;

    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');

    DECLARE @UserId INT = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @InterviewerEmail ORDER BY id DESC);
    DECLARE @ProfileId INT = (SELECT TOP 1 Id FROM dbo.InterviewerProfile WHERE UserId = @UserId ORDER BY Id DESC);
    DECLARE @InterviewerId INT = (SELECT TOP 1 id FROM dbo.Interviewer WHERE UserId = @UserId ORDER BY id DESC);

    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));
    DECLARE @FromUtc   DATETIME = DATEADD(MINUTE, -330, @FromIstDt);
    DECLARE @ToUtc     DATETIME = DATEADD(MINUTE, -330, @ToIstDt);

    DECLARE @HasTrial    BIT = CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasAutoQc   BIT = CASE WHEN COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @HasAuditLog BIT = CASE WHEN OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL THEN 1 ELSE 0 END;
    DECLARE @Sql NVARCHAR(MAX);

    /* ---------- Result set 1: IRFs ---------- */
    CREATE TABLE #Irf (
        IrfId          INT          NOT NULL PRIMARY KEY,
        IrfName        VARCHAR(200) NULL,
        ChannelOwner   VARCHAR(310) NULL,
        IsActive       BIT          NULL,
        RedMarkers     INT          NULL,
        TrmStatus      INT          NULL,
        AutoQcVerdict  VARCHAR(50)  NULL,
        TrialStatus    VARCHAR(80)  NULL
    );

    IF @UserId IS NOT NULL
    BEGIN
        INSERT INTO #Irf (IrfId, IrfName, ChannelOwner, IsActive, RedMarkers, TrmStatus)
        SELECT t.IRFId, irf.IRFId,
               COALESCE(NULLIF(LTRIM(RTRIM(ISNULL(o.FirstName, '') + ' ' + ISNULL(o.LastName, ''))), ''), o.EmailId),
               CAST(MAX(CAST(ISNULL(t.IsActive, 0) AS INT)) AS BIT),
               MAX(trm.RedMarker), MAX(trm.RedMarkerStatus)
        FROM dbo.TransInterviewer t
        INNER JOIN dbo.IRF irf ON irf.id = t.IRFId
        LEFT JOIN dbo.UserList o ON o.id = irf.RisebirdHr
        LEFT JOIN dbo.TransInterviewerRedMarker trm ON trm.IRFId = t.IRFId AND trm.InterviewerId = @ProfileId
        WHERE t.UserId = @UserId
        GROUP BY t.IRFId, irf.IRFId, o.FirstName, o.LastName, o.EmailId;

        IF @HasAutoQc = 1
            EXEC sp_executesql N'
                UPDATE i SET AutoQcVerdict = NULLIF(LTRIM(RTRIM(t.AutoQcVerdict)), '''')
                FROM #Irf i
                CROSS APPLY (SELECT TOP 1 x.AutoQcVerdict FROM dbo.TransInterviewer x
                             WHERE x.IRFId = i.IrfId AND x.UserId = @UserId ORDER BY x.id DESC) t',
                N'@UserId INT', @UserId = @UserId;

        IF @HasTrial = 1 AND @InterviewerId IS NOT NULL
            EXEC sp_executesql N'
                UPDATE i SET TrialStatus = LEFT(LTRIM(RTRIM(tf.ReviewStatus)) + '' ('' + tf.OverallVerdict + '')'', 80)
                FROM #Irf i
                CROSS APPLY (SELECT TOP 1 x.ReviewStatus, x.OverallVerdict FROM dbo.TrialFlags x
                             WHERE x.IrfId = i.IrfId AND x.InterviewerId = @InterviewerId ORDER BY x.Id DESC) tf',
                N'@InterviewerId INT', @InterviewerId = @InterviewerId;
    END

    SELECT IrfId, IrfName, ChannelOwner, IsActive, RedMarkers, TrmStatus, AutoQcVerdict, TrialStatus
    FROM #Irf
    ORDER BY IsActive DESC, ISNULL(RedMarkers, 0) DESC, IrfName;

    /* ---------- Result set 2: timeline ---------- */
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

    CREATE TABLE #Ev (
        Kind         VARCHAR(10)   NOT NULL,
        Metric       VARCHAR(30)   NOT NULL,
        ActionAtIst  DATETIME      NULL,
        ActorEmail   VARCHAR(150)  NULL,
        InterviewId  INT           NULL,
        IrfId        INT           NULL,
        Amount       INT           NULL,
        Detail       NVARCHAR(500) NULL,
        [Status]     VARCHAR(150)  NULL,
        RecordingUrl VARCHAR(1000) NULL
    );

    IF @UserId IS NOT NULL
    BEGIN
        EXEC dbo.usp_QCActivity_CollectRows @FromIst = @FromIst, @ToIst = @ToIst, @ActorEmail = NULL, @Metric = NULL,
            @InterviewerEmail = @InterviewerEmail;

        INSERT INTO #Ev (Kind, Metric, ActionAtIst, ActorEmail, InterviewId, IrfId, Amount, Detail, [Status], RecordingUrl)
        SELECT
            CASE WHEN r.Metric = 'PlivoCall' THEN 'Call'
                 WHEN r.Metric = 'Coaching' THEN 'Coaching'
                 WHEN r.Metric IN ('TrialRemoved', 'AuditRemoved', 'TrmRemoved', 'TiRemoved') THEN 'Removal'
                 ELSE 'QC' END,
            r.Metric, r.ActionAtIst, r.ActorEmail, r.InterviewId, r.IrfId, r.Amount, r.Detail, r.[Status], r.RecordingUrl
        FROM #QCActivityRows r
        WHERE r.Metric <> 'AuditBackfill'
          AND (r.InterviewerUserId = @UserId
               OR (r.InterviewerUserId IS NULL AND r.InterviewerEmail = @InterviewerEmail));

        -- AI review penalties (latest AISystem save per interview in the range).
        ;WITH AiSaves AS (
            SELECT h.Interviewid, h.PaymentDeduction, h.Recommendation, h.CreatedOn,
                   ROW_NUMBER() OVER (PARTITION BY h.Interviewid ORDER BY h.CreatedOn DESC, h.id DESC) AS rn
            FROM dbo.PaymentSLADetailsHistory h
            INNER JOIN dbo.InterviewMeetings im ON im.id = h.Interviewid
            WHERE LTRIM(RTRIM(h.CreatedBy)) = 'AISystem'
              AND h.CreatedOn >= @FromUtc AND h.CreatedOn < @ToUtc
              AND im.interviewer = @InterviewerEmail
        )
        INSERT INTO #Ev (Kind, Metric, ActionAtIst, ActorEmail, InterviewId, IrfId, Amount, Detail)
        SELECT 'AI', 'AiPenalty', DATEADD(MINUTE, 330, a.CreatedOn), 'AISystem', a.Interviewid, irf.id, a.PaymentDeduction, a.Recommendation
        FROM AiSaves a
        LEFT JOIN dbo.InterviewMeetings im ON im.id = a.Interviewid
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE a.rn = 1 AND a.PaymentDeduction > 0;

        -- Trial flags raised.
        IF @HasTrial = 1 AND @InterviewerId IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #Ev (Kind, Metric, ActionAtIst, ActorEmail, InterviewId, IrfId, Detail, [Status])
                SELECT ''System'', ''TrialFlagged'', DATEADD(MINUTE, 330, tf.FlaggedAtUtc), ''System'',
                       tf.TriggeringInterviewId, tf.IrfId,
                       LEFT(N''Trial flag raised: '' + tf.OverallVerdict, 500), LEFT(tf.ReviewStatus, 150)
                FROM dbo.TrialFlags tf
                WHERE tf.InterviewerId = @InterviewerId
                  AND tf.FlaggedAtUtc >= @FromUtc AND tf.FlaggedAtUtc < @ToUtc',
                N'@InterviewerId INT, @FromUtc DATETIME, @ToUtc DATETIME',
                @InterviewerId = @InterviewerId, @FromUtc = @FromUtc, @ToUtc = @ToUtc;

        -- Auto-QC flags and removals.
        IF @HasAuditLog = 1
            EXEC sp_executesql N'
                INSERT INTO #Ev (Kind, Metric, ActionAtIst, ActorEmail, InterviewId, IrfId, Detail, [Status])
                SELECT CASE WHEN l.EventType = ''AutoQcRemoval'' THEN ''Removal'' ELSE ''System'' END,
                       l.EventType, DATEADD(MINUTE, 330, l.ChangedAt), l.ChangedBy,
                       l.TriggeringInterviewId, t.IRFId, LEFT(l.ReasonCategory, 500), LEFT(l.ToValue, 150)
                FROM dbo.TransInterviewerAuditLog l
                INNER JOIN dbo.TransInterviewer t ON t.id = l.TransInterviewerId
                WHERE t.UserId = @UserId
                  AND l.EventType IN (''AutoQcFlag'', ''AutoQcRemoval'')
                  AND l.ChangedAt >= @FromUtc AND l.ChangedAt < @ToUtc',
                N'@UserId INT, @FromUtc DATETIME, @ToUtc DATETIME',
                @UserId = @UserId, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    SELECT TOP (1000)
        e.Kind, e.Metric, e.ActionAtIst, e.ActorEmail, e.InterviewId, e.IrfId, irf.IRFId AS IrfName,
        e.Amount, e.Detail, e.[Status], e.RecordingUrl
    FROM #Ev e
    LEFT JOIN dbo.IRF irf ON irf.id = e.IrfId
    ORDER BY e.ActionAtIst DESC;
END
GO
