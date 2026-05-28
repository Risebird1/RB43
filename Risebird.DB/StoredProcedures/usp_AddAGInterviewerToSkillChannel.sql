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
	 DECLARE @isLocked bit = ISNULL((SELECT DmIvLockNew FROM IRF WHERE id = @irfId), 0)
	 Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, PerSuccessRate, ActivatedAt, ModifiedBy)
	 values(@irfId,(select id from InterviewerProfile where UserId = @userId),@UserId,CASE WHEN @isLocked = 1 THEN 0 ELSE 1 END,1, (select FlatRate from irf where id = @irfId), CASE WHEN @isLocked = 1 THEN NULL ELSE @timeNow END, @AddedBy) 
	End
End
Go