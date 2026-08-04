IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetWeekendDriveSpotlight'))
BEGIN
    DROP PROCEDURE dbo.usp_GetWeekendDriveSpotlight
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetWeekendDriveSpotlight]
    @bucket VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.WeekendDriveSpotlight', N'U') IS NULL
    BEGIN
        SELECT CAST(NULL AS INT) AS id,
               CAST(NULL AS INT) AS IRFId,
               CAST(NULL AS VARCHAR(20)) AS WeekendBucket,
               CAST(NULL AS VARCHAR(300)) AS ActionsTaken,
               CAST(NULL AS DATETIME) AS CreatedAt,
               CAST(NULL AS VARCHAR(150)) AS CreatedBy,
               CAST(NULL AS DATETIME) AS ModifiedAt,
               CAST(NULL AS VARCHAR(150)) AS ModifiedBy
        WHERE 1 = 0;
        RETURN;
    END

    SELECT id, IRFId, WeekendBucket, ActionsTaken, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
    FROM dbo.WeekendDriveSpotlight
    WHERE WeekendBucket = @bucket
    ORDER BY IRFId;
END
GO
