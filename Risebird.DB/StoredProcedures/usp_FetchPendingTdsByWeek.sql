CREATE OR ALTER PROCEDURE [dbo].[usp_FetchPendingTdsByWeek]
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartYear INT, @StartMonth INT, @StartDate DATE, @EndDate DATE;
    SET @StartYear = CAST(LEFT(@FinancialYear, 4) AS INT);
    SELECT @StartMonth = CASE @Quarter
        WHEN 'Quarter 1' THEN 4
        WHEN 'Quarter 2' THEN 7
        WHEN 'Quarter 3' THEN 10
        WHEN 'Quarter 4' THEN 1
    END;
    IF @Quarter = 'Quarter 4' SET @StartYear = @StartYear + 1;
    SET @StartDate = DATEFROMPARTS(@StartYear, @StartMonth, 1);
    SET @EndDate   = DATEADD(MONTH, 3, @StartDate);

    WITH Weeks AS (
        SELECT w.WeekNumber,
               CONVERT(NVARCHAR(10), DATEADD(DAY, WeekStartOffset, DATEADD(MONTH, MonthOffset, @StartDate)), 103)
               + ' – ' +
               CONVERT(NVARCHAR(10),
                   CASE
                       WHEN WeekInMonth < 4 THEN DATEADD(DAY, WeekStartOffset + 6, DATEADD(MONTH, MonthOffset, @StartDate))
                       ELSE DATEADD(DAY, -1, DATEADD(MONTH, MonthOffset + 1, @StartDate))
                   END, 103) AS WeekRange
        FROM (VALUES
            (1,  0, 0,  1),
            (2,  0, 7,  2),
            (3,  0, 14, 3),
            (4,  0, 21, 4),
            (5,  1, 0,  1),
            (6,  1, 7,  2),
            (7,  1, 14, 3),
            (8,  1, 21, 4),
            (9,  2, 0,  1),
            (10, 2, 7,  2),
            (11, 2, 14, 3),
            (12, 2, 21, 4)
        ) w(WeekNumber, MonthOffset, WeekStartOffset, WeekInMonth)
    ),
    PayoutsByWeek AS (
        SELECT
            (DATEDIFF(MONTH, @StartDate, CAST(ph.PaymentDate AS DATE)) * 4) +
            CASE
                WHEN DAY(ph.PaymentDate) BETWEEN 1  AND 7  THEN 1
                WHEN DAY(ph.PaymentDate) BETWEEN 8  AND 14 THEN 2
                WHEN DAY(ph.PaymentDate) BETWEEN 15 AND 21 THEN 3
                ELSE 4
            END AS WeekNumber,
            ph.TotalAmount,
			ROUND(ph.TotalAmount * 0.10,0) as Tds,
            --ISNULL(tic.IsBankVerified, 0) AS IsVerified
            1 AS IsVerified
        FROM PaymentHistory ph
        JOIN UserList ul ON ul.id = ph.Userid
        LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
        LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
        WHERE ph.PaymentDate >= @StartDate
          AND ph.PaymentDate < @EndDate
          AND ph.TotalAmount > 0
          AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    )
    SELECT
        w.WeekNumber,
        w.WeekRange,
		ISNULL(SUM(CASE WHEN p.IsVerified = 1 THEN p.Tds ELSE 0 END), 0) AS TdsDue,
        ISNULL(tc.TdsAmount, 0)                                                                     AS TdsDeposited,
        ISNULL(SUM(CASE WHEN p.IsVerified = 1 THEN p.Tds ELSE 0 END), 0)- ISNULL(tc.TdsAmount, 0)   AS Outstanding,
        ISNULL(tc.IsPaid, 0)                                                                        AS IsPaid
    FROM Weeks w
    LEFT JOIN PayoutsByWeek p ON p.WeekNumber = w.WeekNumber
    LEFT JOIN TdsChallan tc ON tc.FinancialYear = @FinancialYear
                            AND tc.Quarter = @Quarter
                            AND tc.WeekNumber = w.WeekNumber
    GROUP BY w.WeekNumber, w.WeekRange, tc.TdsAmount, tc.IsPaid
    ORDER BY w.WeekNumber;
END
GO
