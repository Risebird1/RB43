/*
================================================================================
usp_GetQCActivityDashboardDetails — QC Activity dashboard drill-down.

Lists the records behind one number on the dashboard: one user (@ActorEmail),
one metric key, one IST date range. Reads the same rows as the summary
(usp_QCActivity_CollectRows), newest first, top 2000.

Metric keys: ReportReviewed, PenaltyApplied, SlaApproved, SlaNotApproved,
QCForm, TrialKept, TrialRemoved, Trial, AuditBackfill, AuditKept,
AuditRemoved, AuditActions, TrmUnderReview, TrmReviewed, TrmRemoved, Trm,
TiUnderReview, TiReviewed, TiRemoved, Ti, PlivoCall, Coaching, All.
The web layer whitelists them; an unknown key returns no rows.

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
        SourceId          INT           NULL
    );

    -- A drill-down is always for one user and one known metric.
    IF @ActorEmail IS NOT NULL AND @Metric IS NOT NULL
        EXEC dbo.usp_QCActivity_CollectRows
            @FromIst    = @FromIst,
            @ToIst      = @ToIst,
            @ActorEmail = @ActorEmail,
            @Metric     = @Metric;

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
        r.SourceId
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
            ul.MobileNumber,
            NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '') AS FullName
        FROM dbo.UserList ul
        WHERE r.InterviewerUserId IS NULL
          AND r.InterviewerEmail IS NOT NULL
          AND ul.EmailId = r.InterviewerEmail
        ORDER BY ul.id DESC
    ) byEmail
    WHERE @Metric IN ('All', 'Trial', 'AuditActions', 'Trm', 'Ti')
       OR r.Metric = @Metric
       OR (@Metric = 'PenaltyApplied' AND r.Metric = 'ReportReviewed' AND r.Amount > 0)
    ORDER BY r.ActionAtIst DESC, r.SourceId DESC;
END
GO
