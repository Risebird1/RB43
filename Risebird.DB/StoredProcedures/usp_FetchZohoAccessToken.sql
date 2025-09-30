IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchZohoAccessToken'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchZohoAccessToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchZohoAccessToken  
@TokenType varchar(20)  
AS  
BEGIN  
Select Top(1) AccessToken,TokenExpiry From ZohoAccessToken where TokenType=@TokenType order by TokenExpiry desc  
END
GO