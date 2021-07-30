IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllTemplates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllTemplates
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetAllTemplates]  
AS  
Begin 

select sct.*, ul.EmailId as CreatedByEmailId from SkillChannelTemplate sct join UserList ul on ul.id = sct.CreatedBy
where IsDeleted = 0 order by CreatedOn desc

End
GO


