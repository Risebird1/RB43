


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateWhatsappNumberByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateWhatsappNumberByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateWhatsappNumberByEmail]
    @id VARCHAR(255), @whatsappup VARCHAR(50)
AS
BEGIN
    UPDATE UserList SET WhatsappNumber = @whatsappup WHERE EmailId = @id
END
GO
