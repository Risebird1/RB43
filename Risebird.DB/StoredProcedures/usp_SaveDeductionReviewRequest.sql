/*
================================================================================
usp_SaveDeductionReviewRequest — an interviewer asks QC to review one deduction
(My Quality page). Every rule is enforced here as well as in the UI:

  * the interview is the caller's own (InterviewMeetings.interviewer = @Email,
    the session email — never a request value)
  * the deduction is applied (PaymentSLADetails.IsApproved = 1) and >= @MinPct
  * requested within the window: applied at or after @WindowStart (now - 7 days by default)
  * at most @MonthlyCap requests per calendar month (IST)
  * one request per deduction, ever (a decision is final)

The rule values are passed in by InterviewerMyQualityService (DeductionReviewSettings),
so they stay configurable. @DueAtUtc = request time + the working-day reply deadline.

Returns one row: Result, RequestId
   1 saved            -1 not the caller's / no deduction   -2 below the minimum
  -3 window passed    -4 monthly limit reached             -5 already requested
  -6 not applied yet  -7 tables not deployed               -8 invalid reason / note
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveDeductionReviewRequest]
    @Email          VARCHAR(150),
    @InterviewId    INT,
    @ReasonCategory VARCHAR(30),
    @Note           NVARCHAR(500),
    @MinPct         INT,
    @WindowStart    DATETIME,   -- deductions applied before this (app clock, IST) are outside the window
    @MonthlyCap     INT,
    @NoteMinLength  INT,
    @DueAtUtc       DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Email = NULLIF(LTRIM(RTRIM(@Email)), '');
    SET @Note = NULLIF(LTRIM(RTRIM(@Note)), N'');

    IF OBJECT_ID(N'dbo.DeductionReviewRequest', N'U') IS NULL
    BEGIN
        SELECT -7 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    IF @ReasonCategory NOT IN ('WrongStatus', 'Technical', 'CandidateCaused', 'ReportSavedIncorrectly', 'Other')
       OR @Note IS NULL OR LEN(@Note) < ISNULL(@NoteMinLength, 30)
    BEGIN
        SELECT -8 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    DECLARE @UserId INT = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @Email ORDER BY id DESC);
    IF @UserId IS NULL OR NOT EXISTS (
        SELECT 1 FROM dbo.InterviewMeetings im WHERE im.id = @InterviewId AND im.interviewer = @Email)
    BEGIN
        SELECT -1 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    DECLARE @Pct INT, @Approved INT, @AppliedAt DATETIME;
    SELECT TOP 1
        @Pct = ISNULL(p.PaymentDeduction, 0),
        @Approved = ISNULL(p.IsApproved, 0),
        @AppliedAt = COALESCE(p.ModifiedOn, p.CreatedOn)
    FROM dbo.PaymentSLADetails p
    WHERE p.Interviewid = @InterviewId
    ORDER BY ISNULL(p.PaymentDeduction, 0) DESC, p.id DESC;

    -- The deduction is applied when the first wallet row is written (usp_AddorUpdatePaymentSLADetails).
    DECLARE @WalletAt DATETIME;
    SELECT TOP 1 @WalletAt = mw.AddedOn
    FROM dbo.MoneyProtectionWallet mw
    WHERE mw.Interviewid = @InterviewId
    ORDER BY mw.id ASC;
    SET @AppliedAt = COALESCE(@WalletAt, @AppliedAt);

    -- Amount taken from the interviewer's payout, as Finance/RBPayment and My Quality show it:
    -- InterviewPoints.Points x PaymentSLA % / 100 on the interviewer's Type = 'Interview' row. Not the
    -- MoneyProtectionWallet row, which is the client's credit at the client interview rate.
    DECLARE @PayoutDeduction INT;
    SELECT TOP 1 @PayoutDeduction = ip.Points * ISNULL(ip.PaymentSLA, 0) / 100
    FROM dbo.InterviewPoints ip
    WHERE ip.Interviewid = @InterviewId AND ip.userid = @UserId AND ip.[Type] = 'Interview'
    ORDER BY ip.id DESC;

    IF @Pct IS NULL OR @Pct <= 0
    BEGIN
        SELECT -1 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END
    IF @Approved <> 1
    BEGIN
        SELECT -6 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END
    IF @Pct < ISNULL(@MinPct, 3)
    BEGIN
        SELECT -2 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END
    IF @AppliedAt IS NOT NULL AND @WindowStart IS NOT NULL AND @AppliedAt < @WindowStart
    BEGIN
        SELECT -3 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    DECLARE @TodayIst DATE = CAST(DATEADD(MINUTE, 330, GETUTCDATE()) AS DATE);
    DECLARE @MonthStartUtc DATETIME = DATEADD(MINUTE, -330, CAST(DATEFROMPARTS(YEAR(@TodayIst), MONTH(@TodayIst), 1) AS DATETIME));

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.DeductionReviewRequest WITH (UPDLOCK, HOLDLOCK) WHERE InterviewId = @InterviewId)
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT -5 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    IF (SELECT COUNT(1) FROM dbo.DeductionReviewRequest WITH (UPDLOCK, HOLDLOCK)
        WHERE InterviewerUserId = @UserId AND RequestedAtUtc >= @MonthStartUtc) >= ISNULL(@MonthlyCap, 3)
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT -4 AS Result, CAST(NULL AS INT) AS RequestId;
        RETURN;
    END

    DECLARE @IrfId INT = (
        SELECT TOP 1 irf.id FROM dbo.InterviewMeetings im
        INNER JOIN dbo.IRF irf ON irf.IRFId = im.IRFId
        WHERE im.id = @InterviewId ORDER BY irf.id DESC);

    INSERT INTO dbo.DeductionReviewRequest
        (InterviewId, InterviewerUserId, InterviewerEmail, IrfId, DeductionPct, DeductionAmount,
         ReasonCategory, Note, AiCheck, RequestedAtUtc, DueAtUtc, Status)
    VALUES
        (@InterviewId, @UserId, @Email, @IrfId, @Pct,
         CAST(@PayoutDeduction AS DECIMAL(18,2)),
         @ReasonCategory, LEFT(@Note, 500), dbo.fn_DeductionAiCheck(@InterviewId), GETUTCDATE(), @DueAtUtc, 'Open');

    DECLARE @NewId INT = SCOPE_IDENTITY();
    COMMIT TRANSACTION;

    SELECT 1 AS Result, @NewId AS RequestId;
END
GO
