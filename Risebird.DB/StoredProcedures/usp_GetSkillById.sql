IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSkillById]  
  
@id int  
  
AS  
Begin  
  
SELECT id, [Name]  
     ,[Description]    
FROM [dbo].[Skill]  
WHERE  
      Id = @id  
End  
  
  
  