/****** Object:  StoredProcedure [dbo].[usp_SkillGeneralEvaluatorReportBulkInsert]    Script Date: 25-06-2020 7.29.54 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SkillGeneralEvaluatorReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SkillGeneralEvaluatorReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_SkillGeneralEvaluatorReportBulkInsert]
(
      @SkillGeneralEvaluatorReportBulkData AS [dbo].[SkillGeneralEvaluatorReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[SkillGeneralEvaluatorReport](
	[RequiredLevelId],
	[Type],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[Value],
	[CandidateId],
	[IRFId])

SELECT * FROM @SkillGeneralEvaluatorReportBulkData


END
GO


