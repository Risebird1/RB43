CREATE OR ALTER PROCEDURE [dbo].[usp_FetchInterviewerPaymentItems]
    @StartDate     DATE,
    @EndDate       DATE,
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- Returns individual payment rows per interviewer for the quarter date range.
    -- Used to populate the payment history popover on the Date Paid column.
    -- ChallanSerialNumber is resolved via a LEFT JOIN through TdsChallanInterviewer,
    -- matching each payment date to the week range of its TdsChallan.

    DECLARE @StartYear  INT  = CAST(LEFT(@FinancialYear, 4) AS INT);
    DECLARE @StartMonth INT  = CASE @Quarter
        WHEN 'Quarter 1' THEN 4
        WHEN 'Quarter 2' THEN 7
        WHEN 'Quarter 3' THEN 10
        WHEN 'Quarter 4' THEN 1
        ELSE 4
    END;
    IF @Quarter = 'Quarter 4' SET @StartYear = @StartYear + 1;
    DECLARE @QStartDate DATE = DATEFROMPARTS(@StartYear, @StartMonth, 1);

    -- Subquery groups by (Userid, WeekNumber) so multiple challans for the same
    -- user-week are collapsed into one comma-separated ChallanSerialNumber, preventing
    -- duplicate payment rows in the popup.
    SELECT
        ph.Userid,
        ph.PaymentDate,
        ph.TotalAmount,
        ISNULL(ph.Comment, '')              AS Comment,
        ISNULL(ch.ChallanSerialNumber, '')  AS ChallanSerialNumber
    FROM PaymentHistory ph
    LEFT JOIN (
        -- Outer GROUP BY aggregates distinct challan numbers per user+week.
        -- The inner DISTINCT ensures "12007, 12007" never occurs when the same
        -- serial number exists on multiple TdsChallanInterviewer rows for the same week.
        SELECT
            d.Userid,
            d.WeekNumber,
            STRING_AGG(d.ChallanSerialNumber, ', ')
                WITHIN GROUP (ORDER BY d.ChallanSerialNumber) AS ChallanSerialNumber,
            MAX(d.WeekStart) AS WeekStart,
            MAX(d.WeekEnd)   AS WeekEnd
        FROM (
            SELECT DISTINCT
                tci.Userid,
                tc.WeekNumber,
                NULLIF(tc.ChallanSerialNumber, '') AS ChallanSerialNumber,
                DATEADD(DAY,
                    (((tc.WeekNumber - 1) % 4)) * 7,
                    DATEADD(MONTH, (tc.WeekNumber - 1) / 4, @QStartDate)
                ) AS WeekStart,
                CASE
                    WHEN ((tc.WeekNumber - 1) % 4) + 1 < 4 THEN
                        DATEADD(DAY, 6,
                            DATEADD(DAY, (((tc.WeekNumber - 1) % 4)) * 7,
                                DATEADD(MONTH, (tc.WeekNumber - 1) / 4, @QStartDate)))
                    ELSE
                        DATEADD(DAY, -1,
                            DATEADD(MONTH, (tc.WeekNumber - 1) / 4 + 1, @QStartDate))
                END AS WeekEnd
            FROM TdsChallanInterviewer tci
            JOIN TdsChallan tc ON tc.id = tci.ChallanId
            WHERE tc.FinancialYear = @FinancialYear
              AND tc.Quarter       = @Quarter
              AND NULLIF(tc.ChallanSerialNumber, '') IS NOT NULL
        ) d
        GROUP BY d.Userid, d.WeekNumber
    ) ch ON ch.Userid      = ph.Userid
         AND  CAST(ph.PaymentDate AS DATE) >= ch.WeekStart
         AND  CAST(ph.PaymentDate AS DATE) <= ch.WeekEnd
    WHERE
        ph.PaymentDate >= @StartDate
        AND ph.PaymentDate <= @EndDate
        AND ph.TotalAmount > 0
        AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    ORDER BY ph.Userid, ph.PaymentDate;
END
GO
