IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SubmitInterviewerApplication'))
BEGIN
    DROP PROCEDURE dbo.usp_SubmitInterviewerApplication
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SubmitInterviewerApplication]    

@IRFId int,
@ResumeLink varchar(max),  
@EmailId varchar(100),
@Phone varchar(20),
@ReferredBy varchar(200)=null,
@timeNow datetime
 
AS    
Begin  

begin try
	begin tran
	if not exists (select top 1 id from UserList where EmailId = @EmailId)
	Begin
		if not exists (select top 1 id from InterviewerDirectApplication where irfid = @IRFId and EmailId = RTRIM(LTRIM(@EmailId)))
		Begin
			Insert into InterviewerDirectApplication(IRFId, EmailId, Phone, ResumeLink, CreatedAt, CreatedBy, ReferredBy)  
			values(@IRFId, @EmailId, @Phone, @ResumeLink, @timeNow, @EmailId, @ReferredBy) 
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

