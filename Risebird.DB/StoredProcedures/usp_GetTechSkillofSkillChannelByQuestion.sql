/****** Object:  StoredProcedure [dbo].[usp_GetUserId]    Script Date: 08-01-2020 7.40.41 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTechSkillofSkillChannelByQuestion'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTechSkillofSkillChannelByQuestion
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

    
CREATE PROCEDURE [dbo].[usp_GetTechSkillofSkillChannelByQuestion]    
@Question nvarchar(max),    
@IRFId int    
AS    
Begin   
 --select distinct top 1 sk.Name from TechSkillComments tsc join Skill sk on sk.id = tsc.Skill where IRFId=@IRFId and Comment = @Question    
   select distinct top 1 sk.Name from TechSkillComments tsc join Skill sk on sk.id = tsc.Skill where IRFId=@IRFId and     
   Comment like '%'+RTrim(LTrim((SELECT top(1) value FROM STRING_SPLIT(@Question, '.') order by value) ))  +'%'  
END    

Go
