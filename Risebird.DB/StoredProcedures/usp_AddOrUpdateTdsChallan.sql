CREATE OR ALTER PROCEDURE [dbo].[usp_AddOrUpdateTdsChallan]
    @id                     INT,
    @FinancialYear          NVARCHAR(9),
    @Quarter                NVARCHAR(20),
    @WeekNumber             INT,
    @SrNo                   INT,
    @SectionCode            NVARCHAR(10),
    @TANNumber              NVARCHAR(15),
    @Deductor               NVARCHAR(200),
    @TdsAmount              DECIMAL(18,2),
    @BSRCode                NVARCHAR(10),
    @DepositDate            DATE,
    @ChallanSerialNumber    NVARCHAR(30),
    @MinorHead              NVARCHAR(10),
    @ChallanReferenceNumber NVARCHAR(50),
    @ChallanPdfPath         NVARCHAR(400),
    @PayoutReference        NVARCHAR(100),
    @PaymentDate            DATE,
    @IsPaid                 BIT,
    @Operator               NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ChallanId INT = @id;

    IF @id = 0
    BEGIN
        INSERT INTO TdsChallan
            (FinancialYear, Quarter, WeekNumber, SrNo, SectionCode, TANNumber, Deductor,
             TdsAmount, BSRCode, DepositDate, ChallanSerialNumber, MinorHead,
             ChallanReferenceNumber, ChallanPdfPath, PayoutReference, PaymentDate, IsPaid,
             CreatedOn, CreatedBy)
        VALUES
            (@FinancialYear, @Quarter, @WeekNumber, @SrNo, @SectionCode, @TANNumber, @Deductor,
             @TdsAmount, @BSRCode, @DepositDate, @ChallanSerialNumber, @MinorHead,
             @ChallanReferenceNumber, @ChallanPdfPath, @PayoutReference, @PaymentDate, @IsPaid,
             GETDATE(), @Operator);
        SET @ChallanId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE TdsChallan SET
            FinancialYear          = @FinancialYear,
            Quarter                = @Quarter,
            WeekNumber             = @WeekNumber,
            SrNo                   = @SrNo,
            SectionCode            = @SectionCode,
            TANNumber              = @TANNumber,
            Deductor               = @Deductor,
            TdsAmount              = @TdsAmount,
            BSRCode                = @BSRCode,
            DepositDate            = @DepositDate,
            ChallanSerialNumber    = @ChallanSerialNumber,
            MinorHead              = @MinorHead,
            ChallanReferenceNumber = @ChallanReferenceNumber,
            ChallanPdfPath         = CASE WHEN @ChallanPdfPath IS NOT NULL AND @ChallanPdfPath <> '' THEN @ChallanPdfPath ELSE ChallanPdfPath END,
            PayoutReference        = @PayoutReference,
            PaymentDate            = @PaymentDate,
            IsPaid                 = @IsPaid,
            UpdatedOn              = GETDATE(),
            UpdatedBy              = @Operator
        WHERE id = @ChallanId;
    END

    -- Rebuild the interviewer link rows for this challan's week from verified payouts of that week.
    -- Determine date bounds of this week within the quarter.
    DECLARE @StartYear INT, @StartMonth INT, @QStartDate DATE;

    SET @StartYear = CAST(LEFT(@FinancialYear, 4) AS INT);

    SELECT @StartMonth = CASE @Quarter
        WHEN 'Quarter 1' THEN 4
        WHEN 'Quarter 2' THEN 7
        WHEN 'Quarter 3' THEN 10
        WHEN 'Quarter 4' THEN 1
    END;
    IF @Quarter = 'Quarter 4' SET @StartYear = @StartYear + 1;

    SET @QStartDate = DATEFROMPARTS(@StartYear, @StartMonth, 1);

    DECLARE @MonthOffset INT = (@WeekNumber - 1) / 4;
    DECLARE @WeekInMonth INT = ((@WeekNumber - 1) % 4) + 1;
    DECLARE @WeekStart   DATE = DATEADD(DAY, (@WeekInMonth - 1) * 7, DATEADD(MONTH, @MonthOffset, @QStartDate));
    DECLARE @WeekEnd     DATE;

    IF @WeekInMonth < 4
        SET @WeekEnd = DATEADD(DAY, -1, DATEADD(DAY, 7, @WeekStart));
    ELSE
        SET @WeekEnd = DATEADD(DAY, -1, DATEADD(MONTH, @MonthOffset + 1, @QStartDate));

    -- Delete existing link rows
    DELETE FROM TdsChallanInterviewer WHERE ChallanId = @ChallanId;

    -- Re-insert from verified payouts in that week
    INSERT INTO TdsChallanInterviewer (ChallanId, Userid, PAN, AmountPaid, TdsAmount)
    SELECT
        @ChallanId,
        ul.id,
        ISNULL(tic.PAN, ''),
        SUM(ph.TotalAmount),
        ROUND(SUM(ph.TotalAmount) * 0.10, 0)
    FROM PaymentHistory ph
    JOIN UserList ul ON ul.id = ph.Userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE
        ph.PaymentDate >= @WeekStart
        AND ph.PaymentDate <= @WeekEnd
        AND ph.TotalAmount > 0
        AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
        --AND ISNULL(tic.IsBankVerified, 0) = 1
    GROUP BY ul.id, tic.PAN;

    SELECT @ChallanId AS NewId;
END
GO
