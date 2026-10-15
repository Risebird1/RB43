/*
================================================================================
usp_QCActivity_Diagnostics — QC Activity dashboard, data check (read-only).

For every source usp_QCActivity_CollectRows reads, returns who the rows in the
IST date range are stored under: one row per source and actor value (top 15
actors per source by rows), with the source total, the first and last time
(IST) and the actor's portal profile. Used to tell, when a dashboard number is
0, whether
  - the source has no rows in the range                     (Status 'No rows')
  - the rows are under a user who is not a Quality Controller (Status 'Not a QC user')
  - the stored actor value is not an email at all           (Status 'Actor is not an email')
  - the email is not a portal user                          (Status 'Not a portal user')
  - or the rows are under a QC (Status 'Quality Controller': the dashboard
    should count them).
Actors the collector skips are reported, not hidden: 'System actor (excluded)'
for AISystem / System (and the legacy 'Admin' on InterviewerActionLog),
'Client (excluded)' for the Client profile on report ratings.
InterviewerActionLog is reported per action type.

Filters per source match the collector (event types, statuses, Plivo call
shape), except that excluded actors are kept so they show up here. Optional
tables are read through sp_executesql behind OBJECT_ID checks; a missing one
reports Status 'Table missing'.

Exposed as GET /QCActivity/Diagnostics?from&to (Admin and SupportAdmin only).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_QCActivity_Diagnostics]
    @FromIst DATE,
    @ToIst   DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromIst IS NULL OR @ToIst IS NULL OR @FromIst > @ToIst
        RETURN;

    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));
    DECLARE @FromUtc   DATETIME = DATEADD(MINUTE, -330, @FromIstDt);
    DECLARE @ToUtc     DATETIME = DATEADD(MINUTE, -330, @ToIstDt);

    /* Sources in display order. TableExists = 0 reports 'Table missing'. */
    CREATE TABLE #Sources (
        SortOrder   INT           NOT NULL,
        [Source]    VARCHAR(80)   NOT NULL PRIMARY KEY,
        Feeds       VARCHAR(200)  NOT NULL,
        TableExists BIT           NOT NULL
    );

    INSERT INTO #Sources (SortOrder, [Source], Feeds, TableExists) VALUES
        (1,  'PaymentSLADetailsHistory.CreatedBy',          'Reports reviewed / penalised',            1),
        (2,  'ClientCommentsForReport.CreatedBy',           'Reports reviewed (every rated save)',     1),
        (3,  'PaymentSLADetails.ModifiedBy',                'Approved / Not approved',                 1),
        (4,  'Trans_QCFormReport.Userid',                   'QC forms',                                1),
        (5,  'TrialFlags.ReviewedBy',                       'Trial kept / removed',                    CASE WHEN OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (6,  'IrfAuditRiskBackfillRun.ExecutedBy',          'IRF audit backfills',                     CASE WHEN OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (7,  'AutoQcIrfBackfillRun.ExecutedBy',             'IRF audit backfills',                     CASE WHEN OBJECT_ID(N'dbo.AutoQcIrfBackfillRun', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (8,  'TransInterviewerAuditLog.ChangedBy (RiskAudit*)', 'IRF audit kept / removed',            CASE WHEN OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (9,  'RedMarkerStatusDetails.CreatedBy (IRF)',      'Total Red Marker',                        1),
        (10, 'RedMarkerStatusDetails.CreatedBy (TotalInterview)', 'Total Interviews',                  1),
        (11, 'CallRecordingData.Caller (Plivo)',            'Plivo calls',                             1),
        (12, 'InterviewerCoachingSession.CoachEmail',       'Coaching',                                CASE WHEN OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (13, 'SuspiciousInterviewer.CreatedBy',             'Interviewer status: Suspicious',          CASE WHEN OBJECT_ID(N'dbo.SuspiciousInterviewer', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (14, 'InterviewerActionLog.ActedBy (SuspiciousUndone)', 'Interviewer status: Suspicious undone', CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (15, 'InterviewerActionLog.ActedBy (Blocked)',      'Interviewer status: Blocked',             CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (16, 'InterviewerActionLog.ActedBy (Unblocked)',    'Interviewer status: Unblocked',           CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (17, 'InterviewerActionLog.ActedBy (AccessRevoked)', 'Interviewer status: Access off',         CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (18, 'InterviewerActionLog.ActedBy (AccessRestored)', 'Interviewer status: Access on',         CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (19, 'InterviewerActionLog.ActedBy (Inactivated)',  'Interviewer status: Inactive',            CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (20, 'InterviewerActionLog.ActedBy (Reactivated)',  'Interviewer status: Reactivated',         CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (21, 'InterviewerActionLog.ActedBy (PaymentHold)',  'Interviewer status: Pay hold',            CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (22, 'InterviewerActionLog.ActedBy (PaymentHoldReleased)', 'Interviewer status: Pay hold released', CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (23, 'ReportEditLog.EditedBy',                      'Reports edited',                          CASE WHEN OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (24, 'InterviewCommentsHistory.PerformedBy (comment changed)', 'Reports edited (before ReportEditLog)', CASE WHEN OBJECT_ID(N'dbo.InterviewCommentsHistory', N'U') IS NOT NULL THEN 1 ELSE 0 END),
        (25, 'InterviewerActionLog.ActedBy (DeductionReview)', 'Deduction review decisions',          CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END);

    /* Per source and actor: rows, first and last time (IST). */
    CREATE TABLE #A (
        [Source]   VARCHAR(80)   NOT NULL,
        Actor      VARCHAR(200)  NULL,
        ActorRows  INT           NOT NULL,
        FirstAtIst DATETIME      NULL,
        LastAtIst  DATETIME      NULL
    );

    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @Params NVARCHAR(200) = N'@FromUtc DATETIME, @ToUtc DATETIME';

    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT 'PaymentSLADetailsHistory.CreatedBy', LTRIM(RTRIM(h.CreatedBy)), COUNT(*),
           DATEADD(MINUTE, 330, MIN(h.CreatedOn)), DATEADD(MINUTE, 330, MAX(h.CreatedOn))
    FROM dbo.PaymentSLADetailsHistory h
    WHERE h.CreatedOn >= @FromUtc AND h.CreatedOn < @ToUtc AND h.Interviewid IS NOT NULL
    GROUP BY LTRIM(RTRIM(h.CreatedBy));

    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT 'ClientCommentsForReport.CreatedBy', LTRIM(RTRIM(c.CreatedBy)), COUNT(*),
           DATEADD(MINUTE, 330, MIN(c.CreatedAt)), DATEADD(MINUTE, 330, MAX(c.CreatedAt))
    FROM dbo.ClientCommentsForReport c
    WHERE c.CreatedAt >= @FromUtc AND c.CreatedAt < @ToUtc AND c.Rating IS NOT NULL
    GROUP BY LTRIM(RTRIM(c.CreatedBy));

    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT 'PaymentSLADetails.ModifiedBy', LTRIM(RTRIM(ps.ModifiedBy)), COUNT(*),
           DATEADD(MINUTE, 330, MIN(ps.ModifiedOn)), DATEADD(MINUTE, 330, MAX(ps.ModifiedOn))
    FROM dbo.PaymentSLADetails ps
    WHERE ps.IsApproved IN (1, 2) AND ps.ModifiedOn >= @FromUtc AND ps.ModifiedOn < @ToUtc
    GROUP BY LTRIM(RTRIM(ps.ModifiedBy));

    -- Actor is a UserList id here; an id with no user is reported as 'user id <n>' (not an email).
    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT 'Trans_QCFormReport.Userid',
           ISNULL(LTRIM(RTRIM(ul.EmailId)), 'user id ' + ISNULL(CAST(q.Userid AS VARCHAR(20)), 'NULL')), COUNT(*),
           DATEADD(MINUTE, 330, MIN(q.Addedon)), DATEADD(MINUTE, 330, MAX(q.Addedon))
    FROM dbo.Trans_QCFormReport q
    LEFT JOIN dbo.UserList ul ON ul.id = q.Userid
    WHERE q.Addedon >= @FromUtc AND q.Addedon < @ToUtc
    GROUP BY ISNULL(LTRIM(RTRIM(ul.EmailId)), 'user id ' + ISNULL(CAST(q.Userid AS VARCHAR(20)), 'NULL'));

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''TrialFlags.ReviewedBy'', LTRIM(RTRIM(tf.ReviewedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(tf.ReviewedAtUtc)), DATEADD(MINUTE, 330, MAX(tf.ReviewedAtUtc))
        FROM dbo.TrialFlags tf
        WHERE LOWER(LTRIM(RTRIM(tf.ReviewStatus))) IN (''kept'', ''removed'')
          AND tf.ReviewedAtUtc >= @FromUtc AND tf.ReviewedAtUtc < @ToUtc
        GROUP BY LTRIM(RTRIM(tf.ReviewedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    IF OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''IrfAuditRiskBackfillRun.ExecutedBy'', LTRIM(RTRIM(r.ExecutedBy)), COUNT(*),
               DATEADD(MINUTE, 330, CAST(MIN(r.ExecutedAtUtc) AS DATETIME)), DATEADD(MINUTE, 330, CAST(MAX(r.ExecutedAtUtc) AS DATETIME))
        FROM dbo.IrfAuditRiskBackfillRun r
        WHERE r.ExecutedAtUtc >= @FromUtc AND r.ExecutedAtUtc < @ToUtc
        GROUP BY LTRIM(RTRIM(r.ExecutedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    IF OBJECT_ID(N'dbo.AutoQcIrfBackfillRun', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''AutoQcIrfBackfillRun.ExecutedBy'', LTRIM(RTRIM(r.ExecutedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(r.ExecutedAtUtc)), DATEADD(MINUTE, 330, MAX(r.ExecutedAtUtc))
        FROM dbo.AutoQcIrfBackfillRun r
        WHERE r.ExecutedAtUtc >= @FromUtc AND r.ExecutedAtUtc < @ToUtc
        GROUP BY LTRIM(RTRIM(r.ExecutedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    IF OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''TransInterviewerAuditLog.ChangedBy (RiskAudit*)'', LTRIM(RTRIM(l.ChangedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(l.ChangedAt)), DATEADD(MINUTE, 330, MAX(l.ChangedAt))
        FROM dbo.TransInterviewerAuditLog l
        WHERE l.EventType IN (''RiskAuditKept'', ''RiskAuditRemoval'')
          AND l.ChangedAt >= @FromUtc AND l.ChangedAt < @ToUtc
        GROUP BY LTRIM(RTRIM(l.ChangedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    -- RedMarkerStatusDetails.CreatedOn is stored in IST.
    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT CASE WHEN r.[Type] = 'IRF' THEN 'RedMarkerStatusDetails.CreatedBy (IRF)'
                ELSE 'RedMarkerStatusDetails.CreatedBy (TotalInterview)' END,
           LTRIM(RTRIM(r.CreatedBy)), COUNT(*), MIN(r.CreatedOn), MAX(r.CreatedOn)
    FROM dbo.RedMarkerStatusDetails r
    WHERE r.[Type] IN ('IRF', 'TotalInterview')
      AND r.[Status] IN (1, 2, 3)
      AND r.CreatedOn >= @FromIstDt AND r.CreatedOn < @ToIstDt
    GROUP BY r.[Type], LTRIM(RTRIM(r.CreatedBy));

    SET @Sql = N'
    INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
    SELECT ''CallRecordingData.Caller (Plivo)'', LTRIM(RTRIM(c.Caller)), COUNT(*),
           DATEADD(MINUTE, 330, MIN(c.DateCreated)), DATEADD(MINUTE, 330, MAX(c.DateCreated))
    FROM dbo.CallRecordingData c
    WHERE c.DateCreated >= @FromUtc AND c.DateCreated < @ToUtc
      AND c.CallSid LIKE ''%-%-%-%-%''
      AND c.RecordingSid IS NULL'
        + CASE WHEN COL_LENGTH('dbo.CallRecordingData', 'RecipientKind') IS NOT NULL
               THEN N'
      AND (c.RecipientProfileType = ''Interviewer'' OR c.RecipientKind = ''Interviewer'')'
               ELSE N'
      AND c.RecipientProfileType = ''Interviewer''' END + N'
    GROUP BY LTRIM(RTRIM(c.Caller));';
    EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;

    IF OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''InterviewerCoachingSession.CoachEmail'', LTRIM(RTRIM(s.CoachEmail)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(s.CreatedAtUtc)), DATEADD(MINUTE, 330, MAX(s.CreatedAtUtc))
        FROM dbo.InterviewerCoachingSession s
        WHERE s.CreatedAtUtc >= @FromUtc AND s.CreatedAtUtc < @ToUtc
        GROUP BY LTRIM(RTRIM(s.CoachEmail));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    -- SuspiciousInterviewer.Createddate is the app server clock, treated as UTC (as the collector does).
    IF OBJECT_ID(N'dbo.SuspiciousInterviewer', N'U') IS NOT NULL
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT 'SuspiciousInterviewer.CreatedBy', LTRIM(RTRIM(s.CreatedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(s.Createddate)), DATEADD(MINUTE, 330, MAX(s.Createddate))
        FROM dbo.SuspiciousInterviewer s
        WHERE s.Createddate >= @FromUtc AND s.Createddate < @ToUtc
        GROUP BY LTRIM(RTRIM(s.CreatedBy));

    IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''InterviewerActionLog.ActedBy ('' + l.ActionType + '')'', LTRIM(RTRIM(l.ActedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(l.ActedAtUtc)), DATEADD(MINUTE, 330, MAX(l.ActedAtUtc))
        FROM dbo.InterviewerActionLog l
        WHERE l.ActedAtUtc >= @FromUtc AND l.ActedAtUtc < @ToUtc
          AND l.ActionType IN (''SuspiciousUndone'', ''Blocked'', ''Unblocked'', ''AccessRevoked'', ''AccessRestored'',
                               ''Inactivated'', ''Reactivated'', ''PaymentHold'', ''PaymentHoldReleased'',
                               ''DeductionReview'')
        GROUP BY l.ActionType, LTRIM(RTRIM(l.ActedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    IF OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT ''ReportEditLog.EditedBy'', LTRIM(RTRIM(l.EditedBy)), COUNT(*),
               DATEADD(MINUTE, 330, MIN(l.EditedAtUtc)), DATEADD(MINUTE, 330, MAX(l.EditedAtUtc))
        FROM dbo.ReportEditLog l
        WHERE l.EditedAtUtc >= @FromUtc AND l.EditedAtUtc < @ToUtc
        GROUP BY LTRIM(RTRIM(l.EditedBy));';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc;
    END

    -- InterviewCommentsHistory.PerformedAt is IST. Snapshots whose interviewer comment differs from the
    -- previous non-empty snapshot, in the whole range (the collector reads only the part before the first
    -- ReportEditLog row; the interviewer's own saves are kept here so they show up).
    IF OBJECT_ID(N'dbo.InterviewCommentsHistory', N'U') IS NOT NULL
        INSERT INTO #A ([Source], Actor, ActorRows, FirstAtIst, LastAtIst)
        SELECT 'InterviewCommentsHistory.PerformedBy (comment changed)', LTRIM(RTRIM(h.PerformedBy)), COUNT(*),
               MIN(h.PerformedAt), MAX(h.PerformedAt)
        FROM dbo.InterviewCommentsHistory h
        CROSS APPLY (SELECT TOP 1 p.InterviewerComment FROM dbo.InterviewCommentsHistory p
                     WHERE p.InterviewId = h.InterviewId AND p.PerformedAt < h.PerformedAt
                     ORDER BY p.PerformedAt DESC) prev
        WHERE h.PerformedAt >= @FromIstDt AND h.PerformedAt < @ToIstDt
          AND NULLIF(LTRIM(RTRIM(prev.InterviewerComment)), '') IS NOT NULL
          AND (h.InterviewerComment IS NULL
               OR DATALENGTH(h.InterviewerComment) <> DATALENGTH(prev.InterviewerComment)
               OR h.InterviewerComment COLLATE Latin1_General_BIN2 <> prev.InterviewerComment COLLATE Latin1_General_BIN2)
        GROUP BY LTRIM(RTRIM(h.PerformedBy));

    ;WITH Totals AS (
        SELECT a.[Source], SUM(a.ActorRows) AS SourceTotalRows, COUNT(*) AS SourceActors,
               MIN(a.FirstAtIst) AS SourceFirstAtIst, MAX(a.LastAtIst) AS SourceLastAtIst
        FROM #A a
        GROUP BY a.[Source]
    ),
    Ranked AS (
        SELECT a.*, ROW_NUMBER() OVER (PARTITION BY a.[Source] ORDER BY a.ActorRows DESC, a.Actor) AS rn
        FROM #A a
    )
    SELECT
        s.SortOrder,
        s.[Source],
        s.Feeds,
        r.Actor                                   AS ActorValue,
        ISNULL(r.ActorRows, 0)                    AS ActorRows,
        ISNULL(t.SourceTotalRows, 0)              AS SourceTotalRows,
        ISNULL(t.SourceActors, 0)                 AS SourceActors,
        ISNULL(r.FirstAtIst, t.SourceFirstAtIst)  AS FirstAtIst,
        ISNULL(r.LastAtIst, t.SourceLastAtIst)    AS LastAtIst,
        u.ProfileName,
        u.ActorName,
        CASE
            WHEN s.TableExists = 0                                         THEN 'Table missing'
            WHEN r.[Source] IS NULL                                        THEN 'No rows'
            WHEN NULLIF(r.Actor, '') IS NULL                               THEN 'Actor is empty'
            WHEN r.Actor IN ('AISystem', 'System')                         THEN 'System actor (excluded)'
            WHEN r.Actor = 'Admin' AND s.[Source] LIKE 'InterviewerActionLog.%' THEN 'System actor (excluded)'
            WHEN r.Actor NOT LIKE '%_@_%._%' OR r.Actor LIKE '% %'         THEN 'Actor is not an email'
            WHEN u.UserId IS NULL                                          THEN 'Not a portal user'
            WHEN u.ProfileName = 'Quality Controller'                      THEN 'Quality Controller'
            WHEN u.ProfileName = 'Client'
                 AND s.[Source] IN ('PaymentSLADetailsHistory.CreatedBy', 'ClientCommentsForReport.CreatedBy')
                                                                           THEN 'Client (excluded)'
            ELSE 'Not a QC user'
        END                                       AS [Status]
    FROM #Sources s
    LEFT JOIN Totals t ON t.[Source] = s.[Source]
    LEFT JOIN Ranked r ON r.[Source] = s.[Source] AND r.rn <= 15
    OUTER APPLY (
        SELECT TOP 1
            ul.id AS UserId,
            p.[Name] AS ProfileName,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS ActorName
        FROM dbo.UserList ul
        LEFT JOIN dbo.[Profile] p ON p.id = ul.idProfile
        WHERE r.Actor IS NOT NULL AND ul.EmailId = r.Actor
        ORDER BY ul.id DESC
    ) u
    ORDER BY s.SortOrder, ISNULL(r.ActorRows, 0) DESC, r.Actor;
END
GO
