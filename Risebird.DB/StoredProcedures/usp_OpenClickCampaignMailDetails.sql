IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_OpenClickCampaignMailDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_OpenClickCampaignMailDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_OpenClickCampaignMailDetails
 @campaignId int,
 @timeNow datetime
 as
Begin
	select  distinct  smd.MailSubject, smd.MailBody, smd.CreatedAt , sge.email, sge.[event] as eventType, sge.[timestamp], sge.[url],
	smd.InterviewId, smd.SentBy,ipr.CompanyName, ipr.ResumeLink, ipr.VideoConfirmation, ipr.RedMarker,
	--Case 
	--when ti.IsAvailable=1 then 'Yes' 
	--Else 'No'
	----when ti.IsAvailable = 0 then 'No' 
	----when ti.IsAvailable is null then 'No'
	--END as IsAvailable,  
    ul.MobileNumber, ul.EmailId, ul.FirstName + ' ' + ul.LastName as RecipientName,--im.interviewDate,
    0 as TotalInterviews
	from SendgridEventData sge 
	join SentMailDetails smd on smd.sg_message_id = sge.Split_Message_Id
	join UserList ul on ul.EmailId = sge.email
	join InterviewerProfile ipr on ipr.UserId  = ul.id
	--left join IRF on IRF.id = smd.IRFId
	--left join InterviewMeetings im on im.interviewer = ul.EmailId
	--join AGCampaigns agc on agc.id = smd.CampaignId
	--Left Join TransInterviewer ti on (ti.CampaignId=smd.CampaignId and ti.UserId = ul.id)
	where (sge.[event] = 'open' or sge.[event]='click') 
	and ul.idProfile = 3
	and smd.CampaignId =@campaignId
	--and cast(sge.CreatedAt as date) >= cast(DATEADD(DAY, -1, @timeNow) as date)
	order by smd.CreatedAt desc
	End
GO