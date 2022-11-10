IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAadhaarDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAadhaarDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetAadhaarDetails]    
@MCQId varchar(50),     
@candidateEmail varchar(200)
AS    
Begin   
Select *,user_image as passport_image From AadhaarDetails where reference_id=@MCQId and email=@candidateEmail
End  
Go  
  