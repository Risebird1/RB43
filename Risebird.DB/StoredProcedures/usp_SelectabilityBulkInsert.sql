/****** Object:  StoredProcedure [dbo].[usp_SelectabilityBulkInsert]    Script Date: 24-06-2020 1.22.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SelectabilityBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SelectabilityBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SelectabilityBulkInsert]
(
      @SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransSelectability](
	[IRFId] ,
	[LevelId],
	[Type],
	[Value],
	[Weightage])

SELECT * FROM @SelectabilityBulkData


END
GO


