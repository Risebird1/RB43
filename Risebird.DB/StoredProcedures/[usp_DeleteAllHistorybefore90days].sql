IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteAllHistorybefore90days'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteAllHistorybefore90days
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
  
Create PROCEDURE [dbo].[usp_DeleteAllHistorybefore90days]  
  
AS  
Begin  
  
Delete    
FROM ClonableSkillChannelHistory  
WHERE   
CreatedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete        
FROM InterviewCommentsHistory  
WHERE   
ModifiedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete      
FROM InterviewerProfileHistory  
WHERE   
ModifiedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete        
FROM InterviewMeetingsHistory  
WHERE   
dtCreatedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete        
FROM InterviewReportHistory  
WHERE CreatedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete   
FROM IRFHistory  
WHERE CreatedOn<DATEADD(DAY,-90, GETDATE())  
  
Delete    
FROM UserListHistory  
WHERE PerformedAt<DATEADD(DAY,-90, GETDATE())  
  
END  
  
  
Go