IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCompletedInterviewsByClientIdDate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCompletedInterviewsByClientIdDate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetCompletedInterviewsByClientIdDate
@clientid int,
@StartDate datetime,      
@EndDate datetime,  
@year int,  
@month int  
AS
BEGIN
Select distinct im.IRFId, im.interviewDate,im.candidateEmail,im.ClientHrContact, im.interviewer,im.candidateName,IRF.JDTiltle,ic.VideoLink,ic.InterviewerComment,
ic.InterviewStatus,ic.MeetingStatus,ul.EmailId as risebirdHR,  
im.HRCoordinatorEmailId,cl.[Name] as Client
From InterviewMeetings im join IRF on IRF.IRFId=im.IRFId       
join InterviewComments ic on ic.InterviewId=im.id join Client cl on IRF.ClientId=cl.id  
left join UserList ul on ul.id=IRF.RisebirdHr  
where im.interviewDate>=@StartDate and im.interviewDate<@EndDate and IRF.Clientid=@clientid 
and (ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8)        
END
GO