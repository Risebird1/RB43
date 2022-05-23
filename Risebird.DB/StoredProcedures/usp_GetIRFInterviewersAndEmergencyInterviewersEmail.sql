/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewersAndEmergencyInterviewersEmail] ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewersAndEmergencyInterviewersEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewersAndEmergencyInterviewersEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetIRFInterviewersAndEmergencyInterviewersEmail]  
@IRFId int  
AS  
Begin  
  
  select distinct ul.EmailId from TransInterviewer ti 
  join UserList ul on ul.id = ti.UserId
  where ti.IRFId = @IRFId and ti.IsActive = 1 and ti.IsAvailable=1 and ul.IsActive = 1 
  Union
  select distinct ul.EmailId from TransEmergencyInterviewer tei 
  join UserList ul on ul.id = tei.UserId
  where tei.IRFId = @IRFId and tei.IsActive = 1 and ul.IsActive = 1 
  
  
End  
GO


