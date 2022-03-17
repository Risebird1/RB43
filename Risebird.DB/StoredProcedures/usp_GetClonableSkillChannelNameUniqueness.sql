
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableSkillChannelNameUniqueness'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableSkillChannelNameUniqueness
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableSkillChannelNameUniqueness]
@value varchar(100)

AS
Begin


 select * from ClonableSkillChannel where [Name] = @value
		


End

GO


