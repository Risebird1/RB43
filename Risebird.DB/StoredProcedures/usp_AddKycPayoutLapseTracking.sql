IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddKycPayoutLapseTracking'))
BEGIN
    DROP PROCEDURE dbo.usp_AddKycPayoutLapseTracking
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Idempotent insert: only inserts if (Userid, Month) is not already tracked.
CREATE PROCEDURE [dbo].[usp_AddKycPayoutLapseTracking]
    @Userid        INT,
    @Month         VARCHAR(7),
    @Remark        VARCHAR(500) = NULL,
    @PendingAmount DECIMAL(18,2) = NULL,
    @CreatedBy     VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (
        SELECT 1 FROM KycPayoutLapseTracking
        WHERE InterviewerUserid = @Userid AND [Month] = @Month
    )
    BEGIN
        INSERT INTO KycPayoutLapseTracking
            (InterviewerUserid, [Month], Remark, ReminderCount, PendingAmount, TrackingStatus, CreatedOn, CreatedBy)
        VALUES
            (@Userid, @Month, @Remark, 0, @PendingAmount, 'Active', GETDATE(), @CreatedBy);
        SELECT 1 AS Inserted;
    END
    ELSE
    BEGIN
        SELECT 0 AS Inserted;
    END
END
GO
