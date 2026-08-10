

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetWhatsappNumberByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_GetWhatsappNumberByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetWhatsappNumberByEmail]
    @id VARCHAR(255)
AS
BEGIN
    SELECT WhatsappNumber FROM UserList WHERE EmailId = @id
END
GO
