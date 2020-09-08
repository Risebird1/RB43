 
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddorEditQuestionsDetails'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddorEditQuestionsDetails]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_AddorEditQuestionsDetails]  
(  
      @QuestionsDetailsBulkData AS [dbo].[AddQuestionDetails_TableType] READONLY  
)  
AS  
  
BEGIN  
  
SET NOCOUNT ON;  
  
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[VideoQuestionDetails](  
 Interviewid,
VideoLink,
VideoTitle,
Priority,
Question)  
SELECT Interviewid,VideoLink,VideoTitle,Priority,Question FROM @QuestionsDetailsBulkData Where id=0  

Update vq Set vq.VideoLink=nt.VideoLink,vq.VideoTitle=nt.VideoTitle,vq.Priority=nt.Priority,vq.Question=nt.Question 
FROM 
    VideoQuestionDetails vq
    JOIN @QuestionsDetailsBulkData nt ON nt.Interviewid=vq.Interviewid and nt.id=vq.id
where nt.id!=0
  
END
