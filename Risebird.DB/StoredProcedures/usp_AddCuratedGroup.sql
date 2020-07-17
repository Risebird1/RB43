/****** Object:  StoredProcedure [dbo].[usp_AddCuratedGroup]    Script Date: 13-07-2020 10.42.56 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCuratedGroup'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCuratedGroup
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddCuratedGroup]
      @Name varchar(50),
      @CreatedOn date
      
AS
BEGIN
      SET NOCOUNT ON;
      INSERT INTO  CuratedGroup([Name], CreatedOn)
      VALUES (@Name, @CreatedOn)
      SELECT SCOPE_IDENTITY()
END

GO


