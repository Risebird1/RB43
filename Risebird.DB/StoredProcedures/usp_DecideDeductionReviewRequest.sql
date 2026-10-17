/*
================================================================================
usp_DecideDeductionReviewRequest — QC's single, final decision on an interviewer's
deduction review request (QC Activity → Review requests).

  @Decision  Stands | Reduce | Refund
  Reduce     @NewPct between 1 and the current % - 1
  Refund     new % = 0

What a Reduce / Refund writes (in one transaction):
  1. PaymentSLADetails.PaymentDeduction = new %  (ModifiedBy = the QC; the existing
     tr_PaymentSLADetails trigger keeps the audit row in PaymentSLADetailsHistory)
  2. InterviewPoints.PaymentSLA = new % on the interviewer's Type = 'Interview' row.
     Interviewer payouts (Usp_FetchRBInterviewerDetails, FN payment / invoice SPs,
     Cashfree) read this percent, so this is what changes the money paid.
     If that row is already Paid it is NOT changed: PayoutAlreadyPaid = 1 and Finance
     settles the difference by hand.
  3. MoneyProtectionWallet: the latest row for the interview is updated IN PLACE
     (Points = new % x Rate / 100), so both the per-client SUM readers
     (usp_GetMoneyProtectionList) and the latest-row readers stay correct. No new row.
A Stands decision changes no money.
RefundedAmount = InterviewPoints.Points x (old % - new %) / 100 on the interviewer's 'Interview' row,
the payout amount Finance/RBPayment shows (not the wallet credit, which uses the client rate).

Every decision is logged in dbo.InterviewerActionLog (ActionType = 'DeductionReview'),
which usp_QCActivity_CollectRows counts on the QC Activity dashboard.

Returns one row: Result, Status, NewDeductionPct, RefundedAmount, PayoutAlreadyPaid
   1 decided   -1 not found   -2 already decided   -3 invalid decision / %   -4 not deployed
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_DecideDeductionReviewRequest]
    @RequestId    INT,
    @Decision     VARCHAR(10),
    @NewPct       INT = NULL,
    @DecisionNote NVARCHAR(500),
    @QcEmail      VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @QcEmail = NULLIF(LTRIM(RTRIM(@QcEmail)), '');
    SET @DecisionNote = NULLIF(LTRIM(RTRIM(@DecisionNote)), N'');

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NULL
    BEGIN
        SELECT -4 AS Result, CAST(NULL AS VARCHAR(20)) AS Status, CAST(NULL AS INT) AS NewDeductionPct,
               CAST(NULL AS DECIMAL(18,2)) AS RefundedAmount, CAST(0 AS BIT) AS PayoutAlreadyPaid;
        RETURN;
    END

    IF @Decision NOT IN ('Stands', 'Reduce', 'Refund') OR @QcEmail IS NULL OR @DecisionNote IS NULL
    BEGIN
        SELECT -3 AS Result, CAST(NULL AS VARCHAR(20)) AS Status, CAST(NULL AS INT) AS NewDeductionPct,
               CAST(NULL AS DECIMAL(18,2)) AS RefundedAmount, CAST(0 AS BIT) AS PayoutAlreadyPaid;
        RETURN;
    END

    BEGIN TRANSACTION;

    DECLARE @InterviewId INT, @UserId INT, @Email VARCHAR(150), @Pct INT, @Status VARCHAR(20);
    SELECT @InterviewId = r.InterviewId, @UserId = r.InterviewerUserId, @Email = r.InterviewerEmail,
           @Pct = r.DeductionPct, @Status = r.Status
    FROM dbo.DeductionReviewRequest r WITH (UPDLOCK, HOLDLOCK)
    WHERE r.Id = @RequestId;

    IF @InterviewId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT -1 AS Result, CAST(NULL AS VARCHAR(20)) AS Status, CAST(NULL AS INT) AS NewDeductionPct,
               CAST(NULL AS DECIMAL(18,2)) AS RefundedAmount, CAST(0 AS BIT) AS PayoutAlreadyPaid;
        RETURN;
    END
    IF @Status <> 'Open'
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT -2 AS Result, @Status AS Status, CAST(NULL AS INT) AS NewDeductionPct,
               CAST(NULL AS DECIMAL(18,2)) AS RefundedAmount, CAST(0 AS BIT) AS PayoutAlreadyPaid;
        RETURN;
    END

    -- Current % on the report (it may have moved since the request; decide against what is applied now).
    DECLARE @CurrentPct INT = (
        SELECT TOP 1 ISNULL(p.PaymentDeduction, 0) FROM dbo.PaymentSLADetails p
        WHERE p.Interviewid = @InterviewId ORDER BY ISNULL(p.PaymentDeduction, 0) DESC, p.id DESC);
    SET @CurrentPct = ISNULL(@CurrentPct, @Pct);

    DECLARE @FinalPct INT =
        CASE @Decision WHEN 'Stands' THEN @CurrentPct WHEN 'Refund' THEN 0 ELSE @NewPct END;

    IF @Decision = 'Reduce' AND (@NewPct IS NULL OR @NewPct < 1 OR @NewPct >= @CurrentPct)
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT -3 AS Result, CAST(NULL AS VARCHAR(20)) AS Status, CAST(NULL AS INT) AS NewDeductionPct,
               CAST(NULL AS DECIMAL(18,2)) AS RefundedAmount, CAST(0 AS BIT) AS PayoutAlreadyPaid;
        RETURN;
    END

    DECLARE @NewStatus VARCHAR(20) =
        CASE @Decision WHEN 'Stands' THEN 'Stands' WHEN 'Refund' THEN 'Refunded' ELSE 'Reduced' END;

    DECLARE @Refunded DECIMAL(18,2) = 0;
    DECLARE @PayoutPaid BIT = 0;

    IF @Decision IN ('Reduce', 'Refund')
    BEGIN
        -- 1 Report
        UPDATE dbo.PaymentSLADetails
        SET PaymentDeduction = @FinalPct,
            ModifiedBy = @QcEmail,
            ModifiedOn = DATEADD(MINUTE, 330, GETUTCDATE())
        WHERE Interviewid = @InterviewId;

        -- 2 Interviewer payout percent (not when that payout row is already paid)
        IF EXISTS (
            SELECT 1 FROM dbo.InterviewPoints ip
            WHERE ip.Interviewid = @InterviewId AND ip.Userid = @UserId AND ip.Type = 'Interview'
              AND LTRIM(RTRIM(ISNULL(ip.Status, ''))) = 'Paid')
            SET @PayoutPaid = 1;

        -- Refunded = what goes back to the interviewer's payout, priced the way Finance/RBPayment prices the
        -- deduction (InterviewPoints.Points x % / 100), not the client's wallet credit (client rate). Worked
        -- out from the same row even when it is already Paid, so Finance sees what to settle by hand.
        DECLARE @PayPoints INT;
        SELECT TOP 1 @PayPoints = ISNULL(ip.Points, 0)
        FROM dbo.InterviewPoints ip
        WHERE ip.Interviewid = @InterviewId AND ip.Userid = @UserId AND ip.Type = 'Interview'
        ORDER BY ip.id DESC;
        IF @PayPoints IS NOT NULL
            SET @Refunded = CAST(@PayPoints * @CurrentPct / 100 - @PayPoints * @FinalPct / 100 AS DECIMAL(18,2));

        UPDATE dbo.InterviewPoints
        SET PaymentSLA = @FinalPct
        WHERE Interviewid = @InterviewId AND Userid = @UserId AND Type = 'Interview'
          AND LTRIM(RTRIM(ISNULL(Status, ''))) <> 'Paid';

        -- 3 Client money-protection credit: correct the latest wallet row in place
        DECLARE @WalletId INT, @OldPoints INT, @Rate INT;
        SELECT TOP 1 @WalletId = mw.id, @OldPoints = ISNULL(mw.Points, 0), @Rate = ISNULL(mw.Rate, 0)
        FROM dbo.MoneyProtectionWallet mw
        WHERE mw.Interviewid = @InterviewId
        ORDER BY mw.id DESC;

        IF @WalletId IS NOT NULL
        BEGIN
            DECLARE @NewPoints INT = @FinalPct * @Rate / 100;
            UPDATE dbo.MoneyProtectionWallet
            SET Points = @NewPoints,
                Comment = LEFT(CONCAT('Deduction review #', @RequestId, ': ', @NewStatus,
                                      ' (', @CurrentPct, '% -> ', @FinalPct, '%)'), 200)
            WHERE id = @WalletId;
        END
    END

    UPDATE dbo.DeductionReviewRequest
    SET Status = @NewStatus,
        NewDeductionPct = @FinalPct,
        RefundedAmount = @Refunded,
        DecidedBy = @QcEmail,
        DecidedAtUtc = GETUTCDATE(),
        DecisionNote = LEFT(@DecisionNote, 500),
        PayoutAlreadyPaid = @PayoutPaid
    WHERE Id = @RequestId;

    IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
        INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
        VALUES (@UserId, @Email, 'DeductionReview',
                LEFT(CONCAT(N'Interview #', @InterviewId, N': ',
                            CASE @NewStatus WHEN 'Stands' THEN N'deduction stands'
                                            WHEN 'Refunded' THEN N'refunded'
                                            ELSE CONCAT(N'reduced to ', @FinalPct, N'%') END,
                            N' (was ', @CurrentPct, N'%). ', @DecisionNote), 500),
                @QcEmail, 'usp_DecideDeductionReviewRequest');

    COMMIT TRANSACTION;

    SELECT 1 AS Result, @NewStatus AS Status, @FinalPct AS NewDeductionPct, @Refunded AS RefundedAmount,
           @PayoutPaid AS PayoutAlreadyPaid;
END
GO
