IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RedmarkerInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_RedmarkerInterviewers
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_RedmarkerInterviewers
AS
BEGIN
Select ul.EmailId as interviewer,ul.MobileNumber,ip.NickName,ip.ResumeLink,ip.CompanyName,trm.RedMarker,trm.IRFId as id,IRF.IRFId,ul.id as UserId,IRF.JDTiltle,cl.Name as Client,
ulc.EmailId as ChannelOwner,ulb.EmailId as Clienthr,tc.TotalCompletedMeetings,tr.AIVerdict
From TransInterviewerRedMarker trm join InterviewerProfile ip on trm.Interviewerid=ip.Id 
join IRF on IRF.id=trm.IRFId
join UserList ul on ul.id=ip.UserId
left join Client cl on cl.id=IRF.ClientId
left join UserList ulc on ulc.id=IRF.RisebirdHr
left join UserList ulb on ulb.id=IRF.BillingOwner 
left join TransInterviewer tr on tr.IRFId=trm.IRFId and tr.UserId=ul.id
outer Apply (Select Count(im.id) as TotalCompletedMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where im.IRFId=IRF.IRFId and im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)) tc
where trm.RedMarker>0
END
GO