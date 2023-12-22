IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_InterviewerCodedResumeBulkUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_InterviewerCodedResumeBulkUpdate
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_InterviewerCodedResumeBulkUpdate]    
(    
      @InterviewerCodedResumeBulkUpdateData AS [dbo].[InterviewerCodedResumeUpdate_TableType] READONLY    
)    
AS    
BEGIN    
   
 begin try  
  begin tran  
  
   update [dbo].[InterviewerProfile] set [IsResumeRedactAttempted] = icrbd.IsResumeRedactAttempted, [IsResumeRedactFailed] = icrbd.IsResumeRedactFailed, [CodedResumeLink] = icrbd.CodedResumeLink
   from InterviewerProfile ipr inner join @InterviewerCodedResumeBulkUpdateData icrbd on icrbd.id = ipr.id

  commit tran  
 end try  
 begin catch  
  rollback transaction  
  raiserror('Error adding resumes', 16, 1)  
 end catch  
  
END    
GO


