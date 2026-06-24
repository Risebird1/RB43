IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveClientBillingRate'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveClientBillingRate
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Saves a new PerSuccessRate + GSTApplicable entry for a client with an effective date.
-- Applicable for InterviewerBillingType = 1 (Client billing).
--
-- Each call inserts a new history row; old rows are kept intact so that
-- revenue computed for interviews before EffectiveFrom still uses the
-- previous rate and GST setting.
-- Duplicate protection: if a row already exists for the same ClientId + EffectiveFrom,
-- it is updated in-place.
CREATE PROCEDURE [dbo].[usp_SaveClientBillingRate]
    @ClientId       INT,
    @PerSuccessRate INT,
    @GSTApplicable  BIT,
    @EffectiveFrom  DATE,
    @CreatedBy      VARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT TOP 1 Id FROM ClientBillingRateHistory
        WHERE ClientId = @ClientId AND EffectiveFrom = @EffectiveFrom
    )
    BEGIN
        UPDATE ClientBillingRateHistory
        SET PerSuccessRate = @PerSuccessRate,
            GSTApplicable  = @GSTApplicable,
            CreatedBy      = @CreatedBy,
            CreatedOn      = GETDATE()
        WHERE ClientId     = @ClientId
          AND EffectiveFrom = @EffectiveFrom
    END
    ELSE
    BEGIN
        INSERT INTO ClientBillingRateHistory
            (ClientId, PerSuccessRate, GSTApplicable, EffectiveFrom, CreatedBy, CreatedOn)
        VALUES
            (@ClientId, @PerSuccessRate, @GSTApplicable, @EffectiveFrom, @CreatedBy, GETDATE())
    END

    SELECT Id, ClientId, PerSuccessRate, GSTApplicable, EffectiveFrom, CreatedOn
    FROM ClientBillingRateHistory
    WHERE ClientId = @ClientId
    ORDER BY EffectiveFrom DESC
END
GO
