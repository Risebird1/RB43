/*
================================================================================
usp_ExpireDeductionReviewRequests — closes review requests QC did not answer by
their deadline (DueAtUtc = request + 5 working days by default): Status = Expired,
the deduction stands (NewDeductionPct = DeductionPct, nothing refunded).

Called by the DeductionReviewExpiry timer and, so the queue is always current,
whenever the QC queue or an interviewer's My Quality page loads. Idempotent.

Result set: expired requests not yet told to the QC lead (LeadNotifiedAtUtc IS NULL).
The caller emails the QC lead, then calls usp_MarkDeductionReviewLeadNotified.
@ReturnUnnotified = 0 skips the result set (page loads).
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_ExpireDeductionReviewRequests]
    @ReturnUnnotified BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NULL
    BEGIN
        IF @ReturnUnnotified = 1
            SELECT CAST(NULL AS INT) AS Id, CAST(NULL AS INT) AS InterviewId, CAST(NULL AS VARCHAR(150)) AS InterviewerEmail,
                   CAST(NULL AS VARCHAR(200)) AS IrfName, CAST(NULL AS INT) AS DeductionPct, CAST(NULL AS VARCHAR(30)) AS AiCheck,
                   CAST(NULL AS DATETIME) AS RequestedAtUtc, CAST(NULL AS DATETIME) AS DueAtUtc
            WHERE 1 = 0;
        RETURN;
    END

    UPDATE dbo.DeductionReviewRequest
    SET Status = 'Expired',
        NewDeductionPct = DeductionPct,
        RefundedAmount = 0,
        DecidedBy = 'System',
        DecidedAtUtc = GETUTCDATE(),
        DecisionNote = N'No QC reply within the deadline; the deduction stands.'
    WHERE Status = 'Open'
      AND DueAtUtc < GETUTCDATE();

    IF @ReturnUnnotified = 1
        SELECT r.Id, r.InterviewId, r.InterviewerEmail, irf.IRFId AS IrfName, r.DeductionPct, r.AiCheck,
               r.RequestedAtUtc, r.DueAtUtc
        FROM dbo.DeductionReviewRequest r
        LEFT JOIN dbo.IRF irf ON irf.id = r.IrfId
        WHERE r.Status = 'Expired' AND r.LeadNotifiedAtUtc IS NULL
        ORDER BY r.DueAtUtc;
END
GO

/*
usp_MarkDeductionReviewLeadNotified — stamps LeadNotifiedAtUtc after the QC lead email
went out. @Ids is a comma-separated list of DeductionReviewRequest.Id.
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_MarkDeductionReviewLeadNotified]
    @Ids VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NULL RETURN;

    UPDATE r
    SET LeadNotifiedAtUtc = GETUTCDATE()
    FROM dbo.DeductionReviewRequest r
    INNER JOIN (
        SELECT TRY_CAST(LTRIM(RTRIM(value)) AS INT) AS Id
        FROM STRING_SPLIT(ISNULL(@Ids, ''), ',')
    ) ids ON ids.Id = r.Id
    WHERE r.LeadNotifiedAtUtc IS NULL;
END
GO
