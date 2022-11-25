IF EXISTS (SELECT * FROM sys.objects WHERE OBJECT_ID = OBJECT_ID(N'[fn_GetCandidateCommunicationCountForIRF]') AND type IN (N'IF')) 
BEGIN 
   DROP FUNCTION dbo.fn_GetCandidateCommunicationCountForIRF 
END 
GO
 
CREATE FUNCTION dbo.fn_GetCandidateCommunicationCountForIRF(@irfId int, @candidateEmail varchar(60))  
RETURNS TABLE 
AS

--declare @mailCount int, @whatsappCount int, @voiceCallsCount int 
RETURN 
   ( 
    select (select count(id) from SentMailDetails where IRFId = @irfId and CHARINDEX(@candidateEmail, [To]) > 0) as MailCount,
	(select count(id) from SentWhatsAppMessageDetails where IRFId = @irfId and Recipient = @candidateEmail) as WhatsappCount,
	(select count(id) from CallRecordingData where IRFId = @irfId and CallRecipient = @candidateEmail) as CallsCount
   ) 


GO