
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableSkillChannelById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableSkillChannelById
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableSkillChannelById]  
@id int
AS  
Begin

	select csc.id as ClonableSkillChannelId, csc.[Name] as TemplateName, csc.CreatedFromIRFId, csc.IsJoinSelectGenskillsRequired as IsTemplateJoinSelectGenskillsRequired, csc.MeetingPlatform   
	from ClonableSkillChannel csc where id = @id

End
GO


