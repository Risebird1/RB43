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
@status int
--@targetIrf int
AS
BEGIN

declare @hmVideoInstructionLink varchar(1000)

if(@status = 1)
Begin
	
	set @hmVideoInstructionLink = (select AudioComment from IRF where id = (select top 1 IRFId from TransInterviewer where UserId = @userId))

	update UserList set IsActive = 1 where id = @userId

	select EmailId, UserPassword, @hmVideoInstructionLink as HMVideoForTargetIRF from UserList where id = @userId
end

END
Go