/****** Object:  StoredProcedure [dbo].[usp_TechSkillReportBulkInsert]    Script Date: 25-06-2020 1.12.08 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TechSkillReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_TechSkillReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_TechSkillReportBulkInsert]
(
      @TechSkillReportBulkData AS [dbo].[TechSkillReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TechSkillReport](
	[InterviewId] ,
	[RequiredLevelId],
	[SkillId],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[TechComments])

SELECT * FROM @TechSkillReportBulkData


END
GO


