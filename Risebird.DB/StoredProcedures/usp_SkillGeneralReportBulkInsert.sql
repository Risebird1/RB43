/****** Object:  StoredProcedure [dbo].[usp_SkillGeneralReportBulkInsert]    Script Date: 25-06-2020 1.11.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SkillGeneralReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SkillGeneralReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_SkillGeneralReportBulkInsert]
(
      @SkillGeneralReportBulkData AS [dbo].[SkillGeneralReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[SkillGeneralReport](
	[InterviewId] ,
	[RequiredLevelId],
	[Type],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[Value])

SELECT * FROM @SkillGeneralReportBulkData


END
GO


