IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MarkKycPayoutLapseReminderSent'))
BEGIN
    DROP PROCEDURE dbo.usp_MarkKycPayoutLapseReminderSent
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MarkKycPayoutLapseReminderSent]
    @Id            INT,
    @ReminderCount INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE KycPayoutLapseTracking
    SET ReminderCount = @ReminderCount, LastReminderOn = GETDATE()
    WHERE Id = @Id AND TrackingStatus = 'Active';
END
GO
