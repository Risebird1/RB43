/*
================================================================================
usp_GetDeductionReviewQueue — QC Activity → Review requests.

Result sets:
  1 Open      open requests, oldest first, with the interview, channel, interviewer,
              the interviewer's reason and note, the AI check, and the Payment SLA
              recommendation for context (report / transcript / AI reasoning links are
              built in the view from InterviewId / IrfId)
  2 Decided   decisions (incl. Expired) in the last @RecentDays days, newest first
  3 Checks    AI accuracy by check, all time: requests, decided (Stands / Reduced /
              Refunded; Expired is not a decision), upheld (Reduced + Refunded),
              stands, open, and the auto-approval policy for the check

Run usp_ExpireDeductionReviewRequests first so overdue requests are closed.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_GetDeductionReviewQueue]
    @RecentDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    IF @RecentDays IS NULL OR @RecentDays < 1 SET @RecentDays = 30;
    IF @RecentDays > 365 SET @RecentDays = 365;

    /* ---------- 1 Open ---------- */
    SELECT
        r.Id, r.InterviewId, r.IrfId, irf.IRFId AS IrfName, im.interviewDate AS InterviewDate,
        r.InterviewerEmail,
        NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(ul.FirstName, ''), ' ', ISNULL(ul.LastName, '')))), '') AS InterviewerName,
        ipr.NickName AS InterviewerCodedId,
        r.DeductionPct, r.DeductionAmount, r.ReasonCategory, r.Note, r.AiCheck,
        r.RequestedAtUtc, r.DueAtUtc, r.Status, r.NewDeductionPct, r.RefundedAmount,
        r.DecidedBy, r.DecidedAtUtc, r.DecisionNote, r.LeadNotifiedAtUtc, r.PayoutAlreadyPaid,
        ps.Recommendation, ps.RecommendationComments
    FROM dbo.DeductionReviewRequest r
    LEFT JOIN dbo.IRF irf ON irf.id = r.IrfId
    LEFT JOIN dbo.InterviewMeetings im ON im.id = r.InterviewId
    LEFT JOIN dbo.UserList ul ON ul.id = r.InterviewerUserId
    OUTER APPLY (SELECT TOP 1 x.NickName FROM dbo.InterviewerProfile x WHERE x.UserId = r.InterviewerUserId ORDER BY x.Id DESC) ipr
    OUTER APPLY (
        SELECT TOP 1 p.Recommendation, p.RecommendationComments
        FROM dbo.PaymentSLADetails p WHERE p.Interviewid = r.InterviewId
        ORDER BY ISNULL(p.PaymentDeduction, 0) DESC, p.id DESC
    ) ps
    WHERE r.Status = 'Open'
    ORDER BY r.RequestedAtUtc ASC, r.Id ASC;

    /* ---------- 2 Decided (recent) ---------- */
    SELECT TOP 200
        r.Id, r.InterviewId, r.IrfId, irf.IRFId AS IrfName, im.interviewDate AS InterviewDate,
        r.InterviewerEmail,
        NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(ul.FirstName, ''), ' ', ISNULL(ul.LastName, '')))), '') AS InterviewerName,
        ipr.NickName AS InterviewerCodedId,
        r.DeductionPct, r.DeductionAmount, r.ReasonCategory, r.Note, r.AiCheck,
        r.RequestedAtUtc, r.DueAtUtc, r.Status, r.NewDeductionPct, r.RefundedAmount,
        r.DecidedBy, r.DecidedAtUtc, r.DecisionNote, r.LeadNotifiedAtUtc, r.PayoutAlreadyPaid,
        CAST(NULL AS VARCHAR(15)) AS Recommendation, CAST(NULL AS VARCHAR(500)) AS RecommendationComments
    FROM dbo.DeductionReviewRequest r
    LEFT JOIN dbo.IRF irf ON irf.id = r.IrfId
    LEFT JOIN dbo.InterviewMeetings im ON im.id = r.InterviewId
    LEFT JOIN dbo.UserList ul ON ul.id = r.InterviewerUserId
    OUTER APPLY (SELECT TOP 1 x.NickName FROM dbo.InterviewerProfile x WHERE x.UserId = r.InterviewerUserId ORDER BY x.Id DESC) ipr
    WHERE r.Status <> 'Open'
      AND r.DecidedAtUtc >= DATEADD(DAY, -@RecentDays, GETUTCDATE())
    ORDER BY r.DecidedAtUtc DESC, r.Id DESC;

    /* ---------- 3 AI accuracy by check ---------- */
    ;WITH Stats AS (
        SELECT
            r.AiCheck,
            COUNT(1) AS Requests,
            SUM(CASE WHEN r.Status IN ('Stands', 'Reduced', 'Refunded') THEN 1 ELSE 0 END) AS Decided,
            SUM(CASE WHEN r.Status IN ('Reduced', 'Refunded') THEN 1 ELSE 0 END) AS Upheld,
            SUM(CASE WHEN r.Status = 'Stands' THEN 1 ELSE 0 END) AS Stands,
            SUM(CASE WHEN r.Status = 'Open' THEN 1 ELSE 0 END) AS [Open]
        FROM dbo.DeductionReviewRequest r
        GROUP BY r.AiCheck
    )
    SELECT
        COALESCE(s.AiCheck, pol.AiCheck) AS AiCheck,
        ISNULL(s.Requests, 0) AS Requests,
        ISNULL(s.Decided, 0) AS Decided,
        ISNULL(s.Upheld, 0) AS Upheld,
        ISNULL(s.Stands, 0) AS Stands,
        ISNULL(s.[Open], 0) AS [Open],
        CAST(ISNULL(pol.ExcludeFromAutoApprove, 0) AS BIT) AS ExcludeFromAutoApprove,
        pol.ModifiedBy AS PolicyModifiedBy,
        pol.ModifiedAtUtc AS PolicyModifiedAtUtc
    FROM Stats s
    FULL OUTER JOIN dbo.DeductionReviewCheckPolicy pol ON pol.AiCheck = s.AiCheck
    ORDER BY ISNULL(s.Decided, 0) DESC, COALESCE(s.AiCheck, pol.AiCheck);
END
GO

/*
usp_SetDeductionReviewCheckPolicy — take an AI check out of auto-approval (or put it back).
usp_AutoApprovePaymentSlaDetails skips deductions whose main check (dbo.fn_DeductionAiCheck)
has ExcludeFromAutoApprove = 1. Returns the saved row.
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_SetDeductionReviewCheckPolicy]
    @AiCheck   VARCHAR(30),
    @Exclude   BIT,
    @ModifiedBy VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SET @AiCheck = NULLIF(LTRIM(RTRIM(@AiCheck)), '');
    IF @AiCheck IS NULL RETURN;

    MERGE dbo.DeductionReviewCheckPolicy AS t
    USING (SELECT @AiCheck AS AiCheck) AS s ON t.AiCheck = s.AiCheck
    WHEN MATCHED THEN
        UPDATE SET ExcludeFromAutoApprove = ISNULL(@Exclude, 0), ModifiedBy = @ModifiedBy, ModifiedAtUtc = GETUTCDATE()
    WHEN NOT MATCHED THEN
        INSERT (AiCheck, ExcludeFromAutoApprove, ModifiedBy, ModifiedAtUtc)
        VALUES (@AiCheck, ISNULL(@Exclude, 0), @ModifiedBy, GETUTCDATE());

    SELECT AiCheck, ExcludeFromAutoApprove, ModifiedBy, ModifiedAtUtc
    FROM dbo.DeductionReviewCheckPolicy WHERE AiCheck = @AiCheck;
END
GO
