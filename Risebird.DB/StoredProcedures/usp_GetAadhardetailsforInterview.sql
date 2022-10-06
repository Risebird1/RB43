
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAadhardetailsforInterview'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAadhardetailsforInterview
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetAadhardetailsforInterview]                      
        
AS           
          
        
Begin                            
          
  select * from  AadhaarDetails 
                  
End            
  
           
GO

