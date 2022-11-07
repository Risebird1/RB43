/****** Object:  StoredProcedure [dbo].[usp_GetEmailRecipientCurrentStatus]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientsLatestActivity'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientsLatestActivity
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetEmailRecipientsLatestActivity] 
@recipientType varchar(30),
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime
AS    
Begin   
  
  

	;WITH CTE AS
	(SELECT id,
		ROW_NUMBER() OVER (PARTITION BY email, Split_Message_Id ORDER BY [timestamp] desc) as rn FROM SendgridEventData
	)
	SELECT id into #latestActivityRowId FROM CTE WHERE rn=1

  
  select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],
  irf.IRFId as SkillChannelName, im.interviewDate
  from SentMailDetails smd
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id
  join UserList ul on ul.EmailId = sge.email
  left join IRF on smd.IRFId = irf.id 
  left join InterviewMeetings im on im.id = smd.InterviewId
  where sge.id in (select id from #latestActivityRowId) 
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY, -1, @timeNow) as date)) or
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))
  and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3) or (@recipientType = 'Client' and ul.idProfile=4))

  drop table #latestActivityRowId
 
End  
  
GO

