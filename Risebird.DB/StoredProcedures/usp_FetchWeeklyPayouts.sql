CREATE OR ALTER PROCEDURE [dbo].[usp_FetchWeeklyPayouts]
    @FinancialYear NVARCHAR(9),
    @Quarter       NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    -- Determine the quarter start month and year
    DECLARE @StartYear  INT, @StartMonth INT, @StartDate DATE, @EndDate DATE;

    SET @StartYear = CAST(LEFT(@FinancialYear, 4) AS INT);

    SELECT
        @StartMonth = CASE @Quarter
            WHEN 'Quarter 1' THEN 4   -- Apr–Jun (FY year)
            WHEN 'Quarter 2' THEN 7   -- Jul–Sep (FY year)
            WHEN 'Quarter 3' THEN 10  -- Oct–Dec (FY year)
            WHEN 'Quarter 4' THEN 1   -- Jan–Mar (FY year +1)
        END;

    IF @Quarter = 'Quarter 4'
        SET @StartYear = @StartYear + 1;

    SET @StartDate = DATEFROMPARTS(@StartYear, @StartMonth, 1);
    SET @EndDate   = DATEADD(MONTH, 3, @StartDate);  -- exclusive upper bound

    -- Generate all 12 week slots
    WITH Weeks AS (
        SELECT w.WeekNumber,
               DATEADD(DAY, WeekStartOffset, DATEADD(MONTH, MonthOffset, @StartDate)) AS WeekStart,
               CASE
                   WHEN WeekInMonth < 4 THEN DATEADD(DAY, WeekStartOffset + 6, DATEADD(MONTH, MonthOffset, @StartDate))
                   ELSE DATEADD(DAY, -1, DATEADD(MONTH, MonthOffset + 1, @StartDate))
               END AS WeekEnd
        FROM (VALUES
            (1,  0, 0,  1),   -- Month 1 wk 1: day 1..7
            (2,  0, 7,  2),   -- Month 1 wk 2: day 8..14
            (3,  0, 14, 3),   -- Month 1 wk 3: day 15..21
            (4,  0, 21, 4),   -- Month 1 wk 4: day 22..EOM
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
            -- Compute WeekNumber from PaymentDate
            (DATEDIFF(MONTH, @StartDate, CAST(ph.PaymentDate AS DATE)) * 4) +
            CASE
                WHEN DAY(ph.PaymentDate) BETWEEN 1  AND 7  THEN 1
                WHEN DAY(ph.PaymentDate) BETWEEN 8  AND 14 THEN 2
                WHEN DAY(ph.PaymentDate) BETWEEN 15 AND 21 THEN 3
                ELSE 4
            END                                         AS WeekNumber,
            ph.TotalAmount,
            ph.Userid,
            --ISNULL(tic.IsBankVerified, 0)              AS IsVerified
            1 AS IsVerified
        FROM PaymentHistory ph
        JOIN UserList ul ON ul.id = ph.Userid
        LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
        LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
        WHERE
            ph.PaymentDate >= @StartDate
            AND ph.PaymentDate < @EndDate
            AND ph.TotalAmount > 0
            AND (ph.Status = 'Done' OR ph.PayoutStatus = 'TRANSFER_SUCCESS')
    )
    SELECT
        w.WeekNumber,
        CONVERT(NVARCHAR(10), w.WeekStart, 103) + ' – ' + CONVERT(NVARCHAR(10), w.WeekEnd, 103) AS WeekRange,
        ISNULL(SUM(CASE WHEN p.IsVerified = 1 THEN p.TotalAmount ELSE 0 END), 0)  AS TotalPaid,
        ISNULL(ROUND(SUM(CASE WHEN p.IsVerified = 1 THEN p.TotalAmount ELSE 0 END) * 0.10, 0), 0) AS TdsDue,
        ISNULL(COUNT(DISTINCT CASE WHEN p.IsVerified = 1 THEN p.Userid END), 0)   AS InterviewerCount,
        ISNULL(tc.IsPaid, 0)                                                       AS IsPaid,
        tc.id                                                                       AS ChallanId
    FROM Weeks w
    LEFT JOIN PayoutsByWeek p ON p.WeekNumber = w.WeekNumber
    LEFT JOIN TdsChallan tc ON tc.FinancialYear = @FinancialYear
                            AND tc.Quarter = @Quarter
                            AND tc.WeekNumber = w.WeekNumber
    GROUP BY w.WeekNumber, w.WeekStart, w.WeekEnd, tc.IsPaid, tc.id
    ORDER BY w.WeekNumber;
END
GO
