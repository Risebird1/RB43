/****** Object:  StoredProcedure [dbo].[usp_SkillGeneralBulkInsert]    Script Date: 24-06-2020 1.24.14 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SkillGeneralBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SkillGeneralBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_SkillGeneralBulkInsert]
(
      @SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransSkillGeneral](
	[IRFId] ,
	[LevelId],
	[Type],
	[Value],
	[Weightage])

SELECT * FROM @SkillGeneralBulkData


END
GO


