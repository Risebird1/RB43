
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTemplateById'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetTemplateById]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetTemplateById]

@id int

AS
Begin

select * from SkillChannelTemplate where Id = @id

End



GO

