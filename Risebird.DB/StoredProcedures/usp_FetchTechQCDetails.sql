IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchTechQCDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchTechQCDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchTechQCDetails
@EmailId varchar(200)
AS
BEGIN
Declare @Role varchar(150)              
Select @Role=pr.Name From UserList ul join Profile pr on pr.id=ul.idprofile where Emailid=@Emailid 
if(@Role='SeniorHR')
Begin
Select tqd.Interviewid,im.IRFId,im.ConfirmationStatus,im.ClientHrContact,im.candidateEmail,ic.MeetingStatus,ic.InterviewStatus,im.Interviewer,cl.Name as Client,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,im.interviewdate,ul.Emailid as ChannelOwner,tqd.IsCompleted as IsQC,IRF.id as irfIntId
From TechQualityDetails tqd join InterviewMeetings im on tqd.Interviewid=im.id
join InterviewComments ic on ic.Interviewid=im.id
join IRF on IRF.IRFId=im.IRFId
join Client cl on cl.id=IRF.Clientid
left join UserList ul on ul.id=IRF.Risebirdhr
where tqd.IsActive=1 and ul.EmailId=@EmailId
END
ELSE
BEGIN
Select tqd.Interviewid,im.IRFId,im.ConfirmationStatus,im.ClientHrContact,im.candidateEmail,ic.MeetingStatus,ic.InterviewStatus,im.Interviewer,cl.Name as Client,Case when irf.SkillSetsevaluate>1  and ISNULL(irf.JDTitleTwo,'')!='' then irf.JDTiltle+' and '+irf.JDTitleTwo else irf.JDTiltle end as JDTiltle,im.interviewdate,ul.Emailid as ChannelOwner,tqd.IsCompleted as IsQC,IRF.id as irfIntId
From TechQualityDetails tqd join InterviewMeetings im on tqd.Interviewid=im.id
join InterviewComments ic on ic.Interviewid=im.id
join IRF on IRF.IRFId=im.IRFId
join Client cl on cl.id=IRF.Clientid
left join UserList ul on ul.id=IRF.Risebirdhr
where tqd.IsActive=1
END
END
Go