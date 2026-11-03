IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchWhatsAppNumberByEmail'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchWhatsAppNumberByEmail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchWhatsAppNumberByEmail
@Emailid varchar(150)
AS
BEGIN
-- A blank WhatsappNumber ('') falls back to MobileNumber, like NULL does.
Select Isnull(NullIf(LTrim(RTrim(WhatsappNumber)),''),MobileNumber) as WhatsappNumber,FirstName,LastName from UserList Where Emailid=@Emailid
END
GO