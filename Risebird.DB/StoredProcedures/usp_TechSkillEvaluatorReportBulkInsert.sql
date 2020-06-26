/****** Object:  StoredProcedure [dbo].[usp_TechSkillEvaluatorReportBulkInsert]    Script Date: 25-06-2020 7.29.31 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TechSkillEvaluatorReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_TechSkillEvaluatorReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_TechSkillEvaluatorReportBulkInsert]
(
      @TechSkillEvaluatorReportBulkData AS [dbo].[TechSkillEvaluatorReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TechSkillEvaluatorReport](
	[RequiredLevelId],
	[SkillId],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[CandidateId],
	[IRFId])

SELECT * FROM @TechSkillEvaluatorReportBulkData


END
GO


