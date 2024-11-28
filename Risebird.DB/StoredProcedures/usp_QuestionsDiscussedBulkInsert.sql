/****** Object:  StoredProcedure [dbo].[usp_QuestionsDiscussedBulkInsert]    Script Date: 25-06-2020 1.12.26 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_QuestionsDiscussedBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_QuestionsDiscussedBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_QuestionsDiscussedBulkInsert]
(
      @QuestionsDiscussedBulkData AS [dbo].[QuestionsDiscussedV2_TableType] READONLY
)
AS

BEGIN

SET NOCOUNT ON;
declare @interviewId int = (select top 1 InterviewId from @QuestionsDiscussedBulkData)
delete from QuestionsDiscussed where InterviewId=@interviewId
--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[QuestionsDiscussed](
	[InterviewId] ,
	[Comment],
	[RatingId],
	[Timing],
	[SkillName],
	[SkillSet])
	

SELECT * FROM @QuestionsDiscussedBulkData


END
GO


