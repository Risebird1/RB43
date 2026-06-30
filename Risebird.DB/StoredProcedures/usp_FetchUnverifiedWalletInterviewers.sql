CREATE OR ALTER PROCEDURE [dbo].[usp_FetchUnverifiedWalletInterviewers]
    @AsOfDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Returns interviewers with unverified bank account AND an unclaimed/pending payout.
    -- DaysSinceFirstUnclaimed drives the reminder schedule.
    SELECT
        ul.id                               AS Userid,
        ul.EmailId                          AS Email,
        ul.FirstName,
        ul.LastName,
        MIN(ph.PaymentDate)                 AS FirstUnclaimedDate,
        SUM(ph.TotalAmount)                 AS TotalUnpaid,
        DATEDIFF(DAY, MIN(ph.PaymentDate), @AsOfDate) AS DaysSinceFirstUnclaimed,
        CAST(ph.id AS NVARCHAR(20))         AS PaymentReference
    FROM PaymentHistory ph
    JOIN UserList ul ON ul.id = ph.Userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE
        ISNULL(tic.IsBankVerified, 0) = 0
        AND ph.TotalAmount > 0
        AND ph.Status NOT IN ('Done', 'Paid')
        AND DATEDIFF(DAY, ph.PaymentDate, @AsOfDate) <= 90
    GROUP BY ul.id, ul.EmailId, ul.FirstName, ul.LastName, ph.id
    HAVING DATEDIFF(DAY, MIN(ph.PaymentDate), @AsOfDate) > 0;
END
GO
