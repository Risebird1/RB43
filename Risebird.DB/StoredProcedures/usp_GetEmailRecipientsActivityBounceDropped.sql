IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetEmailRecipientsActivityBounceDropped'))
BEGIN
    DROP PROCEDURE dbo.usp_GetEmailRecipientsActivityBounceDropped
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetEmailRecipientsActivityBounceDropped]     
@recipientType varchar(30),
@startDate datetime = null,
@endDate datetime = null,
@timeNow datetime
AS    
Begin   
;WITH CTE AS
	(SELECT sge.id,
		ROW_NUMBER() OVER (PARTITION BY email, Split_Message_Id ORDER BY [timestamp] desc) as rn FROM SendgridEventData sge
		join UserList ul on ul.EmailId = sge.email
		where ((@startDate is null and @endDate is null and cast(CreatedAt as date) >= cast(DATEADD(DAY, -1, @timeNow) as date)) or
		(@startDate is not null and cast(CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(CreatedAt as date) <= cast(@endDate as date)))
		and ([event] = 'bounce' or [event] = 'dropped')
		and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))

	)
	SELECT id into #latestActivityRowId FROM CTE WHERE rn=1

  
  select smd.MailBody, smd.MailSubject, smd.InterviewId, smd.IRFId, smd.CreatedAt as SentOn, smd.SentBy, smd.sg_message_id,
  sge.email, sge.[event] as latestEvent, sge.CreatedAt as ActivityDate, sge.[timestamp], sge.[url],
  irf.IRFId as SkillChannelName, im.interviewDate,uli.EmailId as ChannelOwner, ca.ResumeLink as ResumeLink,
  ca.Phone as MobileNumber,irf.CalendlyLink,ulc.EmailId as candidateEmail,
  case when ulc.LastName is null then ulc.FirstName                  
else RTRIM(LTRIM(ulc.FirstName)) + SPACE(1) + RTRIM(LTRIM(ulc.LastName))                  
end as candidateName, ca.ClientHRAttemptContact as CandidateClientHR,IRF.EndDate
  from SentMailDetails smd
  join SendgridEventData sge on sge.Split_Message_Id = smd.sg_message_id
  join UserList ul on ul.EmailId = sge.email
  left join IRF on smd.IRFId = irf.id 
  left join InterviewMeetings im on im.id = smd.InterviewId
  left join UserList ulc on im.candidateEmail= ulc.EmailId
  left join UserList uli on uli.id=  irf.RisebirdHr
  join CandidatesMaster cm on ulc.id = cm.UserId                                                       
  join Candidate ca on cm.id = ca.CandidateMasterId and irf.id= ca.IRFId
  where sge.id in (select id from #latestActivityRowId) 
  and sge.email != 'hire@risebird.io'
  and ((@startDate is null and @endDate is null and cast(smd.CreatedAt as date) >= cast(DATEADD(DAY,-1, @timeNow) as date)) or
  (@startDate is not null and cast(smd.CreatedAt as date) >= cast(@startDate as date) and @endDate is not null and cast(smd.CreatedAt as date) <= cast(@endDate as date)))
  and ((@recipientType = 'Candidate' and ul.idProfile=23) or (@recipientType = 'Interviewer' and ul.idProfile=3))
  order by smd.CreatedAt desc
  drop table #latestActivityRowId
 
End 
Go