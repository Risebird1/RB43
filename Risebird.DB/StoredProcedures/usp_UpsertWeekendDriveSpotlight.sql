IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpsertWeekendDriveSpotlight'))
BEGIN
    DROP PROCEDURE dbo.usp_UpsertWeekendDriveSpotlight
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpsertWeekendDriveSpotlight]
    @IRFId INT,
    @WeekendBucket VARCHAR(20),
    @ActionsTaken VARCHAR(300) = NULL,
    @Remove BIT = 0,
    @UserEmail VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.WeekendDriveSpotlight', N'U') IS NULL
    BEGIN
        RAISERROR('WeekendDriveSpotlight table is missing. Deploy Risebird.DB/Tables/WeekendDriveSpotlight.sql', 16, 1);
        RETURN;
    END

    IF @Remove = 1
    BEGIN
        DELETE FROM dbo.WeekendDriveSpotlight
        WHERE IRFId = @IRFId AND WeekendBucket = @WeekendBucket;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.WeekendDriveSpotlight WHERE IRFId = @IRFId AND WeekendBucket = @WeekendBucket)
    BEGIN
        UPDATE dbo.WeekendDriveSpotlight
        SET ActionsTaken = ISNULL(@ActionsTaken, ActionsTaken),
            ModifiedAt = GETDATE(),
            ModifiedBy = @UserEmail
        WHERE IRFId = @IRFId AND WeekendBucket = @WeekendBucket;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.WeekendDriveSpotlight (IRFId, WeekendBucket, ActionsTaken, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
        VALUES (@IRFId, @WeekendBucket, @ActionsTaken, GETDATE(), @UserEmail, GETDATE(), @UserEmail);
    END
END
GO
