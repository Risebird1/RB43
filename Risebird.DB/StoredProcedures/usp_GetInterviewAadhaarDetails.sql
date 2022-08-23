IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewAadhaarDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewAadhaarDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetInterviewAadhaarDetails]    
@candidateEmail varchar(200)
AS    
Begin   
Select * From AadhaarDetails where email=@candidateEmail
End  
Go  
  