
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

	select * from ClonableSkillChannel where id = @id

End
GO


