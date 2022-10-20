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
@CreatedAt datetime  
AS    
Begin  

Insert into SendgridEventData(email, [timestamp], [event], CreatedAt)  
values(@email, @timestamp, @event, @CreatedAt)  

End
GO