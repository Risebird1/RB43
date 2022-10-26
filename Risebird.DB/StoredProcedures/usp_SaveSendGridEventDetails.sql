IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveSendGridEventDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveSendGridEventDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_SaveSendGridEventDetails]    
@email varchar(50),     
@timestamp varchar(50),  
@event varchar(25),  
@CreatedAt datetime,
@smtpid varchar(50),
@ip varchar(50),
@category varchar(50),
@sg_event_id varchar(50),
@sg_message_id varchar(50),
@response varchar(40),
@url varchar(500),
@useragent varchar(200),
@reason varchar(100),
@status varchar(50),
@Split_Message_Id varchar(50)
AS    
Begin  

Insert into SendgridEventData(email, [timestamp], [event], CreatedAt, [smtp-id], [ip], category, sg_event_id, sg_message_id, response, [url], useragent, reason, [status], Split_Message_Id)  
values(@email, @timestamp, @event, @CreatedAt, @smtpid, @ip, @category, @sg_event_id, @sg_message_id, @response, @url, @useragent, @reason, @status, @Split_Message_Id )  

End
GO

