/****** Object:  StoredProcedure [dbo].[usp_AddIRFRecommendation]    Script Date: 3/3/2022 4:50:22 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFRecommendation'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFRecommendation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_AddIRFRecommendation]   
@IRFId int,
@Status bit,
@CategoryId int,
@CompetencyLevelId int,
@SeniorityLevelId int,
@CountryType int,
@CreatedOn datetime

AS      
Begin      
	IF NOT EXISTS(Select 1 from IRFRecommendations where IRFId = @IRFId)     
	BEGIN
		Insert into [IRFRecommendations](IRFId, Status, CategoryId, CompetencyLevelId, SeniorityLevelId, CountryType, CreatedOn)    
		values (@IRFId, @Status, @CategoryId, @CompetencyLevelId, @SeniorityLevelId, @CountryType, @CreatedOn)    
	END    
END   
   



GO


