/****** Object:  StoredProcedure [dbo].[usp_AddSupportHR]    Script Date: 03-03-2020 6.39.07 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddSupportHR'))
BEGIN
    DROP PROCEDURE dbo.usp_AddSupportHR
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_AddSupportHR] 
 
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
INSERT INTO [dbo].[Support]    
VALUES          
(
@Name,
@EmailId,
@MobileNumber,
@WhatsappNumber,
@OnVoiceCall,
@OnWhatsapp,
@OnEmail,
@AvailabilityHours
)
   
END
GO


