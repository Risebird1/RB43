IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CandidateResumeBulkUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_CandidateResumeBulkUpdate
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_CandidateResumeBulkUpdate]    
(    
      @CandidateResumeBulkUpdateData AS [dbo].[CandidateResumeUpdate_TableType_Two] READONLY    
)    
AS    
BEGIN    
   
 begin try  
  begin tran  
  
   update [dbo].[CandidateResume] set [IsProcessed] = crbd.IsProcessed, [IsResumeParsed] = crbd.IsResumeParsed, [IsResumeParsingFailed] = crbd.IsResumeParsingFailed
   from CandidateResume cr inner join @CandidateResumeBulkUpdateData crbd on crbd.id = cr.id

  commit tran  
 end try  
 begin catch  
  rollback transaction  
  raiserror('Error adding resumes', 16, 1)  
 end catch  
  
END    
GO


