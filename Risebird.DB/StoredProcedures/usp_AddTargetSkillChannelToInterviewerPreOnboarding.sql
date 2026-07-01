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
  -- Channel activation follows the channel's Interviewer Lock (IRF.DmIvLockNew):
  --   Locked  (DmIvLockNew = 1) → Available-only (IsActive = 0) pending manual RB vetting.
  --   Unlocked(DmIvLockNew = 0) → auto-activated (IsActive = 1, RB-vetted, Client-shortlisted) now.
  DECLARE @isLocked bit = ISNULL((SELECT DmIvLockNew FROM IRF WHERE id = @TargetIrfId), 0)
  Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable, IsSMEVerified, ClientAction, PerSuccessRate, ActivatedAt)
  values(@TargetIrfId,(select id from InterviewerProfile where UserId = @userId),@UserId,
   CASE WHEN @isLocked = 1 THEN 0 ELSE 1 END, 1,
   CASE WHEN @isLocked = 1 THEN 0 ELSE 1 END,
   CASE WHEN @isLocked = 1 THEN NULL ELSE 'Shortlisted' END,
   (select FlatRate from irf where id = @TargetIrfId),
   CASE WHEN @isLocked = 1 THEN NULL ELSE GETDATE() END) 
 End
End


END
Go