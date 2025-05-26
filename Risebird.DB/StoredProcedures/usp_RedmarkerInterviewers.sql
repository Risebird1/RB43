IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RedmarkerInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_RedmarkerInterviewers
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_RedmarkerInterviewers      
AS      
BEGIN      
Select ul.EmailId as interviewer,ul.MobileNumber,ip.NickName,ip.ResumeLink,ip.CompanyName,trm.RedMarker,trm.IRFId as id,IRF.IRFId,ul.id as UserId,IRF.JDTiltle,cl.Name as Client,      
ulc.EmailId as ChannelOwner,ulb.EmailId as Clienthr,tc.TotalCompletedMeetings,tr.AIVerdict,sti.TotalInterview,Fti.TotalFuture,Tai.TotalInterviewers,ip.IsEmployed,ip.AcknowledgementStatus,ip.VideoConfirmation,tir.RedMarkerStatus,tr.IsActive,Rcd.RedMarkerReviewComment      
From TransInterviewerRedMarker trm join InterviewerProfile ip on trm.Interviewerid=ip.Id       
join IRF on IRF.id=trm.IRFId      
join UserList ul on ul.id=ip.UserId      
left join Client cl on cl.id=IRF.ClientId      
left join UserList ulc on ulc.id=IRF.RisebirdHr      
left join UserList ulb on ulb.id=IRF.BillingOwner       
left join TransInterviewer tr on tr.IRFId=trm.IRFId and tr.UserId=ul.id      
left join TransInterviewerRedMarker tir on tir.IRFId=trm.IRFId and tir.InterviewerId=ip.Id      
outer Apply (Select Count(im.id) as TotalCompletedMeetings from InterviewMeetings im join InterviewComments ic on ic.Interviewid=im.id where im.IRFId=IRF.IRFId and im.Interviewer=ul.EmailId and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)) tc      
outer Apply (Select Count(id) as TotalFuture from InterviewMeetings where interviewer=ul.EmailId and interviewDate>DATEADD(MINUTE,330,Getdate())) Fti      
outer Apply (Select Count(id) as TotalInterview from InterviewMeetings where IRFId=IRF.IRFId and interviewDate>DATEADD(MINUTE,330,Getdate())) sti      
outer Apply (Select Count(ti.Id) as TotalInterviewers from TransInterviewer ti join UserList ul on ul.id=ti.UserId where ti.IsActive=1 and ul.IsActive=1 and ti.IRFId=IRF.id) Tai      
outer Apply (Select Top 1 Comment as RedMarkerReviewComment from RedMarkerStatusDetails where InterviewerUserid=ul.id and IRFId=IRF.id and Type='IRF' Order by CreatedOn desc) Rcd  
where trm.RedMarker>0  and Isnull(IRF.IsPaused,0)=0 and tc.TotalCompletedMeetings>=10    
END      
GO