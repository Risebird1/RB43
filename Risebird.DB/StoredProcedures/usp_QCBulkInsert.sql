/****** Object:  StoredProcedure [dbo].[usp_QCBulkInsert]    Script Date: 24-06-2020 1.33.11 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_QCBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_QCBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



  
  
  
Create PROCEDURE [dbo].[usp_QCBulkInsert]  
(  
      @QCBulkData AS [dbo].[TransQualityController_TableType] READONLY  
)  
AS  
BEGIN  
  
SET NOCOUNT ON;  
  DELETE from TransQualityController where IRFID in (Select IRFId From @QCBulkData)
--This row performs bulk copy of object DataTable to Table in SQL  
INSERT INTO [dbo].[TransQualityController](  
 [IRFId] ,  
 [UserId],  
 [PerSuccessRate],
 [IsActive],
 [InterviewerId])  
   
  
--SELECT qcbd.*, Interviewer.id as InterviewerId FROM @QCBulkData qcbd left join Interviewer on qcbd.UserId = Interviewer.UserId  
  
SELECT qcbd.*, InterviewerProfile.id as InterviewerId FROM @QCBulkData qcbd left join InterviewerProfile on qcbd.UserId = InterviewerProfile.UserId  
  
  
  
END  
GO


