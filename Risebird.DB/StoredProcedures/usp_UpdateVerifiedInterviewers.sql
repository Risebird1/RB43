IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateVerifiedInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVerifiedInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateVerifiedInterviewers]    
(    
	@smeEmail varchar(100),
	@InterviewerBulkData AS [dbo].[TransSMEVerifiedInterviewer_TableType] READONLY
	
)    
AS    
BEGIN    
  
  
 begin try  
  begin tran  
   
   if exists (select top 1 id from @InterviewerBulkData)
   Begin

	   update TransInterviewer set IsSMEVerified = ibd.IsSMEVerified from TransInterviewer ti 
	   inner join @InterviewerBulkData ibd on ibd.id = ti.id

	   declare @interviewerProfileId int,  @irfId int

	   set @interviewerProfileId = (select ipr.id from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId where EmailId like @smeEmail)
   
	   set @irfId = (select IRFId from TransInterviewer where id = (select top 1 id from @InterviewerBulkData))

	   if not exists (select top 1 id from SMEActivityTracker where IRFId =@irfId and InterviewerProfileId = @interviewerProfileId)
		Begin
			insert into SMEActivityTracker (IRFId, InterviewerProfileId, AreInterviewersVerified)
			values (@irfId, @interviewerProfileId, 1)
		End
		Else
		Begin
			update SMEActivityTracker set AreInterviewersVerified = 1 where InterviewerProfileId = @interviewerProfileId and IRFId = @irfId
		End

   ENd


   

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


