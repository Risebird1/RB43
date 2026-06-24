IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerActiveStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerActiveStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateInterviewerActiveStatus
@userId int,
@status int,
@ActivateChannels bit = 1   -- when 0, the interviewer's skill channels are left Available but NOT activated (pending manual review)
--@targetIrf int
AS
BEGIN

declare @hmVideoInstructionLink varchar(1000)

if(@status = 1)
Begin
	
	set @hmVideoInstructionLink = (select AudioComment from IRF where id = (select top 1 IRFId from TransInterviewer where UserId = @userId))

	update UserList set IsActive = 1, InactivationType = NULL, InactivatedBy = NULL, InactivatedAt = NULL where id = @userId

	-- Skill channels are only auto-activated for non-funnel onboarding. For interviewers coming
	-- through the NaukriOnboarding / availability funnels (@ActivateChannels = 0) the channels stay
	-- Available (IsActive = 0) and must be activated manually by an operator after review.
	if(@ActivateChannels = 1)
		update TransInterviewer set IsActive = 1 where UserId = @userId

	select EmailId, UserPassword, @hmVideoInstructionLink as HMVideoForTargetIRF, uniqueid from UserList where id = @userId
end

END
Go