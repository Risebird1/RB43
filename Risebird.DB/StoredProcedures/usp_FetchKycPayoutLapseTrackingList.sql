IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchKycPayoutLapseTrackingList'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchKycPayoutLapseTrackingList
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- KycPayoutLapseTracking listing (Active/Lapsed only -- Completed rows are excluded) for the
-- Finance "Payment Lapsed" page, joined with interviewer/KYC details for display.
CREATE PROCEDURE [dbo].[usp_FetchKycPayoutLapseTrackingList]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        kt.Id,
        kt.InterviewerUserid           AS Userid,
        ul.EmailId                     AS Email,
        ul.FirstName,
        ul.LastName,
        tic.PAN,
        tic.NameAtPan,
        tic.NameAtBank,
        kt.[Month],
        kt.PendingAmount,
        kt.ReminderCount,
        kt.TrackingStatus,
        kt.Remark,
        kt.CreatedOn,
        kt.CreatedBy,
        kt.LastReminderOn,
        kt.KYCCompletedOn,
        kt.LapsedOn,
        ISNULL(tic.IsBankVerified, 0)  AS IsBankVerified,
        ISNULL(tic.IsPanVerified, 0)   AS IsPanVerified
    FROM KycPayoutLapseTracking kt
    JOIN UserList ul ON ul.id = kt.InterviewerUserid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE kt.TrackingStatus <> 'Completed'
    ORDER BY kt.CreatedOn DESC;
END
GO
