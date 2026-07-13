IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetDriveWeekendActionsTaken'))
BEGIN
    DROP PROCEDURE dbo.usp_GetDriveWeekendActionsTaken
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetDriveWeekendActionsTaken]
    @weekendStart date
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.DriveWeekendActionsTaken', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS int) AS IrfIntId, CAST('' AS varchar(500)) AS ActionsTaken WHERE 1 = 0;
        RETURN;
    END

    SELECT
        IrfIntId,
        ActionsTaken
    FROM dbo.DriveWeekendActionsTaken
    WHERE WeekendStart = @weekendStart;
END
GO
