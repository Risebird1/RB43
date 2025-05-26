IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateApplicantById'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateApplicantById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateApplicantById]    
@id int,
@MAGIds varchar(300),
@ResumeLink varchar(max),  
@EmailId varchar(100),
@Phone varchar(20),
@ModifiedBy varchar(100),
@timeNow datetime,
@result bit out
 
AS    
Begin  

begin try
	begin tran
	select @result = 0
	if not exists (select top 1 id from UserList where EmailId = @EmailId)
	Begin
		if exists (select top 1 id from InterviewerDirectApplication where id = @id)
		Begin

			Update InterviewerDirectApplication set EmailId = @EmailId, ResumeLink = @ResumeLink, 
			Phone = @Phone, MAGIds = @MAGIds, ModifiedAt = @timeNow, ModifiedBy = @ModifiedBy, isVerified = 0, IsEmployed = null, IsAIScreeningAttempted = 0
			where id = @id
			select @result = 1
		End 
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



End
GO

