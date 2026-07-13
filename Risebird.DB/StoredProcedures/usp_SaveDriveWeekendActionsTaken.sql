IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveDriveWeekendActionsTaken'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveDriveWeekendActionsTaken
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveDriveWeekendActionsTaken]
    @irfIntId int,
    @weekendStart date,
    @actionsTaken varchar(500),
    @updatedBy varchar(200)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.DriveWeekendActionsTaken', N'U') IS NULL
    BEGIN
        RETURN;
    END

    MERGE dbo.DriveWeekendActionsTaken AS target
    USING (SELECT @irfIntId AS IrfIntId, @weekendStart AS WeekendStart) AS source
        ON target.IrfIntId = source.IrfIntId
       AND target.WeekendStart = source.WeekendStart
    WHEN MATCHED THEN
        UPDATE SET
            ActionsTaken = @actionsTaken,
            UpdatedBy = @updatedBy,
            UpdatedOn = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (IrfIntId, WeekendStart, ActionsTaken, UpdatedBy, UpdatedOn)
        VALUES (@irfIntId, @weekendStart, @actionsTaken, @updatedBy, GETDATE());
END
GO
