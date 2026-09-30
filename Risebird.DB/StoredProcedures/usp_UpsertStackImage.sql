IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpsertStackImage'))
BEGIN
    DROP PROCEDURE dbo.usp_UpsertStackImage
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_UpsertStackImage
    @Code varchar(50),
    @DisplayName varchar(200),
    @Toolchain varchar(500),
    @GalleryImageId varchar(500),
    @OsType varchar(25)
AS
BEGIN
    SET NOCOUNT ON;
    MERGE StackImages AS t
    USING (SELECT @Code AS Code) AS s ON t.Code = s.Code
    WHEN MATCHED THEN UPDATE SET DisplayName = @DisplayName, Toolchain = @Toolchain, GalleryImageId = @GalleryImageId, OsType = @OsType, IsActive = 1
    WHEN NOT MATCHED THEN INSERT (Code, DisplayName, Toolchain, GalleryImageId, OsType, IsActive)
                          VALUES (@Code, @DisplayName, @Toolchain, @GalleryImageId, @OsType, 1);
END
GO
