/*
================================================================================
usp_QCActivity_CollectRows — QC Activity dashboard, single source of truth.

Fills the CALLER's temp table #QCActivityRows with one row per human action in
the IST date range. usp_GetQCActivityDashboardSummary aggregates these rows and
usp_GetQCActivityDashboardDetails lists them, so a number on the dashboard and
its drill-down always agree.

The caller must create the table first (exact shape):

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
        RefId             INT           NULL    -- optional column; filled only when the caller has it
    );

Metric (atomic, one row each)       Source                                       Actor            Stored time
  ReportReviewed  (Amount = %)      PaymentSLADetailsHistory + ClientComments-   CreatedBy        server clock (UTC)
                                    ForReport (every rated save), latest save
                                    per interview per user; Client profiles
                                    excluded; Detail = recommendation + rating
  SlaApproved / SlaNotApproved      PaymentSLADetails IsApproved = 1 / 2         ModifiedBy       server clock (UTC)
  QCForm          (Amount = flags)  Trans_QCFormReport                           Userid -> email  server clock (UTC)
  TrialKept / TrialRemoved          TrialFlags ReviewStatus kept / removed       ReviewedBy       UTC
  AuditBackfill                     IrfAuditRiskBackfillRun + AutoQcIrfBackfillRun ExecutedBy     UTC
  AuditKept / AuditRemoved          TransInterviewerAuditLog RiskAuditKept /     ChangedBy        UTC
                                    RiskAuditRemoval
  TrmUnderReview/TrmReviewed/TrmRemoved RedMarkerStatusDetails Type IRF 1/2/3    CreatedBy        IST
  TiUnderReview/TiReviewed/TiRemoved    RedMarkerStatusDetails Type TotalInterview CreatedBy      IST
  PlivoCall       (Amount = seconds) CallRecordingData to interviewers, Plivo    Caller           server clock (UTC)
  Coaching        (Amount = linked call seconds) InterviewerCoachingSession CoachEmail      UTC
  Suspicious                        SuspiciousInterviewer (one row per mark)     CreatedBy        app server clock (UTC)
  SuspiciousUndone / Blocked /      InterviewerActionLog, Metric = ActionType;   ActedBy          UTC
    Unblocked / AccessRevoked /       legacy actor 'Admin' excluded
    AccessRestored / Inactivated /
    Reactivated / PaymentHold /
    PaymentHoldReleased
  DeductionReview                   InterviewerActionLog ActionType DeductionReview: ActedBy          UTC
                                    a QC decision on an interviewer's deduction
                                    review request (usp_DecideDeductionReviewRequest)
  ReportEdited                      ReportEditLog (RefId = log Id), and for the  EditedBy         UTC
                                    part of the range before the first log row,  PerformedBy      IST
                                    InterviewCommentsHistory (comment changed
                                    by someone other than the interviewer)

Status-change and report-edit rows carry a readable action label in [Status]
("Payment put on hold", "Edited interviewer comment (n to m chars)") and the
reason or the before / after excerpts in Detail.

PenaltyApplied is ReportReviewed with Amount > 0 (derived by the readers).
Group keys (one action with its undo): StatusActions (all ten status metrics),
SuspiciousAll, BlockedAll, AccessAll, ActiveAll, PaymentHoldAll.
@Metric narrows the sources read (NULL / 'All' = every source); unknown keys
collect nothing. @ActorEmail narrows to one user. @InterviewerEmail narrows to
rows about one interviewer (backfill runs are skipped: they are per IRF).

Optional tables/columns (TrialFlags, both backfill-run tables,
TransInterviewerAuditLog, InterviewerActionLog, ReportEditLog,
#QCActivityRows.RefId, CallRecordingData.RecipientKind / HangupCause) are
read through sp_executesql behind OBJECT_ID / COL_LENGTH checks so the
procedure still runs where they are missing.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_QCActivity_CollectRows]
    @FromIst    DATE,
    @ToIst      DATE,
    @ActorEmail VARCHAR(150) = NULL,
    @Metric     VARCHAR(40)  = NULL,
    @InterviewerEmail VARCHAR(150) = NULL   -- only rows about this interviewer (Interviewer QC View side panel)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'tempdb..#QCActivityRows') IS NULL
        RETURN;

    IF @FromIst IS NULL OR @ToIst IS NULL OR @FromIst > @ToIst
        RETURN;

    SET @ActorEmail = NULLIF(LTRIM(RTRIM(@ActorEmail)), '');
    SET @Metric     = NULLIF(LTRIM(RTRIM(@Metric)), '');
    IF @Metric = 'All'
        SET @Metric = NULL;

    -- Inclusive IST calendar dates -> half-open IST window -> same window in UTC.
    DECLARE @FromIstDt DATETIME = CAST(@FromIst AS DATETIME);
    DECLARE @ToIstDt   DATETIME = DATEADD(DAY, 1, CAST(@ToIst AS DATETIME));
    DECLARE @FromUtc   DATETIME = DATEADD(MINUTE, -330, @FromIstDt);
    DECLARE @ToUtc     DATETIME = DATEADD(MINUTE, -330, @ToIstDt);

    DECLARE @WantReports BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('ReportReviewed', 'PenaltyApplied') THEN 1 ELSE 0 END;
    DECLARE @WantSla     BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('SlaApproved', 'SlaNotApproved') THEN 1 ELSE 0 END;
    DECLARE @WantQcForm  BIT = CASE WHEN @Metric IS NULL OR @Metric = 'QCForm' THEN 1 ELSE 0 END;
    DECLARE @WantTrial   BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('TrialKept', 'TrialRemoved', 'Trial') THEN 1 ELSE 0 END;
    DECLARE @WantBackfill BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('AuditBackfill', 'AuditActions') THEN 1 ELSE 0 END;
    DECLARE @WantAuditLog BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('AuditKept', 'AuditRemoved', 'AuditActions') THEN 1 ELSE 0 END;
    DECLARE @WantTrm     BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('TrmUnderReview', 'TrmReviewed', 'TrmRemoved', 'Trm') THEN 1 ELSE 0 END;
    DECLARE @WantTi      BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('TiUnderReview', 'TiReviewed', 'TiRemoved', 'Ti') THEN 1 ELSE 0 END;
    DECLARE @WantPlivo   BIT = CASE WHEN @Metric IS NULL OR @Metric = 'PlivoCall' THEN 1 ELSE 0 END;
    DECLARE @WantCoaching BIT = CASE WHEN @Metric IS NULL OR @Metric = 'Coaching' THEN 1 ELSE 0 END;
    DECLARE @WantSuspicious BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('Suspicious', 'SuspiciousAll', 'StatusActions') THEN 1 ELSE 0 END;
    DECLARE @WantActionLog BIT = CASE WHEN @Metric IS NULL OR @Metric IN (
        'SuspiciousUndone', 'Blocked', 'Unblocked', 'AccessRevoked', 'AccessRestored', 'Inactivated', 'Reactivated',
        'PaymentHold', 'PaymentHoldReleased', 'DeductionReview',
        'StatusActions', 'SuspiciousAll', 'BlockedAll', 'AccessAll', 'ActiveAll', 'PaymentHoldAll') THEN 1 ELSE 0 END;
    DECLARE @WantReportEdit BIT = CASE WHEN @Metric IS NULL OR @Metric = 'ReportEdited' THEN 1 ELSE 0 END;

    -- Interviewer filter: rows keyed by interviewer email use @InterviewerEmail, rows keyed by UserList.id use @IvUserId.
    -- An unknown email leaves @IvUserId NULL, so the id-keyed sources return nothing for it.
    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');
    DECLARE @IvUserId INT = NULL;
    IF @InterviewerEmail IS NOT NULL
    BEGIN
        SET @IvUserId = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @InterviewerEmail ORDER BY id DESC);
        SET @WantBackfill = 0; -- backfill runs are per IRF, never about one interviewer
    END

    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @Params NVARCHAR(400) = N'@FromUtc DATETIME, @ToUtc DATETIME, @ActorEmail VARCHAR(150), @InterviewerEmail VARCHAR(150), @IvUserId INT';

    /* ---------------------------------------------------------------
       1. Reports reviewed (Payment SLA saves). One row per interview per
          user: that user's latest save in the range. Amount = deduction %.
          Two sources, because usp_AddorUpdatePaymentSLADetails writes
          PaymentSLADetails (and so PaymentSLADetailsHistory) only when
          @Rating < 5, but inserts ClientCommentsForReport on every rated
          save:
            - PaymentSLADetailsHistory  (human saves; exact deduction)
            - ClientCommentsForReport   (every rated save; the deduction
              comes from the current PaymentSLADetails row)
          Clients also rate reports, so users whose profile is Client are
          excluded, as are AISystem and System.
       --------------------------------------------------------------- */
    IF @WantReports = 1
    BEGIN
        ;WITH Raw AS (
            SELECT
                h.id                        AS SrcId,
                0                           AS SrcRank,     -- history wins a same-time tie: its deduction is exact
                h.Interviewid,
                LTRIM(RTRIM(h.CreatedBy))   AS Actor,
                h.CreatedOn                 AS SavedAt,
                h.PaymentDeduction,
                h.Recommendation,
                CAST(NULL AS INT)           AS Rating
            FROM dbo.PaymentSLADetailsHistory h
            WHERE h.CreatedOn >= @FromUtc
              AND h.CreatedOn <  @ToUtc
              AND h.Interviewid IS NOT NULL
              AND NULLIF(LTRIM(RTRIM(h.CreatedBy)), '') IS NOT NULL
              AND LTRIM(RTRIM(h.CreatedBy)) NOT IN ('AISystem', 'System')
              AND (@ActorEmail IS NULL OR LTRIM(RTRIM(h.CreatedBy)) = @ActorEmail)

            UNION ALL

            SELECT
                c.id,
                1,
                c.InterviewId,
                LTRIM(RTRIM(c.CreatedBy)),
                c.CreatedAt,
                ps.PaymentDeduction,
                ps.Recommendation,
                c.Rating
            FROM dbo.ClientCommentsForReport c
            OUTER APPLY (SELECT TOP 1 p.PaymentDeduction, p.Recommendation FROM dbo.PaymentSLADetails p
                         WHERE p.Interviewid = c.InterviewId ORDER BY p.id DESC) ps
            WHERE c.CreatedAt >= @FromUtc
              AND c.CreatedAt <  @ToUtc
              AND c.Rating IS NOT NULL
              AND NULLIF(LTRIM(RTRIM(c.CreatedBy)), '') IS NOT NULL
              AND LTRIM(RTRIM(c.CreatedBy)) NOT IN ('AISystem', 'System')
              AND (@ActorEmail IS NULL OR LTRIM(RTRIM(c.CreatedBy)) = @ActorEmail)
        ),
        Saves AS (
            SELECT
                r.*,
                -- The reviewer's latest rating on this interview in the range (NULL when only history rows exist).
                FIRST_VALUE(r.Rating) OVER (
                    PARTITION BY r.Actor, r.Interviewid
                    ORDER BY CASE WHEN r.Rating IS NULL THEN 1 ELSE 0 END, r.SavedAt DESC, r.SrcId DESC
                    ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS LatestRating,
                ROW_NUMBER() OVER (
                    PARTITION BY r.Actor, r.Interviewid
                    ORDER BY r.SavedAt DESC, r.SrcRank, r.SrcId DESC) AS rn
            FROM Raw r
            WHERE (@InterviewerEmail IS NULL OR EXISTS (SELECT 1 FROM dbo.InterviewMeetings fm
                                                        WHERE fm.id = r.Interviewid AND fm.interviewer = @InterviewerEmail))
              AND NOT EXISTS (SELECT 1 FROM dbo.UserList cu
                              INNER JOIN dbo.[Profile] cp ON cp.id = cu.idProfile
                              WHERE cu.EmailId = r.Actor AND cp.[Name] = 'Client')
        )
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, Detail, SourceId)
        SELECT
            LEFT(s.Actor, 150),
            'ReportReviewed',
            DATEADD(MINUTE, 330, s.SavedAt),
            s.Interviewid,
            irf.id,
            im.interviewer,
            ISNULL(s.PaymentDeduction, 0),
            LEFT(NULLIF(LTRIM(RTRIM(ISNULL(s.Recommendation, '')
                + CASE WHEN s.LatestRating IS NOT NULL
                       THEN ' (rated ' + CAST(s.LatestRating AS VARCHAR(10)) + '/5)' ELSE '' END)), ''), 500),
            s.SrcId
        FROM Saves s
        LEFT JOIN dbo.InterviewMeetings im ON im.id = s.Interviewid
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE s.rn = 1;
    END

    /* ---------------------------------------------------------------
       2. Payment SLA approved / not approved (current state, ModifiedBy).
       --------------------------------------------------------------- */
    IF @WantSla = 1
    BEGIN
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(ps.ModifiedBy)), 150),
            CASE ps.IsApproved WHEN 1 THEN 'SlaApproved' ELSE 'SlaNotApproved' END,
            DATEADD(MINUTE, 330, ps.ModifiedOn),
            ps.Interviewid,
            irf.id,
            im.interviewer,
            ISNULL(ps.PaymentDeduction, 0),
            ps.Recommendation,
            ps.id
        FROM dbo.PaymentSLADetails ps
        LEFT JOIN dbo.InterviewMeetings im ON im.id = ps.Interviewid
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE ps.IsApproved IN (1, 2)
          AND ps.ModifiedOn >= @FromUtc
          AND ps.ModifiedOn <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(ps.ModifiedBy)), '') IS NOT NULL
          AND LTRIM(RTRIM(ps.ModifiedBy)) NOT IN ('AISystem', 'System')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(ps.ModifiedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail)
          AND (@Metric IS NULL
               OR (@Metric = 'SlaApproved'    AND ps.IsApproved = 1)
               OR (@Metric = 'SlaNotApproved' AND ps.IsApproved = 2));
    END

    /* ---------------------------------------------------------------
       3. QC forms (red flags). Amount = red flags on the form.
       --------------------------------------------------------------- */
    IF @WantQcForm = 1
    BEGIN
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(ul.EmailId)), 150),
            'QCForm',
            DATEADD(MINUTE, 330, q.Addedon),
            q.Interviewid,
            irf.id,
            im.interviewer,
            ISNULL(q.Redflags, 0),
            q.id
        FROM dbo.Trans_QCFormReport q
        INNER JOIN dbo.UserList ul ON ul.id = q.Userid
        LEFT JOIN dbo.InterviewMeetings im ON im.id = q.Interviewid
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE q.Addedon >= @FromUtc
          AND q.Addedon <  @ToUtc
          AND (@ActorEmail IS NULL OR ul.EmailId = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail);
    END

    /* ---------------------------------------------------------------
       4. Trial interviewers kept / removed (TrialFlags, optional table).
       --------------------------------------------------------------- */
    IF @WantTrial = 1 AND OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerUserId, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(tf.ReviewedBy)), 150),
            CASE WHEN LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''kept'' THEN ''TrialKept'' ELSE ''TrialRemoved'' END,
            DATEADD(MINUTE, 330, tf.ReviewedAtUtc),
            tf.TriggeringInterviewId,
            tf.IrfId,
            iv.UserId,
            LEFT(tf.ReviewerComment, 500),
            LEFT(tf.OverallVerdict, 150),
            tf.Id
        FROM dbo.TrialFlags tf
        LEFT JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
        WHERE LOWER(LTRIM(RTRIM(tf.ReviewStatus))) IN (''kept'', ''removed'')
          AND tf.ReviewedAtUtc >= @FromUtc
          AND tf.ReviewedAtUtc <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(tf.ReviewedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(tf.ReviewedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(tf.ReviewedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR iv.UserId = @IvUserId)';

        IF @Metric = 'TrialKept'
            SET @Sql += N' AND LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''kept''';
        ELSE IF @Metric = 'TrialRemoved'
            SET @Sql += N' AND LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''removed''';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       5. IRF audit backfill runs (both run tables are optional).
       --------------------------------------------------------------- */
    IF @WantBackfill = 1 AND OBJECT_ID(N'dbo.IrfAuditRiskBackfillRun', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, IrfId, Amount, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(r.ExecutedBy)), 150),
            ''AuditBackfill'',
            DATEADD(MINUTE, 330, CAST(r.ExecutedAtUtc AS DATETIME)),
            r.IrfId,
            r.Processed,
            LEFT(N''Risk backfill '' + CONVERT(NVARCHAR(10), r.FromIst, 23) + N'' to '' + CONVERT(NVARCHAR(10), r.ToIst, 23)
                + N'': scanned '' + CAST(r.Scanned AS NVARCHAR(10)) + N'', processed '' + CAST(r.Processed AS NVARCHAR(10)), 500),
            r.Id
        FROM dbo.IrfAuditRiskBackfillRun r
        WHERE r.ExecutedAtUtc >= @FromUtc
          AND r.ExecutedAtUtc <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(r.ExecutedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(r.ExecutedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(r.ExecutedBy)) = @ActorEmail);';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    IF @WantBackfill = 1 AND OBJECT_ID(N'dbo.AutoQcIrfBackfillRun', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, IrfId, Amount, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(r.ExecutedBy)), 150),
            ''AuditBackfill'',
            DATEADD(MINUTE, 330, r.ExecutedAtUtc),
            r.IrfId,
            r.NewFlagsSet,
            LEFT(N''Auto-QC backfill: scanned '' + CAST(r.VerdictsScanned AS NVARCHAR(10))
                + N'', new flags '' + CAST(r.NewFlagsSet AS NVARCHAR(10))
                + N'', already flagged '' + CAST(r.AlreadyFlaggedSkipped AS NVARCHAR(10)), 500),
            r.Id
        FROM dbo.AutoQcIrfBackfillRun r
        WHERE r.ExecutedAtUtc >= @FromUtc
          AND r.ExecutedAtUtc <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(r.ExecutedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(r.ExecutedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(r.ExecutedBy)) = @ActorEmail);';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       6. IRF audit keep / remove decisions (TransInterviewerAuditLog).
       --------------------------------------------------------------- */
    IF @WantAuditLog = 1 AND OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerUserId, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(l.ChangedBy)), 150),
            CASE WHEN l.EventType = ''RiskAuditKept'' THEN ''AuditKept'' ELSE ''AuditRemoved'' END,
            DATEADD(MINUTE, 330, l.ChangedAt),
            l.TriggeringInterviewId,
            t.IRFId,
            t.UserId,
            LEFT(l.ReasonCategory, 500),
            LEFT(l.ToValue, 150),
            l.Id
        FROM dbo.TransInterviewerAuditLog l
        INNER JOIN dbo.TransInterviewer t ON t.id = l.TransInterviewerId
        WHERE l.EventType IN (''RiskAuditKept'', ''RiskAuditRemoval'')
          AND l.ChangedAt >= @FromUtc
          AND l.ChangedAt <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(l.ChangedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(l.ChangedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(l.ChangedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR t.UserId = @IvUserId)';

        IF @Metric = 'AuditKept'
            SET @Sql += N' AND l.EventType = ''RiskAuditKept''';
        ELSE IF @Metric = 'AuditRemoved'
            SET @Sql += N' AND l.EventType = ''RiskAuditRemoval''';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       7. Total Red Marker (Type IRF) and Total Interviews (Type
          TotalInterview) status moves. CreatedOn is stored in IST.
          A removal (Status 3) counts once per user, interviewer, IRF and type:
          moving a row to Removed and then pressing Delete on the Total Red
          Marker page both log Status 3; only the latest is kept.
       --------------------------------------------------------------- */
    IF @WantTrm = 1 OR @WantTi = 1
    BEGIN
        ;WITH Moves AS (
        SELECT
            r.id, r.CreatedBy, r.[Type], r.[Status], r.CreatedOn, r.IRFId, r.InterviewerUserid, r.Comment,
            ROW_NUMBER() OVER (
                PARTITION BY LTRIM(RTRIM(r.CreatedBy)), r.InterviewerUserid, r.IRFId, r.[Type],
                             CASE WHEN r.[Status] = 3 THEN -1 ELSE r.id END
                ORDER BY r.CreatedOn DESC, r.id DESC) AS rn
        FROM dbo.RedMarkerStatusDetails r
        WHERE r.[Type] IN ('IRF', 'TotalInterview')
          AND r.[Status] IN (1, 2, 3)
          AND r.CreatedOn >= @FromIstDt
          AND r.CreatedOn <  @ToIstDt
          AND NULLIF(LTRIM(RTRIM(r.CreatedBy)), '') IS NOT NULL
          AND LTRIM(RTRIM(r.CreatedBy)) NOT IN ('AISystem', 'System')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(r.CreatedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR r.InterviewerUserid = @IvUserId)
          AND ((@WantTrm = 1 AND r.[Type] = 'IRF') OR (@WantTi = 1 AND r.[Type] = 'TotalInterview'))
          AND (@Metric IS NULL OR @Metric IN ('Trm', 'Ti')
               OR (@Metric IN ('TrmUnderReview', 'TiUnderReview') AND r.[Status] = 1)
               OR (@Metric IN ('TrmReviewed', 'TiReviewed')       AND r.[Status] = 2)
               OR (@Metric IN ('TrmRemoved', 'TiRemoved')         AND r.[Status] = 3))
        )
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, IrfId, InterviewerUserId, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(m.CreatedBy)), 150),
            CASE WHEN m.[Type] = 'IRF' THEN 'Trm' ELSE 'Ti' END
                + CASE m.[Status] WHEN 1 THEN 'UnderReview' WHEN 2 THEN 'Reviewed' ELSE 'Removed' END,
            m.CreatedOn,
            NULLIF(m.IRFId, 0),
            m.InterviewerUserid,
            LEFT(m.Comment, 500),
            m.id
        FROM Moves m
        WHERE m.rn = 1;
    END

    /* ---------------------------------------------------------------
       8. Plivo calls to interviewers. Plivo CallSids are UUIDs (contain
          dashes) and have no Twilio RecordingSid. Amount = talk seconds
          (NULL when no callback data arrived).
       --------------------------------------------------------------- */
    IF @WantPlivo = 1
    BEGIN
        DECLARE @HasRecipientKind BIT = CASE WHEN COL_LENGTH('dbo.CallRecordingData', 'RecipientKind') IS NOT NULL THEN 1 ELSE 0 END;
        DECLARE @HasHangupCause   BIT = CASE WHEN COL_LENGTH('dbo.CallRecordingData', 'HangupCause') IS NOT NULL THEN 1 ELSE 0 END;

        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, [Status], Phone, RecordingUrl, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(c.Caller)), 150),
            ''PlivoCall'',
            DATEADD(MINUTE, 330, c.DateCreated),
            NULLIF(c.InterviewId, 0),
            NULLIF(TRY_CAST(c.IRFId AS INT), 0),
            c.CallRecipient,
            TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '''') AS INT),
            LEFT(NULLIF(LTRIM(RTRIM(ISNULL(c.[Status], '''')';

        IF @HasHangupCause = 1
            SET @Sql += N'
                + CASE WHEN NULLIF(LTRIM(RTRIM(c.HangupCause)), '''') IS NOT NULL
                       THEN CASE WHEN NULLIF(LTRIM(RTRIM(c.[Status])), '''') IS NOT NULL THEN '' | '' ELSE '''' END + LTRIM(RTRIM(c.HangupCause))
                       ELSE '''' END';

        SET @Sql += N')), ''''), 150),
            LEFT(CAST(c.RecipientPhoneNumber AS VARCHAR(30)), 30),
            NULLIF(LTRIM(RTRIM(c.RecordingUri)), ''''),
            c.id
        FROM dbo.CallRecordingData c
        WHERE c.DateCreated >= @FromUtc
          AND c.DateCreated <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(c.Caller)), '''') IS NOT NULL
          AND (@ActorEmail IS NULL OR c.Caller = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR c.CallRecipient = @InterviewerEmail)
          AND c.CallSid LIKE ''%-%-%-%-%''
          AND c.RecordingSid IS NULL';

        IF @HasRecipientKind = 1
            SET @Sql += N'
          AND (c.RecipientProfileType = ''Interviewer'' OR c.RecipientKind = ''Interviewer'')';
        ELSE
            SET @Sql += N'
          AND c.RecipientProfileType = ''Interviewer''';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       9. Coaching sessions (InterviewerCoachingSession, optional table).
          Status = channel, Detail = outcome + what was agreed,
          Amount / RecordingUrl = the linked call's talk time / recording.
       --------------------------------------------------------------- */
    IF @WantCoaching = 1 AND OBJECT_ID(N'dbo.InterviewerCoachingSession', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewerUserId, InterviewerEmail, Amount, Detail, [Status], RecordingUrl, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(s.CoachEmail)), 150),
            ''Coaching'',
            DATEADD(MINUTE, 330, s.CreatedAtUtc),
            s.InterviewerUserId,
            s.InterviewerEmail,
            TRY_CAST(NULLIF(LTRIM(RTRIM(c.Duration)), '''') AS INT),
            LEFT(s.Outcome + ISNULL(N'' - '' + NULLIF(LTRIM(RTRIM(s.Agreed)), N''''), N''''), 500),
            LEFT(s.Channel, 150),
            NULLIF(LTRIM(RTRIM(c.RecordingUri)), ''''),
            s.Id
        FROM dbo.InterviewerCoachingSession s
        OUTER APPLY (SELECT TOP 1 cr.Duration, cr.RecordingUri FROM dbo.CallRecordingData cr
                     WHERE s.CallSid IS NOT NULL AND cr.CallSid = s.CallSid ORDER BY cr.id DESC) c
        WHERE s.CreatedAtUtc >= @FromUtc
          AND s.CreatedAtUtc <  @ToUtc
          AND (@ActorEmail IS NULL OR s.CoachEmail = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR s.InterviewerUserId = @IvUserId);';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       10. Marked suspicious (SuspiciousInterviewer, one row per mark).
           Createddate is the app server's DateTime.Now, treated as UTC
           like the other server-clock sources. Detail = the comment.
       --------------------------------------------------------------- */
    IF @WantSuspicious = 1 AND OBJECT_ID(N'dbo.SuspiciousInterviewer', N'U') IS NOT NULL
    BEGIN
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewerUserId, InterviewerEmail, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(s.CreatedBy)), 150),
            'Suspicious',
            DATEADD(MINUTE, 330, s.Createddate),
            s.UserId,
            u.EmailId,
            LEFT(NULLIF(LTRIM(RTRIM(s.Comment)), ''), 500),
            'Marked suspicious',
            s.Id
        FROM dbo.SuspiciousInterviewer s
        OUTER APPLY (SELECT TOP 1 ul.EmailId FROM dbo.UserList ul WHERE ul.id = s.UserId) u
        WHERE s.Createddate >= @FromUtc
          AND s.Createddate <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(s.CreatedBy)), '') IS NOT NULL
          AND LTRIM(RTRIM(s.CreatedBy)) NOT IN ('AISystem', 'System')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(s.CreatedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR s.UserId = @IvUserId);
    END

    /* ---------------------------------------------------------------
       11. Interviewer status changes (InterviewerActionLog, optional
           table). Metric = ActionType, [Status] = readable label,
           Detail = reason. The legacy actor 'Admin' (flag toggles before
           the actor was passed) and system actors are excluded.
       --------------------------------------------------------------- */
    IF @WantActionLog = 1 AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewerUserId, InterviewerEmail, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(l.ActedBy)), 150),
            l.ActionType,
            DATEADD(MINUTE, 330, l.ActedAtUtc),
            l.InterviewerUserId,
            l.InterviewerEmail,
            LEFT(NULLIF(LTRIM(RTRIM(l.Reason)), N''''), 500),
            CASE l.ActionType
                WHEN ''SuspiciousUndone''    THEN ''Suspicious mark undone''
                WHEN ''Blocked''             THEN ''Blocked''
                WHEN ''Unblocked''           THEN ''Unblocked''
                WHEN ''AccessRevoked''       THEN ''Portal access turned off''
                WHEN ''AccessRestored''      THEN ''Portal access turned on''
                WHEN ''Inactivated''         THEN ''Made inactive''
                WHEN ''Reactivated''         THEN ''Made active''
                WHEN ''PaymentHold''         THEN ''Payment put on hold''
                WHEN ''PaymentHoldReleased'' THEN ''Payment hold released''
                WHEN ''DeductionReview''     THEN ''Deduction review decided''
            END,
            l.Id
        FROM dbo.InterviewerActionLog l
        WHERE l.ActedAtUtc >= @FromUtc
          AND l.ActedAtUtc <  @ToUtc
          AND l.ActionType IN (''SuspiciousUndone'', ''Blocked'', ''Unblocked'', ''AccessRevoked'', ''AccessRestored'',
                               ''Inactivated'', ''Reactivated'', ''PaymentHold'', ''PaymentHoldReleased'',
                               ''DeductionReview'')
          AND NULLIF(LTRIM(RTRIM(l.ActedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(l.ActedBy)) NOT IN (''Admin'', ''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(l.ActedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR l.InterviewerUserId = @IvUserId
               OR (l.InterviewerUserId IS NULL AND l.InterviewerEmail = @InterviewerEmail))';

        IF @Metric IN ('SuspiciousUndone', 'Blocked', 'Unblocked', 'AccessRevoked', 'AccessRestored',
                       'Inactivated', 'Reactivated', 'PaymentHold', 'PaymentHoldReleased', 'DeductionReview')
            SET @Sql += N' AND l.ActionType = @Metric';
        ELSE IF @Metric = 'StatusActions'
            SET @Sql += N' AND l.ActionType <> ''DeductionReview''';
        ELSE IF @Metric = 'SuspiciousAll'
            SET @Sql += N' AND l.ActionType = ''SuspiciousUndone''';
        ELSE IF @Metric = 'BlockedAll'
            SET @Sql += N' AND l.ActionType IN (''Blocked'', ''Unblocked'')';
        ELSE IF @Metric = 'AccessAll'
            SET @Sql += N' AND l.ActionType IN (''AccessRevoked'', ''AccessRestored'')';
        ELSE IF @Metric = 'ActiveAll'
            SET @Sql += N' AND l.ActionType IN (''Inactivated'', ''Reactivated'')';
        ELSE IF @Metric = 'PaymentHoldAll'
            SET @Sql += N' AND l.ActionType IN (''PaymentHold'', ''PaymentHoldReleased'')';

        EXEC sp_executesql @Sql, N'@FromUtc DATETIME, @ToUtc DATETIME, @ActorEmail VARCHAR(150), @InterviewerEmail VARCHAR(150), @IvUserId INT, @Metric VARCHAR(40)',
            @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId, @Metric = @Metric;
    END

    /* ---------------------------------------------------------------
       12. Report edits (ReportEditLog, optional table). [Status] =
           "Edited interviewer comment (n to m chars)", Detail = the first
           230 characters before and after, RefId = the log Id (the
           dashboard's "View change"). RefId is written only when the
           caller's #QCActivityRows has that column.
       --------------------------------------------------------------- */
    DECLARE @FirstEditLogUtc DATETIME = NULL;
    IF @WantReportEdit = 1 AND OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'SELECT @First = MIN(EditedAtUtc) FROM dbo.ReportEditLog;',
            N'@First DATETIME OUTPUT', @First = @FirstEditLogUtc OUTPUT;

        DECLARE @HasRefId BIT = CASE WHEN COL_LENGTH('tempdb..#QCActivityRows', 'RefId') IS NOT NULL THEN 1 ELSE 0 END;

        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Detail, [Status], SourceId'
            + CASE WHEN @HasRefId = 1 THEN N', RefId' ELSE N'' END + N')
        SELECT
            LEFT(LTRIM(RTRIM(l.EditedBy)), 150),
            ''ReportEdited'',
            DATEADD(MINUTE, 330, l.EditedAtUtc),
            l.InterviewId,
            irf.id,
            im.interviewer,
            LEFT(N''Before: '' + ISNULL(LEFT(l.OldValue, 230) + CASE WHEN LEN(l.OldValue) > 230 THEN NCHAR(8230) ELSE N'''' END, N''(empty)'')
                + N'' | After: '' + ISNULL(LEFT(l.NewValue, 230) + CASE WHEN LEN(l.NewValue) > 230 THEN NCHAR(8230) ELSE N'''' END, N''(empty)''), 500),
            LEFT(CASE WHEN l.FieldName = ''InterviewerComment'' THEN ''Edited interviewer comment'' ELSE ''Edited '' + l.FieldName END
                + '' ('' + CAST(LEN(ISNULL(l.OldValue, N'''')) AS VARCHAR(20)) + '' to ''
                + CAST(LEN(ISNULL(l.NewValue, N'''')) AS VARCHAR(20)) + '' chars)'', 150),
            l.Id'
            + CASE WHEN @HasRefId = 1 THEN N',
            l.Id' ELSE N'' END + N'
        FROM dbo.ReportEditLog l
        LEFT JOIN dbo.InterviewMeetings im ON im.id = l.InterviewId
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE l.EditedAtUtc >= @FromUtc
          AND l.EditedAtUtc <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(l.EditedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(l.EditedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(l.EditedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail);';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       13. Report edits before ReportEditLog existed, rebuilt from
           InterviewCommentsHistory (one snapshot per InterviewComments
           update; PerformedAt is IST, PerformedBy is the row's
           ModifiedBy). Only the part of the range before the first
           ReportEditLog row is read, so nothing is counted twice.
           A snapshot counts when
             - the saver is not the interview's interviewer (by email or
               by coded name),
             - a previous snapshot exists with a non-empty comment, and
             - the comment differs from that previous snapshot.
           No RefId: these rows have no full before / after text.
       --------------------------------------------------------------- */
    IF @WantReportEdit = 1 AND OBJECT_ID(N'dbo.InterviewCommentsHistory', N'U') IS NOT NULL
    BEGIN
        DECLARE @HistToIstDt DATETIME = @ToIstDt;
        IF @FirstEditLogUtc IS NOT NULL AND DATEADD(MINUTE, 330, @FirstEditLogUtc) < @HistToIstDt
            SET @HistToIstDt = DATEADD(MINUTE, 330, @FirstEditLogUtc);

        IF @HistToIstDt > @FromIstDt
        BEGIN
            ;WITH Saves AS (
                SELECT h.id, h.InterviewId, h.InterviewerComment, h.PerformedAt, LTRIM(RTRIM(h.PerformedBy)) AS Actor
                FROM dbo.InterviewCommentsHistory h
                WHERE h.PerformedAt >= @FromIstDt
                  AND h.PerformedAt <  @HistToIstDt
                  AND NULLIF(LTRIM(RTRIM(h.PerformedBy)), '') IS NOT NULL
                  AND LTRIM(RTRIM(h.PerformedBy)) NOT IN ('AISystem', 'System')
                  AND (@ActorEmail IS NULL OR LTRIM(RTRIM(h.PerformedBy)) = @ActorEmail)
            )
            INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Detail, [Status], SourceId)
            SELECT
                LEFT(s.Actor, 150),
                'ReportEdited',
                s.PerformedAt,
                s.InterviewId,
                irf.id,
                im.interviewer,
                LEFT(N'Before: ' + LEFT(CAST(prev.InterviewerComment AS NVARCHAR(MAX)), 230)
                        + CASE WHEN LEN(prev.InterviewerComment) > 230 THEN NCHAR(8230) ELSE N'' END
                    + N' | After: ' + ISNULL(LEFT(CAST(s.InterviewerComment AS NVARCHAR(MAX)), 230)
                        + CASE WHEN LEN(s.InterviewerComment) > 230 THEN NCHAR(8230) ELSE N'' END, N'(empty)'), 500),
                'Edited interviewer comment (' + CAST(LEN(prev.InterviewerComment) AS VARCHAR(20)) + ' to '
                    + CAST(LEN(ISNULL(s.InterviewerComment, '')) AS VARCHAR(20)) + ' chars)',
                s.id
            FROM Saves s
            INNER JOIN dbo.InterviewMeetings im ON im.id = s.InterviewId
            CROSS APPLY (SELECT TOP 1 p.InterviewerComment FROM dbo.InterviewCommentsHistory p
                         WHERE p.InterviewId = s.InterviewId AND p.PerformedAt < s.PerformedAt
                         ORDER BY p.PerformedAt DESC) prev
            OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
            WHERE NULLIF(LTRIM(RTRIM(prev.InterviewerComment)), '') IS NOT NULL
              AND (s.InterviewerComment IS NULL
                   OR DATALENGTH(s.InterviewerComment) <> DATALENGTH(prev.InterviewerComment)
                   OR s.InterviewerComment COLLATE Latin1_General_BIN2 <> prev.InterviewerComment COLLATE Latin1_General_BIN2)
              AND ISNULL(LTRIM(RTRIM(im.interviewer)), '') <> s.Actor
              AND NOT EXISTS (SELECT 1 FROM dbo.InterviewerProfile ip
                              INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
                              WHERE ul.EmailId = s.Actor
                                AND LTRIM(RTRIM(ip.NickName)) = LTRIM(RTRIM(im.interviewer)))
              AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail);
        END
    END
END
GO
