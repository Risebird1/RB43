IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchJDTitleByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchJDTitleByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_FetchJDTitleByIRFId
@IRFId varchar(100)
AS
BEGIN
Select JDTiltle, JDLink from IRF
where IRFId=@IRFId 
union 
Select JDTiltle, JDLink from InterviewerSkillsChannel 
where InterviewerSkillId=@IRFId
END
GO