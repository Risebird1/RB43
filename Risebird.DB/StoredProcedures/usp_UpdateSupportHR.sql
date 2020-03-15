/****** Object:  StoredProcedure [dbo].[usp_UpdateSupportHR]    Script Date: 04-03-2020 3.35.33 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateSupportHR'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSupportHR
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateSupportHR] 
@id int, 
@Name nvarchar(200),    
@EmailId nvarchar(200),    
@MobileNumber nvarchar(200), 
@WhatsappNumber nvarchar(200), 
@AvailabilityHours nvarchar(200), 
@OnVoiceCall bit, 
@OnWhatsapp bit, 
@OnEmail bit 
AS
BEGIN
update [dbo].[Support] 
set
[Name]=@Name,
[EmailId]=@EmailId,
[MobileNumber]=@MobileNumber,
[WhatsappNumber]=@WhatsappNumber,
[AvailabilityHours]=@AvailabilityHours,
[OnVoiceCall]=@OnVoiceCall,
[OnWhatsapp]=@OnWhatsapp,
[OnEmail]=@OnEmail
where id =@id
 
END
GO


