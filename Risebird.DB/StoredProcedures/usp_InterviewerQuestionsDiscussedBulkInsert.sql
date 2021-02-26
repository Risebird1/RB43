/****** Object:  StoredProcedure [dbo].[usp_InterviewerQuestionsDiscussedBulkInsert]    Script Date: 22-01-2021 3.27.20 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_InterviewerQuestionsDiscussedBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerQuestionsDiscussedBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_InterviewerQuestionsDiscussedBulkInsert]  
(  
      @InterviewerQuestionsDiscussedBulkData AS [dbo].[InterviewerQuestionsDiscussed_TableType] READONLY  
)  
AS  
  
BEGIN  
  
SET NOCOUNT ON;  
  
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[InterviewerQuestionsDiscussed](  
 [InterviewId] ,  
 [Question],  
 [RatingId],
 [Timing])  
   
  
SELECT * FROM @InterviewerQuestionsDiscussedBulkData  
  
  
END  


GO


