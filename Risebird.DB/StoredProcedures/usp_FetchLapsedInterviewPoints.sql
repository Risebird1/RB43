IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchLapsedInterviewPoints'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchLapsedInterviewPoints
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns one row per (Interviewer, Month) with InterviewPoints rows whose Status = 'Lapsed',
-- for the Finance "Payment Lapsed" page's "Lapsed" tab. Month is measured off
-- InterviewMeetings.interviewDate when available, falling back to InterviewPoints.Date.
-- @FromDate/@ToDate filter on that same effective date (inclusive) and are required --
-- the caller defaults them to a 1-month window when no filter is supplied by the user.
CREATE PROCEDURE [dbo].[usp_FetchLapsedInterviewPoints]
    @FromDate DATE,
    @ToDate   DATE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH CTE AS (
        SELECT
            ip.userid,
            ip.Points,
            ip.PaymentHistoryId,
            ISNULL(im.interviewDate, ip.[Date]) AS EffectiveDate
        FROM InterviewPoints ip
        LEFT JOIN InterviewMeetings im ON im.id = ip.Interviewid
        WHERE ip.Status = 'Lapsed'
          AND ISNULL(im.interviewDate, ip.[Date]) >= @FromDate
          AND ISNULL(im.interviewDate, ip.[Date]) < DATEADD(DAY, 1, @ToDate)
    )
    SELECT
        ul.id                          AS Userid,
        ul.EmailId                     AS Email,
        ul.FirstName,
        ul.LastName,
        tic.PAN,
        tic.NameAtPan,
        tic.NameAtBank,
        FORMAT(c.EffectiveDate, 'yyyy-MM') AS [Month],
        SUM(c.Points)                  AS LapsedAmount,
        ISNULL(tic.IsBankVerified, 0)  AS IsBankVerified,
        ISNULL(tic.IsPanVerified, 0)   AS IsPanVerified,
        MAX(c.PaymentHistoryId)        AS PaymentHistoryId
    FROM CTE c
    JOIN UserList ul ON ul.id = c.userid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    GROUP BY ul.id, ul.EmailId, ul.FirstName, ul.LastName, tic.PAN, tic.NameAtPan, tic.NameAtBank,
             FORMAT(c.EffectiveDate, 'yyyy-MM'), ISNULL(tic.IsBankVerified, 0), ISNULL(tic.IsPanVerified, 0)
    ORDER BY [Month] DESC;
END
GO
