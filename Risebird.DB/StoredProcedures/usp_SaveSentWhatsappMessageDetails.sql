IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSentWhatsappMessageDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentWhatsappMessageDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSentWhatsappMessageDetails]    
@Recipient varchar(60),     
@CountryCode varchar(5) = null,     
@WhatsappNumber varchar(15) = null,  
@Message varchar(max),  
@ButtonLink varchar(500) = null,
@Event varchar(500) = null,
@IRFId int = null,
@InterviewId int = null,
@InterviewDate datetime = null,
@Header varchar(500) = null,
@CreatedAt datetime,
@SentBy varchar(100) =null
AS    
Begin  

Insert into SentWhatsAppMessageDetails(Recipient, CountryCode, WhatsappNumber,	[Message], ButtonLink, [Event],	IRFId, InterviewId,	InterviewDate, Header, CreatedAt, SentBy)  
values(@Recipient, @CountryCode, @WhatsappNumber, @Message, @ButtonLink, @Event, @IRFId, @InterviewId, @InterviewDate, @Header, @CreatedAt, @SentBy)  

End
GO

