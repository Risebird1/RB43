/****** Object:  StoredProcedure [dbo].[usp_InterviewerBulkInsert]    Script Date: 24-06-2020 1.24.52 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create PROCEDURE [dbo].[usp_InterviewerBulkInsert]    
(    
      @InterviewerBulkData AS [dbo].[TransInterviewer_TableType] READONLY    
)    
AS    
BEGIN    
    
SET NOCOUNT ON;    
  DELETE from TransInterviewer where IRFID in (Select IRFId From @InterviewerBulkData)   
--This row performs bulk copy of object DataTable to Table in SQL    
INSERT INTO [dbo].[TransInterviewer](    
 [IRFId] ,    
 [UserId],    
 [PerSuccessRate],   
 [IsActive],  
 [IsAvailable],  
 [InterviewerId])    
    
    
SELECT distinct ibd.*, InterviewerProfile.id as InterviewerId FROM @InterviewerBulkData ibd join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
WHERE 
NOT EXISTS (
	SELECT 1
	FROM [dbo].[TransInterviewer] ti
	WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId
);
    
END   
GO


