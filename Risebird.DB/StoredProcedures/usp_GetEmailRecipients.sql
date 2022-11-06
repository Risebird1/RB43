/****** Object:  StoredProcedure [dbo].[usp_GetEmailRecipients]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipients'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipients
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetEmailRecipients]    
@recipientType varchar(100),  
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime
AS    
Begin   
  
  select distinct sge.email from SentMailDetails smd
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id
  join UserList ul on ul.EmailId = sge.email
  where ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY, -30, @timeNow) as date)) or
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))
  and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3) or (@recipientType = 'Client' and ul.idProfile=4))


  
 
End  
  
GO


