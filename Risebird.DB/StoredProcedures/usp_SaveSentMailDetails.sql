IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSentMailDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSentMailDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSentMailDetails]    
@To varchar(max),     
@Cc varchar(max),     
@Bcc varchar(max),  
@MailBody varchar(max),  
@MailSubject varchar(500),
@sg_message_id varchar(50),
@IRFId int = null,
@InterviewId int = null,
@CreatedAt datetime,
@SentBy varchar(100)
AS    
Begin  

Insert into SentMailDetails([To], Cc, Bcc, MailBody, MailSubject, sg_message_id, IRFId, InterviewId, CreatedAt, SentBy)  
values(@To, @Cc, @Bcc, @MailBody, @MailSubject, @sg_message_id, @IRFId, @InterviewId, @CreatedAt, @SentBy)  

End
GO

