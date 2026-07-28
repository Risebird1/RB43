IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AIInterviewStatusWebHookDataBulkDelete'))
BEGIN
    DROP PROCEDURE dbo.usp_AIInterviewStatusWebHookDataBulkDelete
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_AIInterviewStatusWebHookDataBulkDelete]    
(    
      @AIInterviewWebHookBulkData AS [dbo].[CommonId_TableType] READONLY    
)    
AS    
BEGIN    
   
 begin try  
  begin tran  
    
   delete from AIInterviewStatusWebHookData where id in (select id from @AIInterviewWebHookBulkData)
  
  commit tran  
 end try  
 begin catch  
  rollback transaction  
  raiserror('Error', 16, 1)  
 end catch  
  
END    
GO
