  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddAGInterviewerToSkillChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_AddAGInterviewerToSkillChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddAGInterviewerToSkillChannel]    
@userId int,  
@irfId int,
@timeNow datetime,
@AddedBy varchar(100) = null  
AS    
Begin
	if not exists (select 1 from TransInterviewer where IRFId =@irfId and UserId = @userId)
	Begin
	 Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, PerSuccessRate, ActivatedAt, ModifiedBy)
	 values(@irfId,(select id from InterviewerProfile where UserId = @userId),@UserId,1,1, (select FlatRate from irf where id = @irfId), @timeNow, @AddedBy) 
	End
End
Go