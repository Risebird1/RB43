IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFRecommendation'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFRecommendation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UpdateIRFRecommendation] 
@id int,
@IRFId int,
@Status bit,
@CategoryId int,
@CompetencyLevelId int,
@SeniorityLevelId int,
@CountryType int,
@ModifiedOn datetime

AS      
Begin      
	IF EXISTS(Select 1 from IRFRecommendations where id = @id)     
	BEGIN
		update IRFRecommendations set 
		[Status] = @Status, CategoryId = @CategoryId, CompetencyLevelId = @CompetencyLevelId, SeniorityLevelId = @SeniorityLevelId,
		CountryType = @CountryType, ModifiedOn = @ModifiedOn where id = @id
		   
	END    
END   
   



GO


