IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchKycPayoutLapseReminderTargets'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchKycPayoutLapseReminderTargets
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Active tracking rows, re-checked live against current KYC verification state.
CREATE PROCEDURE [dbo].[usp_FetchKycPayoutLapseReminderTargets]
    @AsOfDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        kt.Id,
        kt.InterviewerUserid                       AS Userid,
        ul.EmailId                                 AS Email,
        ul.FirstName,
        ul.LastName,
        kt.[Month],
        kt.PendingAmount,
        kt.ReminderCount,
        kt.CreatedOn,
        DATEDIFF(DAY, kt.CreatedOn, @AsOfDate)      AS DaysInTracking,
        ISNULL(tic.IsBankVerified, 0)               AS IsBankVerified,
        ISNULL(tic.IsPanVerified, 0)                AS IsPanVerified
    FROM KycPayoutLapseTracking kt
    JOIN UserList ul ON ul.id = kt.InterviewerUserid
    LEFT JOIN InterviewerProfile irp ON irp.UserId = ul.id
    LEFT JOIN TransInterviewerAccDetails tic ON tic.InterviewerProfileId = irp.id
    WHERE kt.TrackingStatus = 'Active';
END
GO
