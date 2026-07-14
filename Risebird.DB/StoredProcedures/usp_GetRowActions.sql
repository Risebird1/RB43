IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRowActions'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRowActions
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetRowActions]
    @refType varchar(20),
    @refId varchar(20) = NULL,
    @refIds varchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.RowActionLog', N'U') IS NULL
    BEGIN
        SELECT
            CAST('' AS varchar(20)) AS RefId,
            CAST('' AS varchar(500)) AS ActionsTaken
        WHERE 1 = 0;
        RETURN;
    END

    SELECT
        RefId,
        ActionsTaken
    FROM dbo.RowActionLog
    WHERE RefType = @refType
      AND (
            (@refId IS NOT NULL AND RefId = @refId)
         OR (@refIds IS NOT NULL AND RefId IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@refIds, ',')))
         OR (@refId IS NULL AND (@refIds IS NULL OR LTRIM(RTRIM(@refIds)) = ''))
      );
END
GO
