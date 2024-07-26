IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CandidateWebHookDataBulkDelete'))
BEGIN
    DROP PROCEDURE dbo.usp_CandidateWebHookDataBulkDelete
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_CandidateWebHookDataBulkDelete]    
(    
      @CandidateWebHookBulkData AS [dbo].[CommonId_TableType] READONLY    
)    
AS    
BEGIN    
   
 begin try  
  begin tran  
    
   delete from CandidateStatusWebHookData where id in (select id from @CandidateWebHookBulkData)
  
  commit tran  
 end try  
 begin catch  
  rollback transaction  
  raiserror('Error', 16, 1)  
 end catch  
  
END    
GO


