IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(' dbo.usp_FetchAllCodeInviteDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAllCodeInviteDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchAllCodeInviteDetails    
AS      
BEGIN      
Select * from  CodeInviteDetails   
END    
  GO  