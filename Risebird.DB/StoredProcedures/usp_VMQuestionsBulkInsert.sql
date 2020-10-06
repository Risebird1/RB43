/****** Object:  StoredProcedure [dbo].[usp_VMUsersBulkInsert]    Script Date: 02-10-2020 6.51.32 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_VMQuestionsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_VMQuestionsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_VMQuestionsBulkInsert]
(
      @VMQuestionsBulkData AS [dbo].[ProctorVMQuestions_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[ProctorVMQuestions](
	[VmId],
	[Priority],
	[Question],
	[Detail],
	[VideoLink],
	[Time],
	[Level],
	[Skill])

SELECT * FROM @VMQuestionsBulkData


END
GO


