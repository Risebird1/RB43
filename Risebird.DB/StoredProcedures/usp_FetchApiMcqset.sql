IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchApiMcqset'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchApiMcqset
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchApiMcqset
@PrimarySkill varchar(50)
As
BEGIN
Select * from MCQSets where PrimarySkills Like '%'+@PrimarySkill+'%' 
END
GO