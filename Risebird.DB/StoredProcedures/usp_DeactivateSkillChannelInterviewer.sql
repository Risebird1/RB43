/****** Object:  StoredProcedure [dbo].[usp_DeactivateSkillChannelInterviewer]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeactivateSkillChannelInterviewer'))
BEGIN
    DROP PROCEDURE [dbo].[usp_DeactivateSkillChannelInterviewer]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_DeactivateSkillChannelInterviewer]
@irfId int,
@interviewerEmail varchar(100)
AS
BEGIN
  Declare @interviewerUserId int = (select id from UserList where EmailId = @interviewerEmail)
		
	IF EXISTS (SELECT 1 FROM TransInterviewer where IRFId = @irfId and UserId = @interviewerUserId) 
	BEGIN
		update TransInterviewer set IsActive = 0,IsAvailable=0 where IRFId = @irfId and UserId = @interviewerUserId
	END
END
GO

