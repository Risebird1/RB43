IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllInstaRequests'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllInstaRequests
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
        
          
CREATE PROCEDURE [dbo].[usp_GetAllInstaRequests]           
@EmailId varchar(200),             
@Profile varchar(100)        
AS            
Begin           
          
IF (@Profile = 'Client')            
 BEGIN            
 Select ir.*,ag.AssessmentGroup From InstaRequests ir join AssessmentGroup ag on ag.id=ir.AssessmentGroup where RequestedBy = @EmailId          
      
 End            
Else          
begin          
Select ir.*,ag.AssessmentGroup From InstaRequests ir join AssessmentGroup ag on ag.id=ir.AssessmentGroup
      
End          
End  
GO