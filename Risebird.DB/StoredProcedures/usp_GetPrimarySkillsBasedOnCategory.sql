IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetPrimarySkillsBasedOnCategory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPrimarySkillsBasedOnCategory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetPrimarySkillsBasedOnCategory]
@categoryId int

AS
Begin

SELECT 
   distinct trim(value) phone
FROM 
    AssessmentGroup
    CROSS APPLY STRING_SPLIT(PrimarySkills, ',') where Categoryid=@categoryId
End

GO


