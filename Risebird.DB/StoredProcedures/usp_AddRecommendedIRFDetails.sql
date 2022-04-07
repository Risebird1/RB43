IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AddRecommendedIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddRecommendedIRFDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_AddRecommendedIRFDetails]
@IRFId int,
@CreatedBy varchar(150)
AS
BEGIN
if not exists (select 1 from RecommednedSkillChannel where IRFId =@IRFId)       
BEGIN
INSERT into RecommednedSkillChannel([IRFId],[CreatedBy],[CreatedOn]) values(@IRFId,@CreatedBy,GETDATE())
END
END
GO