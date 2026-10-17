/*
================================================================================
usp_GetHighRiskReports — High-Risk Interviewers, Reports tab (QC Tracking,
Delta 4, report level). One row per graded report (RiskCategory P0-P5) of a
completed interview in the window, across every in-scope channel (owner scope
for HR / SeniorHR), paged server-side.

Default sort: severity (P0 first), not reviewed first, oldest first.

Columns per row: interview, candidate, client / channel, interviewer (links to
the overall and IRF levels), Severity (worst RiskCategory row) and its name,
AI verdict (PaymentSLADetails written by AISystem: deduction %, recommendation;
InterviewComments.AITranscriptVerdict), candidate rating
(CandidateFeedbackDetails), process flags (usp_InterviewerProcessSignals_Collect:
short comment, <N questions, short call, late join), QC status (PaymentSLADetails
+ history, RedoInterviews, ProxyReport): Not reviewed / Reviewed / Penalised x% /
Redo / Proxy, and AiQcDisagree (AI penalised but the QC save is 0%, or the reverse).
TotalRows on every row.

@Filter  comma-separated keys, all must hold (NULL = no filter):
           NotReviewed     no human Payment SLA review, redo or proxy form yet
           P0P1            severity P0 or P1 only
           P0 / P1 / P2 / P3 / P4 / P5   that severity only
           AiQcDisagree    AI and QC disagree on the penalty
           HasProcessFlags at least one process flag
           ShortComment / FewQuestions / ShortCall / LateJoin   that flag
           Selected / Rejected            that outcome
@InterviewerId  UserList.id (from an interviewer row)    @IrfId  IRF.id (from a channel row)
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetHighRiskReports]
    @FromIst       DATE = NULL,
    @ToIst         DATE = NULL,
    @OwnerEmail    VARCHAR(200) = NULL,
    @Filter        VARCHAR(200) = NULL,
    @InterviewerId INT = NULL,
    @IrfId         INT = NULL,
    @Offset        INT = 0,
    @PageSize      INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    IF @ToIst IS NULL SET @ToIst = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    IF @FromIst IS NULL OR @FromIst > @ToIst SET @FromIst = DATEADD(DAY, -30, @ToIst);
    IF DATEDIFF(DAY, @FromIst, @ToIst) > 90 SET @FromIst = DATEADD(DAY, -90, @ToIst);
    IF @Offset IS NULL OR @Offset < 0 SET @Offset = 0;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 50;
    IF @PageSize > 200 SET @PageSize = 200;
    SET @OwnerEmail = NULLIF(LTRIM(RTRIM(@OwnerEmail)), '');
    IF @InterviewerId <= 0 SET @InterviewerId = NULL;
    IF @IrfId <= 0 SET @IrfId = NULL;

    DECLARE @FromUtc DATETIME = DATEADD(MINUTE, -330, CAST(@FromIst AS DATETIME));
    DECLARE @ToUtc   DATETIME = DATEADD(MINUTE, -330, DATEADD(DAY, 1, CAST(@ToIst AS DATETIME)));

    -- Filter keys
    CREATE TABLE #F (K VARCHAR(40) NOT NULL PRIMARY KEY);
    INSERT INTO #F (K)
    SELECT DISTINCT LTRIM(RTRIM(value)) FROM STRING_SPLIT(ISNULL(@Filter, ''), ',') WHERE LTRIM(RTRIM(value)) <> '';

    -- The interviewer's email and coded id (InterviewMeetings.interviewer holds either).
    DECLARE @IvEmail VARCHAR(200) = NULL, @IvCoded VARCHAR(200) = NULL;
    IF @InterviewerId IS NOT NULL
    BEGIN
        SELECT @IvEmail = LTRIM(RTRIM(EmailId)) FROM dbo.UserList WHERE id = @InterviewerId;
        SELECT TOP 1 @IvCoded = NULLIF(LTRIM(RTRIM(NickName)), '') FROM dbo.InterviewerProfile WHERE UserId = @InterviewerId ORDER BY Id DESC;
    END

    CREATE TABLE #R (
        InterviewId       INT           NOT NULL PRIMARY KEY,
        InterviewDateUtc  DATETIME      NULL,
        IrfIntId          INT           NULL,
        IRFId             VARCHAR(100)  NULL,
        ClientName        NVARCHAR(300) NULL,
        IsAIInterview     BIT           NULL,
        SessionId         NVARCHAR(200) NULL,
        CandidateName     NVARCHAR(300) NULL,
        CandidateEmail    VARCHAR(200)  NULL,
        InterviewerRaw    VARCHAR(200)  NULL,
        InterviewStatus   INT           NULL,
        AITranscriptVerdict VARCHAR(20) NULL,
        Severity          VARCHAR(4)    NULL,
        SeverityRank      INT           NULL,
        RiskName          NVARCHAR(500) NULL
    );

    ;WITH Done AS (
        SELECT im.id, im.interviewDate, im.IRFId, LTRIM(RTRIM(im.interviewer)) AS Interviewer,
               im.candidateName, im.candidateEmail, CAST(im.SessionId AS NVARCHAR(200)) AS SessionId,
               ic.InterviewStatus, ic.AITranscriptVerdict,
               ROW_NUMBER() OVER (PARTITION BY im.id ORDER BY ic.id DESC) AS rn
        FROM dbo.InterviewMeetings im
        INNER JOIN dbo.InterviewComments ic ON ic.InterviewId = im.id
        WHERE ic.MeetingStatus IN (4, 6, 7, 8)
          AND im.interviewDate >= @FromUtc
          AND im.interviewDate <  @ToUtc
          AND NULLIF(LTRIM(RTRIM(im.interviewer)), '') IS NOT NULL
          AND (@InterviewerId IS NULL OR LTRIM(RTRIM(im.interviewer)) IN (@IvEmail, @IvCoded))
    )
    INSERT INTO #R (InterviewId, InterviewDateUtc, IrfIntId, IRFId, ClientName, IsAIInterview, SessionId, CandidateName,
                    CandidateEmail, InterviewerRaw, InterviewStatus, AITranscriptVerdict, Severity, SeverityRank, RiskName)
    SELECT d.id, d.interviewDate, irf.id, d.IRFId, cl.Name, irf.IsAIInterview, d.SessionId, d.candidateName,
           d.candidateEmail, d.Interviewer, d.InterviewStatus, d.AITranscriptVerdict,
           rk.Severity, rk.SeverityRank, rk.RiskName
    FROM Done d
    CROSS APPLY (SELECT TOP 1 i.id, i.RisebirdHr, i.ClientId, CAST(ISNULL(i.IsAIInterview, 0) AS BIT) AS IsAIInterview
                 FROM dbo.IRF i WHERE i.IRFId = d.IRFId ORDER BY i.id DESC) irf
    LEFT JOIN dbo.UserList co ON co.id = irf.RisebirdHr
    LEFT JOIN dbo.Client cl ON cl.id = irf.ClientId
    CROSS APPLY (
        SELECT TOP 1 UPPER(LTRIM(RTRIM(rc.Severity))) AS Severity,
               CAST(SUBSTRING(UPPER(LTRIM(RTRIM(rc.Severity))), 2, 1) AS INT) AS SeverityRank,
               LEFT(CAST(rc.Name AS NVARCHAR(MAX)), 500) AS RiskName
        FROM dbo.RiskCategory rc
        WHERE rc.InterviewId = d.id AND UPPER(LTRIM(RTRIM(ISNULL(rc.Severity, '')))) LIKE 'P[0-5]'
        ORDER BY UPPER(LTRIM(RTRIM(rc.Severity))), rc.Id DESC
    ) rk
    WHERE d.rn = 1
      AND (@OwnerEmail IS NULL OR LTRIM(RTRIM(co.EmailId)) = @OwnerEmail)
      AND (@IrfId IS NULL OR irf.id = @IrfId)
    OPTION (RECOMPILE);

    -- Severity filters
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'P0P1')
        DELETE FROM #R WHERE SeverityRank > 1;
    IF EXISTS (SELECT 1 FROM #F WHERE K IN ('P0', 'P1', 'P2', 'P3', 'P4', 'P5'))
        DELETE r FROM #R r WHERE NOT EXISTS (SELECT 1 FROM #F f WHERE f.K = r.Severity);
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'Selected')
        DELETE FROM #R WHERE ISNULL(InterviewStatus, 0) NOT IN (2, 4);
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'Rejected')
        DELETE FROM #R WHERE ISNULL(InterviewStatus, 0) NOT IN (3, 5);

    -- AI verdict, QC review, redo / proxy, rating
    CREATE TABLE #Q (
        InterviewId      INT          NOT NULL PRIMARY KEY,
        AiDeductionPct   INT          NULL,
        AiRecommendation VARCHAR(50)  NULL,
        HumanReviewed    BIT          NOT NULL DEFAULT 0,
        QcDeductionPct   INT          NULL,
        QcReviewedBy     VARCHAR(200) NULL,
        QcReviewedAtUtc  DATETIME     NULL,
        IsApproved       INT          NULL,
        HasRedo          BIT          NOT NULL DEFAULT 0,
        HasProxy         BIT          NOT NULL DEFAULT 0,
        CandidateRating  INT          NULL
    );
    INSERT INTO #Q (InterviewId) SELECT InterviewId FROM #R;

    UPDATE q SET AiDeductionPct = ai.Pct, AiRecommendation = ai.Recommendation
    FROM #Q q
    CROSS APPLY (SELECT TOP 1 ISNULL(p.PaymentDeduction, 0) AS Pct, p.Recommendation
                 FROM dbo.PaymentSLADetails p
                 WHERE p.Interviewid = q.InterviewId AND LTRIM(RTRIM(ISNULL(p.CreatedBy, ''))) = 'AISystem'
                 ORDER BY p.id DESC) ai;

    -- Latest human save in the history (exact deduction), else a human-modified current row.
    UPDATE q SET HumanReviewed = 1, QcDeductionPct = h.PaymentDeduction, QcReviewedBy = h.CreatedBy, QcReviewedAtUtc = h.CreatedOn
    FROM #Q q
    CROSS APPLY (SELECT TOP 1 ISNULL(x.PaymentDeduction, 0) AS PaymentDeduction, LTRIM(RTRIM(x.CreatedBy)) AS CreatedBy, x.CreatedOn
                 FROM dbo.PaymentSLADetailsHistory x
                 WHERE x.Interviewid = q.InterviewId
                   AND NULLIF(LTRIM(RTRIM(x.CreatedBy)), '') IS NOT NULL
                   AND LTRIM(RTRIM(x.CreatedBy)) NOT IN ('AISystem', 'System')
                 ORDER BY x.CreatedOn DESC, x.id DESC) h;

    UPDATE q SET HumanReviewed = 1, QcDeductionPct = ISNULL(q.QcDeductionPct, p.PaymentDeduction),
                 QcReviewedBy = ISNULL(q.QcReviewedBy, p.Actor), QcReviewedAtUtc = ISNULL(q.QcReviewedAtUtc, p.ModifiedOn),
                 IsApproved = p.IsApproved
    FROM #Q q
    CROSS APPLY (SELECT TOP 1 ISNULL(x.PaymentDeduction, 0) AS PaymentDeduction, x.IsApproved, x.ModifiedOn,
                        LTRIM(RTRIM(COALESCE(NULLIF(LTRIM(RTRIM(x.ModifiedBy)), ''), x.CreatedBy))) AS Actor
                 FROM dbo.PaymentSLADetails x
                 WHERE x.Interviewid = q.InterviewId
                 ORDER BY x.id DESC) p
    WHERE NULLIF(p.Actor, '') IS NOT NULL AND p.Actor NOT IN ('AISystem', 'System');

    UPDATE q SET IsApproved = p.IsApproved
    FROM #Q q
    CROSS APPLY (SELECT TOP 1 x.IsApproved FROM dbo.PaymentSLADetails x WHERE x.Interviewid = q.InterviewId ORDER BY x.id DESC) p
    WHERE q.IsApproved IS NULL;

    IF OBJECT_ID(N'dbo.RedoInterviews', N'U') IS NOT NULL
        UPDATE q SET HasRedo = 1 FROM #Q q WHERE EXISTS (SELECT 1 FROM dbo.RedoInterviews r WHERE r.interviewid = q.InterviewId);
    IF OBJECT_ID(N'dbo.ProxyReport', N'U') IS NOT NULL
        EXEC sp_executesql N'UPDATE q SET HasProxy = 1 FROM #Q q WHERE EXISTS (SELECT 1 FROM dbo.ProxyReport p WHERE p.interviewid = q.InterviewId);';

    UPDATE q SET CandidateRating = r.Rating
    FROM #Q q
    CROSS APPLY (SELECT MIN(c.Rating) AS Rating FROM dbo.CandidateFeedbackDetails c
                 WHERE c.InterviewId = q.InterviewId HAVING MIN(c.Rating) IS NOT NULL) r;

    -- Process flags (optional; blank when the signals procedure is missing)
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
    DECLARE @HasSignals BIT = 0;
    IF OBJECT_ID(N'dbo.usp_InterviewerProcessSignals_Collect', N'P') IS NOT NULL AND EXISTS (SELECT 1 FROM #R)
    BEGIN
        CREATE TABLE #PsOnly (InterviewId INT NOT NULL PRIMARY KEY);
        INSERT INTO #PsOnly (InterviewId) SELECT InterviewId FROM #R;
        BEGIN TRY
            EXEC dbo.usp_InterviewerProcessSignals_Collect @FromIst = @FromIst, @ToIst = @ToIst;
            SET @HasSignals = 1;
        END TRY
        BEGIN CATCH
            DELETE FROM #ProcessSignalRows;
        END CATCH
        DROP TABLE #PsOnly;
    END

    -- Row set with QC status and the remaining filters
    SELECT
        r.*,
        q.AiDeductionPct, q.AiRecommendation, q.HumanReviewed, q.QcDeductionPct, q.QcReviewedBy, q.QcReviewedAtUtc,
        q.IsApproved, q.HasRedo, q.HasProxy, q.CandidateRating,
        CAST(CASE WHEN q.HumanReviewed = 1 OR q.HasRedo = 1 OR q.HasProxy = 1 THEN 1 ELSE 0 END AS BIT) AS IsReviewed,
        CASE WHEN q.HasRedo = 1 THEN 'Redo'
             WHEN q.HasProxy = 1 THEN 'Proxy'
             WHEN q.HumanReviewed = 1 AND ISNULL(q.QcDeductionPct, 0) > 0 THEN 'Penalised ' + CAST(q.QcDeductionPct AS VARCHAR(10)) + '%'
             WHEN q.HumanReviewed = 1 THEN 'Reviewed'
             ELSE 'Not reviewed' END AS QcStatus,
        CAST(CASE WHEN q.HumanReviewed = 1
                   AND ((ISNULL(q.AiDeductionPct, 0) > 0 AND ISNULL(q.QcDeductionPct, 0) = 0)
                     OR (q.AiDeductionPct IS NOT NULL AND ISNULL(q.AiDeductionPct, 0) = 0 AND ISNULL(q.QcDeductionPct, 0) > 0))
                  THEN 1 ELSE 0 END AS BIT) AS AiQcDisagree,
        CAST(ISNULL(ps.ShortComment, 0) AS BIT) AS ShortComment,
        CAST(ISNULL(ps.FewQuestions, 0) AS BIT) AS FewQuestions,
        CAST(ISNULL(ps.ShortCall, 0) AS BIT) AS ShortCall,
        CAST(ISNULL(ps.LateJoin, 0) AS BIT) AS LateJoin,
        ps.QuestionCount, ps.CommentChars, ps.VideoMinutes, ps.PlannedMinutes, ps.LateJoinMinutes,
        @HasSignals AS SignalsLoaded
    INTO #Out
    FROM #R r
    INNER JOIN #Q q ON q.InterviewId = r.InterviewId
    LEFT JOIN #ProcessSignalRows ps ON ps.InterviewId = r.InterviewId;

    IF EXISTS (SELECT 1 FROM #F WHERE K = 'NotReviewed')     DELETE FROM #Out WHERE IsReviewed = 1;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'AiQcDisagree')    DELETE FROM #Out WHERE AiQcDisagree = 0;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'HasProcessFlags') DELETE FROM #Out WHERE ShortComment = 0 AND FewQuestions = 0 AND ShortCall = 0 AND LateJoin = 0;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'ShortComment')    DELETE FROM #Out WHERE ShortComment = 0;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'FewQuestions')    DELETE FROM #Out WHERE FewQuestions = 0;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'ShortCall')       DELETE FROM #Out WHERE ShortCall = 0;
    IF EXISTS (SELECT 1 FROM #F WHERE K = 'LateJoin')        DELETE FROM #Out WHERE LateJoin = 0;

    DECLARE @TotalRows INT = (SELECT COUNT(1) FROM #Out);

    SELECT
        o.InterviewId,
        DATEADD(MINUTE, 330, o.InterviewDateUtc) AS InterviewDateIst,
        o.IrfIntId, o.IRFId, o.ClientName, o.IsAIInterview, o.SessionId,
        o.CandidateName, o.CandidateEmail,
        COALESCE(ivEmail.id, ivCoded.UserId) AS InterviewerUserId,
        COALESCE(ivEmail.EmailId, ivCodedUl.EmailId, o.InterviewerRaw) AS InterviewerEmail,
        NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(COALESCE(ivEmail.FirstName, ivCodedUl.FirstName), ''), N' ',
                                   ISNULL(COALESCE(ivEmail.LastName, ivCodedUl.LastName), '')))), '') AS InterviewerName,
        ipc.NickName AS CodedId,
        o.InterviewStatus, o.AITranscriptVerdict,
        o.Severity, o.RiskName,
        o.AiDeductionPct, o.AiRecommendation,
        o.CandidateRating,
        o.ShortComment, o.FewQuestions, o.ShortCall, o.LateJoin,
        o.QuestionCount, o.CommentChars, o.VideoMinutes, o.PlannedMinutes, o.LateJoinMinutes, o.SignalsLoaded,
        o.QcStatus, o.IsReviewed, o.HumanReviewed, o.QcDeductionPct, o.QcReviewedBy,
        o.QcReviewedAtUtc, o.IsApproved, o.HasRedo, o.HasProxy, o.AiQcDisagree,
        @TotalRows AS TotalRows
    FROM #Out o
    OUTER APPLY (SELECT TOP 1 ul.id, ul.EmailId, ul.FirstName, ul.LastName FROM dbo.UserList ul
                 WHERE ul.EmailId = o.InterviewerRaw ORDER BY ul.id DESC) ivEmail
    OUTER APPLY (SELECT TOP 1 ip.UserId FROM dbo.InterviewerProfile ip
                 WHERE ivEmail.id IS NULL AND LTRIM(RTRIM(ip.NickName)) = o.InterviewerRaw AND ip.UserId IS NOT NULL
                 ORDER BY ip.Id DESC) ivCoded
    OUTER APPLY (SELECT TOP 1 ul.EmailId, ul.FirstName, ul.LastName FROM dbo.UserList ul WHERE ul.id = ivCoded.UserId) ivCodedUl
    OUTER APPLY (SELECT TOP 1 ip.NickName FROM dbo.InterviewerProfile ip
                 WHERE ip.UserId = COALESCE(ivEmail.id, ivCoded.UserId) ORDER BY ip.Id DESC) ipc
    ORDER BY o.SeverityRank ASC, CAST(o.IsReviewed AS INT) ASC, o.InterviewDateUtc ASC, o.InterviewId ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    DROP TABLE IF EXISTS #Out;
    DROP TABLE IF EXISTS #ProcessSignalRows;
    DROP TABLE IF EXISTS #Q;
    DROP TABLE IF EXISTS #R;
    DROP TABLE IF EXISTS #F;
END
GO
