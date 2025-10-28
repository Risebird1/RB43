IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveZohoAccessToken'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveZohoAccessToken
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_SaveZohoAccessToken
@AccessToken varchar(100),
@TokenExpiry Datetime,
@TokenType varchar(20)
AS
BEGIN
Delete from ZohoAccessToken where TokenType=@TokenType
Insert into ZohoAccessToken(AccessToken,TokenExpiry,TokenType) values(@AccessToken,@TokenExpiry,@TokenType)
END
GO