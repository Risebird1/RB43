
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTemplateTechSkills'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetTemplateTechSkills]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_GetTemplateTechSkills]

@templateId int

AS
Begin

select * from TransTemplateTechSkill 
where TemplateId=@templateId

End



GO

