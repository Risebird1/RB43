IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddTargetSkillChannelToInterviewerPreOnboarding'))
BEGIN
    DROP PROCEDURE dbo.usp_AddTargetSkillChannelToInterviewerPreOnboarding
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_AddTargetSkillChannelToInterviewerPreOnboarding
@userId int,
@targetIrfId int
AS
BEGIN

if(@TargetIrfId > 0)
Begin
 if not exists (select 1 from TransInterviewer where IRFId =@TargetIrfId and UserId = @UserId)
 Begin
  Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, PerSuccessRate)
  values(@TargetIrfId,(select id from InterviewerProfile where UserId = @userId),@UserId,0,1, (select FlatRate from irf where id = @TargetIrfId)) 
 End
End


END
Go