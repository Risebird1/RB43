
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableSkillChannelMeetingPlatformById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableSkillChannelMeetingPlatformById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableSkillChannelMeetingPlatformById]  
@id int
AS  
Begin

	select MeetingPlatform from ClonableSkillChannel where id = @id

End
GO


