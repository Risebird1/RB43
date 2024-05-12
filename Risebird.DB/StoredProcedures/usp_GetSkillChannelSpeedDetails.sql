IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSkillChannelSpeedDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelSpeedDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_GetSkillChannelSpeedDetails    
@irfId int,    
@type varchar(20)    
AS    
BEGIN    
--Report Pending  
if(@type='ReportPending')    
begin    
select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.id as irfIntId,im.id as imid from InterviewMeetings im join IRF on im.IRFId=irf.IRFId join Client cl on irf.ClientId=cl.id   
left join InterviewComments ic on ic.InterviewId= im.id   
where IRF.id=@irfId and  confirmationStatus=1 and (ic.MeetingStatus is null or ic.MeetingStatus=2) and interviewdate<DATEADD(Minute,150,Getdate())   
end  
--Proxy Interviews
else if(@type='ProxyInterview')    
begin
Select im.interviewDate,im.interviewer,im.candidateEmail,im.candidateName,im.MobileNumber,IRF.id as irfIntId,im.id as imid From InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId 
join IRF on IRF.IRFId=im.IRFId
where ic.InterviewStatus=1 and IRF.id=@irfId and confirmationStatus=1
end
END    
GO