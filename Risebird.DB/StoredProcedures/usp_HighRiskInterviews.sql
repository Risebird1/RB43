IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_HighRiskInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_HighRiskInterviews
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_HighRiskInterviews
@CurrentDate datetime  
AS  
BEGIN  
Select distinct ip.id,ul.EmailId as ChannelOwner,ul.uniqueid,im.IRFId as IRFId,im.interviewDate,im.interviewer,ip.RedMarker,im.candidateName
,im.candidateEmail,ip.ResumeLink as ResumeLink,RedmarkerAcknowledge
,ul.MobileNumber
,ip.CompanyName,
IRF.PrimarySkillInvite as PrimarySkill,
im.id as meetingId
From InterviewerProfile ip join UserList ul on ul.id=ip.Userid   
join InterviewMeetings im on im.interviewer=ul.EmailId 
Join IRF on IRF.IRFId=im.IRFId
where interviewDate>@CurrentDate and ip.RedMarker>0   
END
GO