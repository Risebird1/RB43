IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchKycIncompletePayoutCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchKycIncompletePayoutCandidates
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns one row per (Interviewer, Month) with Pending/Submitted InterviewPoints
-- that are @MinAgeDays or older and whose bank+PAN KYC is not both verified,
-- excluding (Userid, Month) combinations already present in KycPayoutLapseTracking.
-- Age/month is measured off InterviewMeetings.interviewDate for Pending rows
-- (falling back to InterviewPoints.Date if there's no linked meeting), and off
-- InterviewPoints.Date directly for Submitted rows.
CREATE PROCEDURE [dbo].[usp_FetchKycIncompletePayoutCandidates]
    @AsOfDate   DATE,
    @MinAgeDays INT = 120
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH CTE AS (
        SELECT
            ip.userid,
            ip.Points,
            CASE WHEN ip.Status = 'Pending' THEN ISNULL(im.interviewDate, ip.[Date]) ELSE ip.[Date] END AS EffectiveDate
        FROM InterviewPoints ip
        LEFT JOIN InterviewMeetings im ON im.id = ip.Interviewid
        WHERE (ip.Status = 'Pending' OR ip.Status = 'Submitted')
    )
    SELECT
        ul.id                          AS Userid,
        ul.EmailId                     AS Email,
        ul.FirstName,
        ul.LastName,
        FORMAT(c.EffectiveDate, 'yyyy-MM') AS [Month],
        SUM(c.Points)                  AS PendingAmount,
        ISNULL(tic.IsBankVerified, 0)  AS IsBankVerified,
        ISNULL(tic.IsPanVerified, 0)   AS IsPanVerified
    FROM CTE c
    JOIN UserList ul ON ul.id = c.userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE
        DATEDIFF(DAY, c.EffectiveDate, @AsOfDate) >= @MinAgeDays
        AND (ISNULL(tic.IsBankVerified, 0) = 0 OR ISNULL(tic.IsPanVerified, 0) = 0)
        AND NOT EXISTS (
            SELECT 1 FROM KycPayoutLapseTracking kt
            WHERE kt.InterviewerUserid = c.userid
              AND kt.[Month] = FORMAT(c.EffectiveDate, 'yyyy-MM')
        )
    GROUP BY ul.id, ul.EmailId, ul.FirstName, ul.LastName, FORMAT(c.EffectiveDate, 'yyyy-MM'),
             ISNULL(tic.IsBankVerified, 0), ISNULL(tic.IsPanVerified, 0);
END
GO
