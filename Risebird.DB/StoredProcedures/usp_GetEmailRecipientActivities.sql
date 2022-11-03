/****** Object:  StoredProcedure [dbo].[usp_GetEmailRecipientActivities]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientActivities'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientActivities
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetEmailRecipientActivities]    
@recipientEmail varchar(100),  
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime
AS    
Begin   
  
  select sge.email, sge.[event] as eventType, sge.[timestamp], sge.CreatedAt, sge.[url],
  smd.InterviewId, smd.IRFId, smd.MailBody, smd.MailSubject, smd.SentBy, irf.IRFId as SkillChannelName,
  im.interviewDate, sge.Split_Message_Id, smd.SentBy,
  DENSE_RANK() OVER (ORDER BY smd.MailSubject) partitionNumber
  from SendgridEventData sge 
  join SentMailDetails smd on smd.sg_message_id = sge.Split_Message_Id
  left join IRF on IRF.id = smd.IRFId
  left join InterviewMeetings im on im.id = smd.InterviewId 
  where sge.email = @recipientEmail
  and ((@startDate is null and @endDate is null and cast(sge.CreatedAt as date) >= cast(DATEADD(DAY, -30, @timeNow) as date)) or
  (@startDate is not null and cast(sge.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(sge.CreatedAt as date) <= cast(@endDate as date)))
  order by sge.CreatedAt desc
 
End  
  
GO


