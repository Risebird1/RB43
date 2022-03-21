IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFRecommendation'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFRecommendation
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetIRFRecommendation] 
@IRFId int

AS      
Begin      
	select * from IRFRecommendations where IRFId = @IRFId   
END   
   



GO


