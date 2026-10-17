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
        RefId             INT           NULL,   -- optional column; filled only when the caller has it
        -- optional authenticity columns (part 2); section 16 fills them when present:
        Comment           NVARCHAR(500) NULL,
        HasEvidence       BIT           NULL,
        IsAuthentic       BIT           NULL,
        PageGroup         VARCHAR(30)   NULL
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
  PrUnderReview/PrReviewed/PrRemoved    RedMarkerStatusDetails Type PotentialRisk  CreatedBy      IST
  HrUnderReview/HrReviewed/HrRemoved    RedMarkerStatusDetails Type HighRisk       CreatedBy      IST
  PliUnderReview/PliReviewed/PliRemoved RedMarkerStatusDetails Type PLI            CreatedBy      IST
  MonitoringRemoved / MonitoringShortlisted  TransInterviewerAuditLog EventType   ChangedBy        UTC
                                    MonitoringRemoved / MonitoringShortlisted
                                    (Interviewer Monitoring Remove / Shortlist)
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

  ProxyForm                         InterviewerActionLog ProxyForm (every save),  ActedBy /        UTC
                                    else ProxyReport first save                  CreatedBy
  RedoRequested                     InterviewerActionLog RedoRequested (every     ActedBy /        UTC
                                    save), else RedoInterviews first save        CreatedBy
  TrialBackfill   (Amount = flags)  TrialFlagBackfillRun (one row per run)       ExecutedBy       UTC

Part 2 group keys (collect every source, the readers filter): PageTotalInterviews,
PageTrm, PageIrfAudit, PageTrial, PageSuspicious, PageReportsSla, PagePotentialRisk,
PageHighRisk, PageMonitoring, ScoredActions,
AuthenticActions, NotAuthentic, NoEvidence, FollowThrough, Reached; ConnectedCall
collects calls only. Section 16 explains PageGroup / Comment / HasEvidence / IsAuthentic.

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

    -- Part 2 group keys. Page and authenticity keys read every source; the readers filter on
    -- PageGroup / IsAuthentic / HasEvidence (filled in section 16). ConnectedCall only needs calls.
    IF @Metric IN ('PageTotalInterviews', 'PageTrm', 'PageIrfAudit', 'PageTrial', 'PageSuspicious', 'PageReportsSla',
                   'PagePotentialRisk', 'PageHighRisk', 'PageMonitoring',
                   'ScoredActions', 'AuthenticActions', 'NotAuthentic', 'NoEvidence', 'FollowThrough', 'Reached', 'ActionsTaken')
        SET @Metric = NULL;
    ELSE IF @Metric = 'ConnectedCall'
        SET @Metric = 'PlivoCall';

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
    -- QC page decisions (RedMarkerStatusDetails Type PotentialRisk / HighRisk / PLI)
    DECLARE @WantPr      BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('PrUnderReview', 'PrReviewed', 'PrRemoved', 'Pr') THEN 1 ELSE 0 END;
    DECLARE @WantHr      BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('HrUnderReview', 'HrReviewed', 'HrRemoved', 'Hr') THEN 1 ELSE 0 END;
    DECLARE @WantPli     BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('PliUnderReview', 'PliReviewed', 'PliRemoved', 'Pli') THEN 1 ELSE 0 END;
    DECLARE @WantMonitoring BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('MonitoringRemoved', 'MonitoringShortlisted', 'Monitoring') THEN 1 ELSE 0 END;
    DECLARE @WantPlivo   BIT = CASE WHEN @Metric IS NULL OR @Metric = 'PlivoCall' THEN 1 ELSE 0 END;
    DECLARE @WantCoaching BIT = CASE WHEN @Metric IS NULL OR @Metric = 'Coaching' THEN 1 ELSE 0 END;
    DECLARE @WantSuspicious BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('Suspicious', 'SuspiciousAll', 'StatusActions') THEN 1 ELSE 0 END;
    DECLARE @WantActionLog BIT = CASE WHEN @Metric IS NULL OR @Metric IN (
        'SuspiciousUndone', 'Blocked', 'Unblocked', 'AccessRevoked', 'AccessRestored', 'Inactivated', 'Reactivated',
        'PaymentHold', 'PaymentHoldReleased', 'DeductionReview',
        'StatusActions', 'SuspiciousAll', 'BlockedAll', 'AccessAll', 'ActiveAll', 'PaymentHoldAll') THEN 1 ELSE 0 END;
    DECLARE @WantReportEdit BIT = CASE WHEN @Metric IS NULL OR @Metric = 'ReportEdited' THEN 1 ELSE 0 END;
    DECLARE @WantProxy   BIT = CASE WHEN @Metric IS NULL OR @Metric = 'ProxyForm' THEN 1 ELSE 0 END;
    DECLARE @WantRedo    BIT = CASE WHEN @Metric IS NULL OR @Metric = 'RedoRequested' THEN 1 ELSE 0 END;
    DECLARE @WantTrialBackfill BIT = CASE WHEN @Metric IS NULL OR @Metric IN ('TrialBackfill', 'Trial') THEN 1 ELSE 0 END;

    -- Interviewer filter: rows keyed by interviewer email use @InterviewerEmail, rows keyed by UserList.id use @IvUserId.
    -- An unknown email leaves @IvUserId NULL, so the id-keyed sources return nothing for it.
    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');
    DECLARE @IvUserId INT = NULL;
    IF @InterviewerEmail IS NOT NULL
    BEGIN
        SET @IvUserId = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @InterviewerEmail ORDER BY id DESC);
        SET @WantBackfill = 0; -- backfill runs are per IRF, never about one interviewer
        SET @WantTrialBackfill = 0;
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
       2. Payment SLA approved / not approved.
          With PaymentSLADetailsHistory.IsApproved (tr_PaymentSLADetails copies it on every
          save): one row per save that moved the report to Approved (1) / Not approved (2),
          by the person who saved it, so a later save cannot erase an approval. Interviews
          with no history carrying IsApproved (saved before that column existed) fall back to
          the current state (ModifiedBy), as before.
       --------------------------------------------------------------- */
    IF @WantSla = 1 AND COL_LENGTH('dbo.PaymentSLADetailsHistory', 'IsApproved') IS NOT NULL
    BEGIN
        SET @Sql = N'
        ;WITH H AS (
            SELECT h.id, h.Interviewid, h.CreatedBy, h.CreatedOn, h.IsApproved, h.PaymentDeduction, h.Recommendation,
                   LAG(h.IsApproved) OVER (PARTITION BY h.Interviewid ORDER BY h.id) AS PrevApproved
            FROM dbo.PaymentSLADetailsHistory h
            WHERE h.Interviewid IS NOT NULL
              AND h.Interviewid IN (SELECT x.Interviewid FROM dbo.PaymentSLADetailsHistory x
                                    WHERE x.CreatedOn >= @FromUtc AND x.CreatedOn < @ToUtc AND x.IsApproved IN (1, 2))
        )
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(h.CreatedBy)), 150),
            CASE h.IsApproved WHEN 1 THEN ''SlaApproved'' ELSE ''SlaNotApproved'' END,
            DATEADD(MINUTE, 330, h.CreatedOn),
            h.Interviewid,
            irf.id,
            im.interviewer,
            ISNULL(h.PaymentDeduction, 0),
            h.Recommendation,
            h.id
        FROM H h
        LEFT JOIN dbo.InterviewMeetings im ON im.id = h.Interviewid
        OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
        WHERE h.IsApproved IN (1, 2)
          AND ISNULL(h.PrevApproved, -1) <> h.IsApproved
          AND h.CreatedOn >= @FromUtc
          AND h.CreatedOn <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(h.CreatedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(h.CreatedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(h.CreatedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail)';
        IF @Metric = 'SlaApproved'
            SET @Sql += N' AND h.IsApproved = 1';
        ELSE IF @Metric = 'SlaNotApproved'
            SET @Sql += N' AND h.IsApproved = 2';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;

        -- Legacy: interviews with no history row carrying IsApproved keep the current-state rule.
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Amount, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(ps.ModifiedBy)), 150),
            CASE ps.IsApproved WHEN 1 THEN ''SlaApproved'' ELSE ''SlaNotApproved'' END,
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
          AND NULLIF(LTRIM(RTRIM(ps.ModifiedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(ps.ModifiedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(ps.ModifiedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR im.interviewer = @InterviewerEmail)
          AND NOT EXISTS (SELECT 1 FROM dbo.PaymentSLADetailsHistory hx
                          WHERE hx.Interviewid = ps.Interviewid AND hx.IsApproved IS NOT NULL)';
        IF @Metric = 'SlaApproved'
            SET @Sql += N' AND ps.IsApproved = 1';
        ELSE IF @Metric = 'SlaNotApproved'
            SET @Sql += N' AND ps.IsApproved = 2';
        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END
    ELSE IF @WantSla = 1
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
       6b. Interviewer Monitoring (PQI) Remove / Shortlist on a channel
           (TransInterviewerAuditLog EventType MonitoringRemoved /
           MonitoringShortlisted, written by usp_updateInterviewerIRFStatusByIRFid
           with the actor and the required comment). Detail = the comment.
       --------------------------------------------------------------- */
    IF @WantMonitoring = 1 AND OBJECT_ID(N'dbo.TransInterviewerAuditLog', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, IrfId, InterviewerUserId, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(l.ChangedBy)), 150),
            l.EventType,
            DATEADD(MINUTE, 330, l.ChangedAt),
            t.IRFId,
            t.UserId,
            LEFT(l.ReasonCategory, 500),
            CASE l.EventType WHEN ''MonitoringRemoved'' THEN ''Removed from channel · Monitoring''
                             ELSE ''Shortlisted on channel · Monitoring'' END,
            l.Id
        FROM dbo.TransInterviewerAuditLog l
        INNER JOIN dbo.TransInterviewer t ON t.id = l.TransInterviewerId
        WHERE l.EventType IN (''MonitoringRemoved'', ''MonitoringShortlisted'')
          AND l.ChangedAt >= @FromUtc
          AND l.ChangedAt <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(l.ChangedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(l.ChangedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(l.ChangedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR t.UserId = @IvUserId)';

        IF @Metric IN ('MonitoringRemoved', 'MonitoringShortlisted')
            SET @Sql += N' AND l.EventType = ''' + @Metric + N'''';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       7. Total Red Marker (Type IRF) and Total Interviews (Type
          TotalInterview) status moves, and the QC page decisions on
          Potential Risk by Candidate (Type PotentialRisk), High-Risk
          Interviewers (Type HighRisk) and PLI (Type PLI). CreatedOn is IST.
          A removal (Status 3) counts once per user, interviewer, IRF and type:
          moving a row to Removed and then pressing Delete on the Total Red
          Marker page both log Status 3; only the latest is kept.
       --------------------------------------------------------------- */
    IF @WantTrm = 1 OR @WantTi = 1 OR @WantPr = 1 OR @WantHr = 1 OR @WantPli = 1
    BEGIN
        ;WITH Moves AS (
        SELECT
            r.id, r.CreatedBy, r.[Type], r.[Status], r.CreatedOn, r.IRFId, r.InterviewerUserid, r.Comment,
            ROW_NUMBER() OVER (
                PARTITION BY LTRIM(RTRIM(r.CreatedBy)), r.InterviewerUserid, r.IRFId, r.[Type],
                             CASE WHEN r.[Status] = 3 THEN -1 ELSE r.id END
                ORDER BY r.CreatedOn DESC, r.id DESC) AS rn
        FROM dbo.RedMarkerStatusDetails r
        WHERE r.[Type] IN ('IRF', 'TotalInterview', 'PotentialRisk', 'HighRisk', 'PLI')
          AND r.[Status] IN (1, 2, 3)
          AND r.CreatedOn >= @FromIstDt
          AND r.CreatedOn <  @ToIstDt
          AND NULLIF(LTRIM(RTRIM(r.CreatedBy)), '') IS NOT NULL
          AND LTRIM(RTRIM(r.CreatedBy)) NOT IN ('AISystem', 'System')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(r.CreatedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR r.InterviewerUserid = @IvUserId)
          AND ((@WantTrm = 1 AND r.[Type] = 'IRF') OR (@WantTi = 1 AND r.[Type] = 'TotalInterview')
               OR (@WantPr = 1 AND r.[Type] = 'PotentialRisk') OR (@WantHr = 1 AND r.[Type] = 'HighRisk')
               OR (@WantPli = 1 AND r.[Type] = 'PLI'))
          AND (@Metric IS NULL OR @Metric IN ('Trm', 'Ti', 'Pr', 'Hr', 'Pli')
               OR (@Metric IN ('TrmUnderReview', 'TiUnderReview', 'PrUnderReview', 'HrUnderReview', 'PliUnderReview') AND r.[Status] = 1)
               OR (@Metric IN ('TrmReviewed', 'TiReviewed', 'PrReviewed', 'HrReviewed', 'PliReviewed')            AND r.[Status] = 2)
               OR (@Metric IN ('TrmRemoved', 'TiRemoved', 'PrRemoved', 'HrRemoved', 'PliRemoved')                 AND r.[Status] = 3))
        )
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, IrfId, InterviewerUserId, Detail, SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(m.CreatedBy)), 150),
            CASE m.[Type] WHEN 'IRF' THEN 'Trm' WHEN 'PotentialRisk' THEN 'Pr' WHEN 'HighRisk' THEN 'Hr' WHEN 'PLI' THEN 'Pli' ELSE 'Ti' END
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
        -- [Status] names the page the mark came from (SuspiciousInterviewer.SourcePage, optional column),
        -- e.g. "Marked suspicious · TRM"; marks from before the column existed show no page.
        DECLARE @SusPage NVARCHAR(MAX) = CASE WHEN COL_LENGTH('dbo.SuspiciousInterviewer', 'SourcePage') IS NOT NULL THEN N'
                + ISNULL('' · '' + CASE
                    WHEN s.SourcePage = ''TotalInterviews''        THEN ''Total Interviews''
                    WHEN s.SourcePage = ''TRM''                    THEN ''TRM''
                    WHEN s.SourcePage = ''PotentialRiskCandidate'' THEN ''Potential Risk''
                    WHEN s.SourcePage = ''TrialFlagReview''        THEN ''Trial Flag Review''
                    WHEN s.SourcePage = ''PaymentSla''             THEN ''Payment SLA''
                    WHEN s.SourcePage = ''HighRisk''               THEN ''High-Risk''
                    WHEN s.SourcePage LIKE ''911:%''               THEN ''911 dashboard ('' + SUBSTRING(s.SourcePage, 5, 50) + '')''
                    ELSE NULLIF(LTRIM(RTRIM(s.SourcePage)), '''') END, '''')' ELSE N'' END;

        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewerUserId, InterviewerEmail, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(s.CreatedBy)), 150),
            ''Suspicious'',
            DATEADD(MINUTE, 330, s.Createddate),
            s.UserId,
            u.EmailId,
            LEFT(NULLIF(LTRIM(RTRIM(s.Comment)), ''''), 500),
            LEFT(''Marked suspicious''' + @SusPage + N', 150),
            s.Id
        FROM dbo.SuspiciousInterviewer s
        OUTER APPLY (SELECT TOP 1 ul.EmailId FROM dbo.UserList ul WHERE ul.id = s.UserId) u
        WHERE s.Createddate >= @FromUtc
          AND s.Createddate <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(s.CreatedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(s.CreatedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(s.CreatedBy)) = @ActorEmail)
          AND (@InterviewerEmail IS NULL OR s.UserId = @IvUserId);';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
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

    /* ---------------------------------------------------------------
       14. Proxy forms (ProxyForm) and redo requests (RedoRequested).
           usp_SaveorUpdateProxyReport / usp_AddRedoInterviewDetails log
           every save in InterviewerActionLog (Reason "Interview #<id>: ...");
           an update of the proxy form overwrites Qcdoneby, so the log is the
           only record of who edited it. Saves from before the log existed
           come from ProxyReport.CreatedBy / RedoInterviews.CreatedBy (first
           save only); such a row is skipped when the log already has the
           same interview and person within 5 minutes (either clock).
       --------------------------------------------------------------- */
    IF @WantProxy = 1 OR @WantRedo = 1
    BEGIN
        DECLARE @HasActionLog BIT = CASE WHEN OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL THEN 1 ELSE 0 END;

        IF @HasActionLog = 1
        BEGIN
            SET @Sql = N'
            INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerUserId, InterviewerEmail, Detail, [Status], SourceId)
            SELECT
                LEFT(LTRIM(RTRIM(l.ActedBy)), 150),
                l.ActionType,
                DATEADD(MINUTE, 330, l.ActedAtUtc),
                x.InterviewId,
                irf.id,
                l.InterviewerUserId,
                l.InterviewerEmail,
                LEFT(NULLIF(LTRIM(RTRIM(l.Reason)), N''''), 500),
                CASE l.ActionType WHEN ''ProxyForm'' THEN ''Proxy form saved'' ELSE ''Redo requested'' END,
                l.Id
            FROM dbo.InterviewerActionLog l
            CROSS APPLY (SELECT CASE WHEN l.Reason LIKE N''Interview #%''
                                     THEN TRY_CAST(SUBSTRING(l.Reason, 12, CHARINDEX(N'':'', l.Reason + N'':'', 12) - 12) AS INT) END AS InterviewId) x
            LEFT JOIN dbo.InterviewMeetings im ON im.id = x.InterviewId
            OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
            WHERE l.ActionType IN (''ProxyForm'', ''RedoRequested'')
              AND l.ActedAtUtc >= @FromUtc
              AND l.ActedAtUtc <  @ToUtc
              AND NULLIF(LTRIM(RTRIM(l.ActedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(l.ActedBy)) NOT IN (''Admin'', ''AISystem'', ''System'')
              AND (@ActorEmail IS NULL OR LTRIM(RTRIM(l.ActedBy)) = @ActorEmail)
              AND (@InterviewerEmail IS NULL OR l.InterviewerUserId = @IvUserId
                   OR (l.InterviewerUserId IS NULL AND l.InterviewerEmail = @InterviewerEmail))';
            IF @WantProxy = 0 SET @Sql += N' AND l.ActionType = ''RedoRequested''';
            IF @WantRedo = 0  SET @Sql += N' AND l.ActionType = ''ProxyForm''';

            EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
                @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
        END

        -- First saves recorded only in the form tables (before the log, or while it is missing).
        DECLARE @NotLogged NVARCHAR(MAX) = CASE WHEN @HasActionLog = 1 THEN N'
              AND NOT EXISTS (SELECT 1 FROM dbo.InterviewerActionLog l
                              WHERE l.ActionType = @LogType
                                AND LTRIM(RTRIM(l.ActedBy)) = LTRIM(RTRIM(t.CreatedBy))
                                AND l.Reason LIKE N''Interview #'' + CAST(t.interviewid AS NVARCHAR(20)) + N'':%''
                                AND (ABS(DATEDIFF(MINUTE, l.ActedAtUtc, t.CreatedOn)) <= 5
                                     OR ABS(DATEDIFF(MINUTE, l.ActedAtUtc, t.CreatedOn) + 330) <= 5))' ELSE N'' END;

        IF @WantProxy = 1 AND OBJECT_ID(N'dbo.ProxyReport', N'U') IS NOT NULL
        BEGIN
            SET @Sql = N'
            INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Detail, [Status], SourceId)
            SELECT
                LEFT(LTRIM(RTRIM(t.CreatedBy)), 150),
                ''ProxyForm'',
                DATEADD(MINUTE, 330, t.CreatedOn),
                t.interviewid,
                irf.id,
                LTRIM(RTRIM(im.interviewer)),
                LEFT(N''Interview #'' + CAST(t.interviewid AS NVARCHAR(20)) + N'': proxy form filled; proxy candidate: ''
                     + CASE WHEN t.isProxyCandidate = 1 THEN N''yes'' ELSE N''no'' END, 500),
                ''Proxy form saved'',
                t.id
            FROM dbo.ProxyReport t
            LEFT JOIN dbo.InterviewMeetings im ON im.id = t.interviewid
            OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
            WHERE t.CreatedOn >= @FromUtc
              AND t.CreatedOn <  @ToUtc
              AND NULLIF(LTRIM(RTRIM(t.CreatedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(t.CreatedBy)) NOT IN (''Admin'', ''AISystem'', ''System'')
              AND (@ActorEmail IS NULL OR LTRIM(RTRIM(t.CreatedBy)) = @ActorEmail)
              AND (@InterviewerEmail IS NULL OR LTRIM(RTRIM(im.interviewer)) = @InterviewerEmail)' + @NotLogged;

            EXEC sp_executesql @Sql, N'@FromUtc DATETIME, @ToUtc DATETIME, @ActorEmail VARCHAR(150), @InterviewerEmail VARCHAR(150), @LogType VARCHAR(40)',
                @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail, @InterviewerEmail = @InterviewerEmail, @LogType = 'ProxyForm';
        END

        IF @WantRedo = 1 AND OBJECT_ID(N'dbo.RedoInterviews', N'U') IS NOT NULL
        BEGIN
            SET @Sql = N'
            INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, IrfId, InterviewerEmail, Detail, [Status], SourceId)
            SELECT
                LEFT(LTRIM(RTRIM(t.CreatedBy)), 150),
                ''RedoRequested'',
                DATEADD(MINUTE, 330, t.CreatedOn),
                t.interviewid,
                irf.id,
                LTRIM(RTRIM(im.interviewer)),
                LEFT(N''Interview #'' + CAST(t.interviewid AS NVARCHAR(20)) + N'': ''
                     + ISNULL(NULLIF(LTRIM(RTRIM(t.Comments)), ''''), N''redo requested''), 500),
                ''Redo requested'',
                t.id
            FROM (SELECT r.id, r.interviewid, r.CreatedBy, r.Createdon AS CreatedOn, r.Comments FROM dbo.RedoInterviews r) t
            LEFT JOIN dbo.InterviewMeetings im ON im.id = t.interviewid
            OUTER APPLY (SELECT TOP 1 i.id FROM dbo.IRF i WHERE i.IRFId = im.IRFId ORDER BY i.id DESC) irf
            WHERE t.CreatedOn >= @FromUtc
              AND t.CreatedOn <  @ToUtc
              AND NULLIF(LTRIM(RTRIM(t.CreatedBy)), '''') IS NOT NULL
              AND LTRIM(RTRIM(t.CreatedBy)) NOT IN (''Admin'', ''AISystem'', ''System'')
              AND (@ActorEmail IS NULL OR LTRIM(RTRIM(t.CreatedBy)) = @ActorEmail)
              AND (@InterviewerEmail IS NULL OR LTRIM(RTRIM(im.interviewer)) = @InterviewerEmail)' + @NotLogged;

            EXEC sp_executesql @Sql, N'@FromUtc DATETIME, @ToUtc DATETIME, @ActorEmail VARCHAR(150), @InterviewerEmail VARCHAR(150), @LogType VARCHAR(40)',
                @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail, @InterviewerEmail = @InterviewerEmail, @LogType = 'RedoRequested';
        END
    END

    /* ---------------------------------------------------------------
       15. Trial-flag backfill runs (TrialFlagBackfillRun, optional table),
           one row per run with who ran it. Amount = flags inserted.
       --------------------------------------------------------------- */
    IF @WantTrialBackfill = 1 AND OBJECT_ID(N'dbo.TrialFlagBackfillRun', N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'
        INSERT INTO #QCActivityRows (ActorEmail, Metric, ActionAtIst, InterviewId, Amount, Detail, [Status], SourceId)
        SELECT
            LEFT(LTRIM(RTRIM(r.ExecutedBy)), 150),
            ''TrialBackfill'',
            DATEADD(MINUTE, 330, r.ExecutedAtUtc),
            r.InterviewId,
            r.Inserted,
            LEFT(NULLIF(LTRIM(RTRIM(r.Message)), ''''), 500),
            LEFT(''Inserted '' + CAST(r.Inserted AS VARCHAR(10)) + '', skipped '' + CAST(r.Skipped AS VARCHAR(10))
                 + '', failed '' + CAST(r.Failed AS VARCHAR(10)), 150),
            r.Id
        FROM dbo.TrialFlagBackfillRun r
        WHERE r.ExecutedAtUtc >= @FromUtc
          AND r.ExecutedAtUtc <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(r.ExecutedBy)), '''') IS NOT NULL
          AND LTRIM(RTRIM(r.ExecutedBy)) NOT IN (''AISystem'', ''System'')
          AND (@ActorEmail IS NULL OR LTRIM(RTRIM(r.ExecutedBy)) = @ActorEmail)';

        EXEC sp_executesql @Sql, @Params, @FromUtc = @FromUtc, @ToUtc = @ToUtc, @ActorEmail = @ActorEmail,
            @InterviewerEmail = @InterviewerEmail, @IvUserId = @IvUserId;
    END

    /* ---------------------------------------------------------------
       16. Authenticity (part 2). Runs only when the caller's #QCActivityRows
           has the columns PageGroup, Comment, HasEvidence and IsAuthentic
           (dynamic SQL, so older callers without them still work).

           PageGroup   TotalInterviews | TRM | IrfAudit | Trial | Suspicious |
                       ReportsSla | Calls | Coaching | PotentialRisk | HighRisk |
                       Monitoring. The page an action came from wins:
                         - Pr* / Hr* / Pli* moves, Monitoring Remove / Shortlist;
                         - InterviewerActionLog rows whose Source ends in
                           :PotentialRisk / :HighRisk / :PLI (Remove, Redo);
                         - suspicious marks by SuspiciousInterviewer.SourcePage
                           (PotentialRiskCandidate, HighRisk);
                         - IRF audit Keep / Remove whose reason starts [High-Risk];
                         - a Payment SLA review (ReportReviewed) with a matching
                           PotentialRiskReview / HighRiskReview log row (same
                           person, interview, within 5 minutes): no extra row;
                         - QC forms with Trans_QCFormReport.SourcePage = HighRisk.
           Comment     the action's own comment: red-marker moves and
                       suspicious use their Comment; IRF audit the comment in
                       TransInterviewerAuditLog.ExtraMetadata ("comment=..."),
                       else its ReasonCategory; trial ReviewerComment; status
                       actions the log Reason (inactivation type prefix
                       removed); Payment SLA rows RecommendationComments.
           HasEvidence the interviewer has, in the window: a P0-P5 graded
                       interview, an AI penalty (PaymentSLADetails AISystem
                       > 0), an open trial flag, an auto-QC verdict, a red
                       marker (same sources as usp_GetInterviewerQCView), or a
                       PLI / PQI process signal on a completed interview in the
                       window (usp_InterviewerProcessSignals_Collect, when deployed).
           IsAuthentic calls: answered, to an interviewer, Amount >=
                       MinCallSeconds. Scored actions (everything except
                       calls, coaching and backfill runs): comment of at
                       least MinCommentChars that is not a stock word, AND
                       evidence. NULL for rows that are not scored.
           Thresholds come from dbo.QCActivitySetting (defaults below).
       --------------------------------------------------------------- */
    IF COL_LENGTH('tempdb..#QCActivityRows', 'PageGroup') IS NOT NULL
       AND COL_LENGTH('tempdb..#QCActivityRows', 'Comment') IS NOT NULL
       AND COL_LENGTH('tempdb..#QCActivityRows', 'HasEvidence') IS NOT NULL
       AND COL_LENGTH('tempdb..#QCActivityRows', 'IsAuthentic') IS NOT NULL
    BEGIN
        DECLARE @MinCallSeconds INT = 120, @MinCommentChars INT = 20,
                @StockComments NVARCHAR(1000) = N'ok,okay,done,fine,checked,check,na,n/a,nil,none,reviewed,good,noted,yes,no,-,.';
        IF OBJECT_ID(N'dbo.QCActivitySetting', N'U') IS NOT NULL
            EXEC sp_executesql N'
                SELECT @MinCall  = ISNULL(MAX(CASE WHEN Name = ''MinCallSeconds''  THEN TRY_CAST(Value AS INT) END), @MinCall),
                       @MinChars = ISNULL(MAX(CASE WHEN Name = ''MinCommentChars'' THEN TRY_CAST(Value AS INT) END), @MinChars),
                       @Stock    = ISNULL(MAX(CASE WHEN Name = ''StockComments''   THEN Value END), @Stock)
                FROM dbo.QCActivitySetting',
                N'@MinCall INT OUTPUT, @MinChars INT OUTPUT, @Stock NVARCHAR(1000) OUTPUT',
                @MinCall = @MinCallSeconds OUTPUT, @MinChars = @MinCommentChars OUTPUT, @Stock = @StockComments OUTPUT;

        CREATE TABLE #QCStock (Word NVARCHAR(100) NOT NULL PRIMARY KEY);
        INSERT INTO #QCStock (Word)
        SELECT DISTINCT LOWER(LTRIM(RTRIM(value))) FROM STRING_SPLIT(ISNULL(@StockComments, N''), ',')
        WHERE LTRIM(RTRIM(value)) <> N'';

        /* Interviewers the rows are about, and which of them have evidence in the window. */
        CREATE TABLE #QCIv (UserId INT NULL, Email VARCHAR(200) NULL);
        CREATE TABLE #QCIvEvidence (UserId INT NOT NULL PRIMARY KEY);
        -- Per-row lookups below (by email and by id) seek instead of scanning #QCIv for every row.
        CREATE INDEX IX_QCIv_Email ON #QCIv (Email) INCLUDE (UserId);
        CREATE INDEX IX_QCIv_UserId ON #QCIv (UserId);

        SET @Sql = N'
        INSERT INTO #QCIv (UserId, Email)
        SELECT DISTINCT r.InterviewerUserId, ul.EmailId
        FROM #QCActivityRows r
        OUTER APPLY (SELECT TOP 1 LTRIM(RTRIM(u.EmailId)) AS EmailId FROM dbo.UserList u WHERE u.id = r.InterviewerUserId) ul
        WHERE r.InterviewerUserId IS NOT NULL;

        INSERT INTO #QCIv (UserId, Email)
        SELECT DISTINCT u.id, r.InterviewerEmail
        FROM (SELECT DISTINCT LTRIM(RTRIM(InterviewerEmail)) AS InterviewerEmail FROM #QCActivityRows
              WHERE InterviewerUserId IS NULL AND NULLIF(LTRIM(RTRIM(InterviewerEmail)), '''') IS NOT NULL) r
        OUTER APPLY (SELECT TOP 1 x.id FROM dbo.UserList x WHERE x.EmailId = r.InterviewerEmail ORDER BY x.id DESC) u
        WHERE u.id IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #QCIv v WHERE v.UserId = u.id);

        -- P0-P5 graded interview in the window (interviewDate is IST)
        INSERT INTO #QCIvEvidence (UserId)
        SELECT DISTINCT v.UserId
        FROM #QCIv v
        WHERE v.UserId IS NOT NULL AND v.Email IS NOT NULL
          AND EXISTS (SELECT 1 FROM dbo.InterviewMeetings im
                      INNER JOIN dbo.RiskCategory rc ON rc.InterviewId = im.id
                      WHERE im.interviewer = v.Email
                        AND im.interviewDate >= @FromIstDt AND im.interviewDate < @ToIstDt
                        AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '''')))) LIKE ''P[0-5]'');

        -- AI penalty on an interview in the window
        INSERT INTO #QCIvEvidence (UserId)
        SELECT DISTINCT v.UserId
        FROM #QCIv v
        WHERE v.UserId IS NOT NULL AND v.Email IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = v.UserId)
          AND EXISTS (SELECT 1 FROM dbo.InterviewMeetings im
                      INNER JOIN dbo.PaymentSLADetails ps ON ps.Interviewid = im.id
                      WHERE im.interviewer = v.Email
                        AND im.interviewDate >= @FromIstDt AND im.interviewDate < @ToIstDt
                        AND LTRIM(RTRIM(ISNULL(ps.CreatedBy, ''''))) = ''AISystem''
                        AND ISNULL(ps.PaymentDeduction, 0) > 0);

        -- Red markers
        INSERT INTO #QCIvEvidence (UserId)
        SELECT DISTINCT v.UserId
        FROM #QCIv v
        WHERE v.UserId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = v.UserId)
          AND EXISTS (SELECT 1 FROM dbo.TransInterviewerRedMarker trm
                      INNER JOIN dbo.InterviewerProfile ip ON ip.Id = trm.InterviewerId
                      WHERE ip.UserId = v.UserId AND trm.RedMarker > 0);';
        EXEC sp_executesql @Sql, N'@FromIstDt DATETIME, @ToIstDt DATETIME', @FromIstDt = @FromIstDt, @ToIstDt = @ToIstDt;

        -- Open trial flag
        IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #QCIvEvidence (UserId)
                SELECT DISTINCT v.UserId
                FROM #QCIv v
                WHERE v.UserId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = v.UserId)
                  AND EXISTS (SELECT 1 FROM dbo.TrialFlags tf
                              INNER JOIN dbo.Interviewer iv ON iv.id = tf.InterviewerId
                              WHERE iv.UserId = v.UserId AND LOWER(LTRIM(RTRIM(tf.ReviewStatus))) = ''pending'')';

        -- Auto-QC verdict on an active channel
        IF COL_LENGTH('dbo.TransInterviewer', 'AutoQcVerdict') IS NOT NULL
            EXEC sp_executesql N'
                INSERT INTO #QCIvEvidence (UserId)
                SELECT DISTINCT v.UserId
                FROM #QCIv v
                WHERE v.UserId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = v.UserId)
                  AND EXISTS (SELECT 1 FROM dbo.TransInterviewer t
                              WHERE t.UserId = v.UserId AND t.IsActive = 1
                                AND NULLIF(LTRIM(RTRIM(ISNULL(t.AutoQcVerdict, ''''))), '''') IS NOT NULL)';

        -- PLI / PQI process signal on a completed interview in the window (short comment, fewer than N questions,
        -- short call, late join, negative candidate rating), so acting on a PLI / PQI-flagged interviewer can score
        -- as authentic. Optional: skipped when the signals procedure is not deployed; never blocks the rows.
        IF OBJECT_ID(N'dbo.usp_InterviewerProcessSignals_Collect', N'P') IS NOT NULL
        BEGIN
            DECLARE @NoEvidenceIds NVARCHAR(MAX) = NULL;
            SELECT @NoEvidenceIds = STRING_AGG(CAST(x.UserId AS NVARCHAR(MAX)), N',')
            FROM (SELECT DISTINCT v.UserId FROM #QCIv v
                  WHERE v.UserId IS NOT NULL
                    AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = v.UserId)) x;

            IF @NoEvidenceIds IS NOT NULL
            BEGIN
            BEGIN TRY
                CREATE TABLE #ProcessSignalRows (
                    InterviewId       INT           NOT NULL PRIMARY KEY,
                    InterviewerUserId INT           NULL,
                    InterviewerEmail  VARCHAR(200)  NULL,
                    IrfIntId          INT           NULL,
                    IRFId             VARCHAR(100)  NULL,
                    InterviewDateIst  DATETIME      NULL,
                    Outcome           VARCHAR(20)   NULL,
                    CommentChars      INT           NULL,
                    QuestionCount     INT           NULL,
                    VideoMinutes      INT           NULL,
                    PlannedMinutes    INT           NULL,
                    LateJoinMinutes   INT           NULL,
                    CandidateRating   INT           NULL,
                    ShortComment      BIT           NOT NULL DEFAULT 0,
                    FewQuestions      BIT           NOT NULL DEFAULT 0,
                    ShortCall         BIT           NOT NULL DEFAULT 0,
                    LateJoin          BIT           NOT NULL DEFAULT 0,
                    NegativeRating    BIT           NOT NULL DEFAULT 0
                );
                EXEC dbo.usp_InterviewerProcessSignals_Collect @FromIst = @FromIst, @ToIst = @ToIst, @UserIds = @NoEvidenceIds;

                INSERT INTO #QCIvEvidence (UserId)
                SELECT DISTINCT p.InterviewerUserId
                FROM #ProcessSignalRows p
                WHERE p.InterviewerUserId IS NOT NULL
                  AND (p.ShortComment = 1 OR p.FewQuestions = 1 OR p.ShortCall = 1 OR p.LateJoin = 1 OR p.NegativeRating = 1)
                  AND NOT EXISTS (SELECT 1 FROM #QCIvEvidence e WHERE e.UserId = p.InterviewerUserId);

                DROP TABLE #ProcessSignalRows;
            END TRY
            BEGIN CATCH
                DROP TABLE IF EXISTS #ProcessSignalRows;
            END CATCH
            END
        END

        /* Page group, comment, evidence, authenticity */
        SET @Sql = N'
        UPDATE #QCActivityRows SET PageGroup = CASE
            WHEN Metric IN (''TiUnderReview'', ''TiReviewed'', ''TiRemoved'', ''Blocked'', ''Unblocked'', ''Inactivated'', ''Reactivated'',
                            ''AccessRevoked'', ''AccessRestored'', ''PaymentHold'', ''PaymentHoldReleased'') THEN ''TotalInterviews''
            WHEN Metric IN (''TrmUnderReview'', ''TrmReviewed'', ''TrmRemoved'') THEN ''TRM''
            WHEN Metric IN (''AuditKept'', ''AuditRemoved'', ''AuditBackfill'') THEN ''IrfAudit''
            WHEN Metric IN (''TrialKept'', ''TrialRemoved'', ''TrialBackfill'') THEN ''Trial''
            WHEN Metric IN (''Suspicious'', ''SuspiciousUndone'') THEN ''Suspicious''
            WHEN Metric = ''PlivoCall'' THEN ''Calls''
            WHEN Metric = ''Coaching'' THEN ''Coaching''
            WHEN Metric IN (''PrUnderReview'', ''PrReviewed'', ''PrRemoved'') THEN ''PotentialRisk''
            WHEN Metric IN (''HrUnderReview'', ''HrReviewed'', ''HrRemoved'') THEN ''HighRisk''
            WHEN Metric IN (''PliUnderReview'', ''PliReviewed'', ''PliRemoved'', ''MonitoringRemoved'', ''MonitoringShortlisted'') THEN ''Monitoring''
            ELSE ''ReportsSla'' END;

        -- IRF audit Keep / Remove made from High-Risk Interviewers (reason starts [High-Risk]).
        UPDATE #QCActivityRows SET PageGroup = ''HighRisk''
        WHERE Metric IN (''AuditKept'', ''AuditRemoved'') AND LEFT(LTRIM(ISNULL(Detail, N'''')), 11) = N''[High-Risk]'';

        UPDATE #QCActivityRows
        SET Comment = LEFT(NULLIF(LTRIM(RTRIM(
                CASE
                    WHEN Metric IN (''Blocked'', ''Inactivated'') AND CHARINDEX(N'': '', ISNULL(Detail, N'''')) > 0
                        THEN SUBSTRING(Detail, CHARINDEX(N'': '', Detail) + 2, 500)
                    WHEN Metric IN (''Blocked'', ''Inactivated'') THEN NULL
                    WHEN Metric IN (''RedoRequested'', ''DeductionReview'') AND Detail LIKE N''Interview #%: %''
                        THEN SUBSTRING(Detail, CHARINDEX(N'': '', Detail) + 2, 500)
                    WHEN Metric = ''ProxyForm'' THEN NULL
                    ELSE Detail
                END)), N''''), 500)
        WHERE Metric NOT IN (''PlivoCall'', ''Coaching'', ''AuditBackfill'', ''TrialBackfill'');

        -- Payment SLA rows: the QC''s recommendation comment when there is one
        UPDATE r SET Comment = LEFT(ps.RecommendationComments, 500)
        FROM #QCActivityRows r
        CROSS APPLY (SELECT TOP 1 NULLIF(LTRIM(RTRIM(p.RecommendationComments)), '''') AS RecommendationComments
                     FROM dbo.PaymentSLADetails p WHERE p.Interviewid = r.InterviewId ORDER BY p.id DESC) ps
        WHERE r.Metric IN (''ReportReviewed'', ''SlaApproved'', ''SlaNotApproved'')
          AND r.InterviewId IS NOT NULL
          AND ps.RecommendationComments IS NOT NULL;

        UPDATE r SET HasEvidence = CASE WHEN EXISTS (
                SELECT 1 FROM #QCIvEvidence e
                WHERE e.UserId = COALESCE(r.InterviewerUserId,
                                          (SELECT TOP 1 v.UserId FROM #QCIv v
                                           WHERE r.InterviewerUserId IS NULL AND v.Email = LTRIM(RTRIM(r.InterviewerEmail))))
            ) THEN 1 ELSE 0 END
        FROM #QCActivityRows r;

        UPDATE #QCActivityRows SET IsAuthentic = CASE
            WHEN Metric = ''PlivoCall'' THEN
                CASE WHEN ISNULL(Amount, 0) >= @MinCall
                          AND ISNULL([Status], '''') NOT LIKE ''%busy%''
                          AND ISNULL([Status], '''') NOT LIKE ''%no-answer%''
                          AND ISNULL([Status], '''') NOT LIKE ''%NO_ANSWER%''
                          AND ISNULL([Status], '''') NOT LIKE ''%fail%''
                          AND ISNULL([Status], '''') NOT LIKE ''%cancel%''
                     THEN 1 ELSE 0 END
            WHEN Metric IN (''Coaching'', ''AuditBackfill'', ''TrialBackfill'') THEN NULL
            WHEN HasEvidence = 1
                 AND LEN(LTRIM(RTRIM(ISNULL(Comment, N'''')))) >= @MinChars
                 AND NOT EXISTS (SELECT 1 FROM #QCStock s
                                 WHERE s.Word = LOWER(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(Comment, N''''), N''.'', N''''), N''!'', N'''')))))
                THEN 1
            ELSE 0 END;';
        EXEC sp_executesql @Sql, N'@MinCall INT, @MinChars INT', @MinCall = @MinCallSeconds, @MinChars = @MinCommentChars;

        -- Page of origin from InterviewerActionLog.Source ("<procedure>:PotentialRisk | :HighRisk | :PLI"):
        -- Remove (Inactivated / Blocked) and Redo from those pages. The row must be the log row itself (same id,
        -- type, person and time), so rows rebuilt from RedoInterviews / ProxyReport are never matched.
        IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
            EXEC sp_executesql N'
                UPDATE r SET PageGroup = CASE
                        WHEN l.Source LIKE ''%:PotentialRisk'' THEN ''PotentialRisk''
                        WHEN l.Source LIKE ''%:HighRisk''      THEN ''HighRisk''
                        ELSE ''Monitoring'' END
                FROM #QCActivityRows r
                INNER JOIN dbo.InterviewerActionLog l
                        ON l.Id = r.SourceId
                       AND l.ActionType = r.Metric
                       AND LTRIM(RTRIM(l.ActedBy)) = r.ActorEmail
                       AND DATEADD(MINUTE, 330, l.ActedAtUtc) = r.ActionAtIst
                WHERE (l.Source LIKE ''%:PotentialRisk'' OR l.Source LIKE ''%:HighRisk'' OR l.Source LIKE ''%:PLI'');

                -- A Payment SLA review saved from Potential Risk by Candidate / the High-Risk Reports tab: the
                -- ReportReviewed row moves to that page (the PotentialRiskReview / HighRiskReview log row is not counted).
                UPDATE r SET PageGroup = CASE WHEN x.ActionType = ''HighRiskReview'' THEN ''HighRisk'' ELSE ''PotentialRisk'' END
                FROM #QCActivityRows r
                CROSS APPLY (SELECT TOP 1 l.ActionType FROM dbo.InterviewerActionLog l
                             WHERE l.ActionType IN (''PotentialRiskReview'', ''HighRiskReview'')
                               AND LTRIM(RTRIM(l.ActedBy)) = r.ActorEmail
                               AND l.Reason LIKE N''Interview #'' + CAST(r.InterviewId AS NVARCHAR(20)) + N'':%''
                               AND (ABS(DATEDIFF(MINUTE, DATEADD(MINUTE, 330, l.ActedAtUtc), r.ActionAtIst)) <= 5
                                    OR ABS(DATEDIFF(MINUTE, l.ActedAtUtc, r.ActionAtIst)) <= 5)
                             ORDER BY l.ActedAtUtc DESC) x
                WHERE r.Metric = ''ReportReviewed'' AND r.InterviewId IS NOT NULL;';

        -- Suspicious marks made on Potential Risk by Candidate / High-Risk count under that page.
        IF COL_LENGTH('dbo.SuspiciousInterviewer', 'SourcePage') IS NOT NULL
            EXEC sp_executesql N'
                UPDATE r SET PageGroup = CASE s.SourcePage WHEN ''PotentialRiskCandidate'' THEN ''PotentialRisk'' ELSE ''HighRisk'' END
                FROM #QCActivityRows r
                INNER JOIN dbo.SuspiciousInterviewer s ON s.Id = r.SourceId
                WHERE r.Metric = ''Suspicious'' AND s.SourcePage IN (''PotentialRiskCandidate'', ''HighRisk'');';

        -- QC forms filled from the High-Risk Reports tab (Trans_QCFormReport.SourcePage, optional column).
        IF COL_LENGTH('dbo.Trans_QCFormReport', 'SourcePage') IS NOT NULL
            EXEC sp_executesql N'
                UPDATE r SET PageGroup = ''HighRisk''
                FROM #QCActivityRows r
                INNER JOIN dbo.Trans_QCFormReport q ON q.id = r.SourceId
                WHERE r.Metric = ''QCForm'' AND q.SourcePage = ''HighRisk'';';

        -- IRF audit: the person's own comment from ExtraMetadata ("...; comment=<text>"), else ReasonCategory (Detail).
        IF COL_LENGTH('dbo.TransInterviewerAuditLog', 'ExtraMetadata') IS NOT NULL
        BEGIN
            EXEC sp_executesql N'
                UPDATE r SET Comment = LEFT(NULLIF(LTRIM(RTRIM(
                        CASE WHEN CHARINDEX(N''comment='', l.ExtraMetadata) > 0
                             THEN SUBSTRING(l.ExtraMetadata, CHARINDEX(N''comment='', l.ExtraMetadata) + 8, 500) END)), N''''), 500)
                FROM #QCActivityRows r
                INNER JOIN dbo.TransInterviewerAuditLog l ON l.Id = r.SourceId
                WHERE r.Metric IN (''AuditKept'', ''AuditRemoved'', ''MonitoringRemoved'', ''MonitoringShortlisted'');

                UPDATE #QCActivityRows SET IsAuthentic = CASE
                    WHEN HasEvidence = 1
                         AND LEN(LTRIM(RTRIM(ISNULL(Comment, N'''')))) >= @MinChars
                         AND NOT EXISTS (SELECT 1 FROM #QCStock s
                                         WHERE s.Word = LOWER(LTRIM(RTRIM(REPLACE(REPLACE(ISNULL(Comment, N''''), N''.'', N''''), N''!'', N'''')))))
                    THEN 1 ELSE 0 END
                WHERE Metric IN (''AuditKept'', ''AuditRemoved'', ''MonitoringRemoved'', ''MonitoringShortlisted'');',
                N'@MinChars INT', @MinChars = @MinCommentChars;
        END

        DROP TABLE IF EXISTS #QCIvEvidence;
        DROP TABLE IF EXISTS #QCIv;
        DROP TABLE IF EXISTS #QCStock;
    END
END
GO
