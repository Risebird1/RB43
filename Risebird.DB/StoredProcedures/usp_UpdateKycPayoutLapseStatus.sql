IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateKycPayoutLapseStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateKycPayoutLapseStatus
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Transitions an Active KycPayoutLapseTracking row to 'Completed' (KYC finished in time)
-- or 'Lapsed' (21-day window expired with KYC still incomplete), stamping the matching
-- date column. @Status must be 'Completed' or 'Lapsed'.
CREATE PROCEDURE [dbo].[usp_UpdateKycPayoutLapseStatus]
    @Id     INT,
    @Status VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE KycPayoutLapseTracking
    SET TrackingStatus = @Status,
        KYCCompletedOn = CASE WHEN @Status = 'Completed' THEN GETDATE() ELSE KYCCompletedOn END,
        LapsedOn       = CASE WHEN @Status = 'Lapsed'    THEN GETDATE() ELSE LapsedOn END
    WHERE Id = @Id AND TrackingStatus = 'Active';
END
GO
