/****** Object:  StoredProcedure [dbo].[usp_EmergencyInterviewerBulkInsert]    Script Date: 24-06-2020 1.32.45 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_EmergencyInterviewerBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_EmergencyInterviewerBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_EmergencyInterviewerBulkInsert]  
(  
      @EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  DELETE from TransEmergencyInterviewer where IRFID in (Select IRFId From @EmergencyInterviewerBulkData)
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[TransEmergencyInterviewer](  
 [IRFId] ,  
 [UserId],  
 [PerSuccessRate], 
 [IsActive],
 [InterviewerId])  
   
  
--SELECT eibd.*, Interviewer.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join Interviewer on eibd.UserId = Interviewer.UserId  
SELECT eibd.*, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId  
  
  
  
END  
GO


