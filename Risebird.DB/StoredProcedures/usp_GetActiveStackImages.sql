IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetActiveStackImages'))
BEGIN
    DROP PROCEDURE dbo.usp_GetActiveStackImages
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_GetActiveStackImages
AS
BEGIN
    SET NOCOUNT ON;
    SELECT id, Code, DisplayName, Toolchain, GalleryImageId, OsType, IsActive
    FROM StackImages
    WHERE IsActive = 1
    ORDER BY Code;
END
GO
