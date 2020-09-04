/****** Object:  StoredProcedure [dbo].[usp_VideoSentimentBulkInsert]    Script Date: 28-08-2020 1.01.03 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_VideoSentimentBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_VideoSentimentBulkInsert
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_VideoSentimentBulkInsert]
(
      @VideoSentimentBulkData AS [dbo].[VideoSentiment_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;
 
--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[VideoSentiment](
	[InterviewId] ,
	[ImageUrl],
	[EmotionType],
	[Value])
	
SELECT * FROM @VideoSentimentBulkData


END
GO


