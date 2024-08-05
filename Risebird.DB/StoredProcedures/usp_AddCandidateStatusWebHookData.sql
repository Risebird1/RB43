IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddCandidateStatusWebHookData'))
BEGIN
    DROP PROCEDURE dbo.usp_AddCandidateStatusWebHookData
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
Create PROCEDURE [dbo].[usp_AddCandidateStatusWebHookData]    
(    
    
   @candidateEmailId varchar(200),  
   @IRFName varchar(25),
   @status varchar(50) = null
)    
AS    
BEGIN    
  
 begin try  
  begin tran  

  if not exists (select top 1* from CandidateStatusWebHookData where IRFId = @IRFName and CandidateEmailId = @candidateEmailId)
  Begin
	insert into CandidateStatusWebHookData values (@IRFName, @candidateEmailId, @status, getdate())
  End

    
  commit tran  
 end try  
 begin catch  
  
  rollback transaction  
  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),  
   @Severity int = ERROR_SEVERITY(),  
   @State smallint = ERROR_STATE()  
   
  RAISERROR (@Message, @Severity, @State)  
   
 end catch  
  
    
END    
GO


