/****** Object:  StoredProcedure [dbo].[usp_ResumeEvaluatorBulkInsert]    Script Date: 24-06-2020 1.33.27 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ResumeEvaluatorBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_ResumeEvaluatorBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_ResumeEvaluatorBulkInsert]
(
      @ResumeEvaluatorBulkData AS [dbo].[TransResumeEvaluator_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransResumeEvaluator](
	[IRFId] ,
	[ResumeEvaluatorId])
	

SELECT rebd.IRFId, ResumeEvaluator.id as ResumeEvaluatorId FROM @ResumeEvaluatorBulkData rebd left join ResumeEvaluator on rebd.UserId = ResumeEvaluator.UserId


END
GO


