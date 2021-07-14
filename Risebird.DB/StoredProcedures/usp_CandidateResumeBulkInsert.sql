IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CandidateResumeBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_CandidateResumeBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_CandidateResumeBulkInsert]  
(  
      @CandidateResumeBulkData AS [dbo].[CandidateResume_TableType] READONLY  
)  
AS  
BEGIN  
 
	begin try
		begin tran

			INSERT INTO [dbo].[CandidateResume]( 
			[CandidateId],
			[IRFId] ,  
			[ResumeLink],  
			[AddedBy],  
			[AddedOn])  
  
			SELECT * FROM @CandidateResumeBulkData 

		commit tran
	end try
	begin catch
		rollback transaction
		raiserror('Error adding resumes', 16, 1)
	end catch

END  
GO


