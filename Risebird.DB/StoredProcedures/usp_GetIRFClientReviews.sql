IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFClientReviews'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFClientReviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetIRFClientReviews
@IRFId int  
AS  
Begin    
Select * From ClientIRFReviewDetails where IRFId=@IRFId
End
Go