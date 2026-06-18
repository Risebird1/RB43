IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSkillChannelBillingRate'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSkillChannelBillingRate
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSkillChannelBillingRate]
    @IRFId      int,           -- IRF.id (PK)
    @BillingRate int,
    @CreatedBy  varchar(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT TOP 1 id FROM SkillChannelBillingRate WHERE IRFId = @IRFId)
    BEGIN
        UPDATE SkillChannelBillingRate
        SET BillingRate = @BillingRate,
            ModifiedOn  = GETDATE()
        WHERE IRFId = @IRFId
    END
    ELSE
    BEGIN
        INSERT INTO SkillChannelBillingRate (IRFId, BillingRate, CreatedBy, CreatedOn)
        VALUES (@IRFId, @BillingRate, @CreatedBy, GETDATE())
    END
END
GO
