/****** Object:  StoredProcedure [dbo].[usp_GetInterviewerMailActivityForAvailabilityByIRFId]    ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetInterviewerMailActivityForAvailabilityByIRFId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerMailActivityForAvailabilityByIRFId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetInterviewerMailActivityForAvailabilityByIRFId]    

@irfId int

AS    
Begin

	declare @irfName varchar(100) = (select irfid from irf where id = @irfId)

  
  select distinct smd.MailSubject, smd.MailBody, smd.CreatedAt , sge.email, sge.[event] as eventType, sge.[timestamp], sge.[url],
  smd.InterviewId, smd.SentBy, irf.IRFId as SkillChannelName, @irfId as IRFId, ipr.CompanyName, ipr.ResumeLink, ipr.VideoConfirmation,
  smd.SentBy, ul.MobileNumber, ul.FirstName + ' ' + ul.LastName as RecipientName--im.interviewDate, 
  from SendgridEventData sge 
  join SentMailDetails smd on smd.sg_message_id = sge.Split_Message_Id
  join UserList ul on ul.EmailId = sge.email
  join InterviewerProfile ipr on ipr.UserId  = ul.id
  left join IRF on IRF.id = smd.IRFId
  --left join InterviewMeetings im on im.id = smd.InterviewId 
  where  sge.[event] = 'open' and ul.idProfile = 3--profile id for Interviewer and irf.id =@irfId and
  and charindex('Risebird: Availability check for Skill Channel ' + @irfName, smd.MailSubject) > 0 --and smd.MailSubject like '%Risebird: Availability check for Skill Channel%' 
  order by smd.CreatedAt desc
 
End  
  
GO


