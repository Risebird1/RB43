IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelDetailsForUpdate'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetSkillChannelDetailsForUpdate]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSkillChannelDetailsForUpdate]                   
@irfId int        
AS                    
BEGIN     
declare @isJoinabilitySelectabilityRequired bit, @isGeneralSkillRequired bit    
set @isJoinabilitySelectabilityRequired = (select IsJoinabilitySelectabilityRequired from IRF where id = @irfId)     
set @isGeneralSkillRequired = (select IsGeneralSkillRequired from IRF where id =@irfId)    
            
Select IRF.*, cl.[Name] as ClientCompName,    
IRF.IsMeetingNotificationSend  as EditIRFConfiguration    
, cl.IsCandidateavailability as IsClientCandidateAvailablily, cl.IsMeetingNotificationSend as IsClientMeetingNotificationSend,    
cl.IsMeetingAutoCancelled as IsClientMeetingAutoCancelled, cl.InterviewDuration as ClientInterviewDuration,  
asg.AssessmentGroup as AssessmentGroupName  
from IRF inner join Client cl on IRF.ClientId = cl.id   
join AssessmentGroup asg on asg.id = irf.AssessmentGroupId  
where IRF.id = @irfId    
    
    
--tech skills details    
Select tts.* from TransTechSkill tts  where IRFId = @irfId  --join Skill on tts.SkillId=Skill.id  
    
--general skills details    
Select * from TransSkillGeneral where IRFId = @irfId and @isGeneralSkillRequired = 1    
    
--joinability details    
Select * from TransJoinability where IRFId = @irfId and @isJoinabilitySelectabilityRequired = 1    
    
--interviewer details — all TransInterviewer rows on this channel (same scope as usp_GetSkillChannelInterviewersForEdit).  
--Do not filter by assessment-group membership; manually added panel members must appear on DM Approval.  
DECLARE @irfName varchar(50) = (SELECT IRFID FROM IRF WHERE id = @irfId)  
  
select distinct ti.*, agc.CampaignName, c.PrimarySkill, ul.EmailId, ul.MobileNumber, c.NickName, c.ResumeLink, c.CodedResumeLink,c.CompanyName, c.IsResumeRedactAttempted,  
ip.VideoLink, isnull(ip.IsVideoApproved, 0) as IsVideoApproved, ip.EmploymentComment,  
isnull(sti.TotalInterview, 0) as TotalInterview,  
isnull(fti.TotalFuture, 0) as TotalFuture,  
isnull(tcm.TotalCompletedMeetings, 0) as TotalCompletedMeetings,  
isnull(tsel.TotalSelected, 0) as TotalSelected,  
isnull(trej.TotalRejected, 0) as TotalRejected,  
ul.LastLogin as LastLogin,  
lic.LastInterviewConductedAt as LastInterviewConductedAt  
from  (SELECT  UserId, NickName, ResumeLink, CodedResumeLink,CompanyName, IsResumeRedactAttempted, PrimarySkill =     
STUFF((SELECT ', ' + SkillName    
FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b     
WHERE b.Id = a.Id    
FOR XML PATH('')), 1, 2, '')    
FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a    
GROUP BY Id, UserId, NickName, ResumeLink, CodedResumeLink,CompanyName, IsResumeRedactAttempted) c     
join  TransInterviewer ti on ti.UserId=c.UserId    
join UserList ul on ul.id = ti.UserId    
join InterviewerProfile ip on ip.UserId = c.UserId  
left join AGCampaigns agc on agc.id = ti.CampaignId  
outer apply (select count(im.id) as TotalInterview from InterviewMeetings im where im.IRFId = @irfName and im.interviewer = ul.EmailId) sti  
outer apply (select count(im.id) as TotalFuture from InterviewMeetings im  
    where im.IRFId = @irfName and im.interviewer = ul.EmailId and im.interviewer is not null  
      and im.interviewDate > DATEADD(MINUTE, 330, GETDATE())  
      and ISNULL(im.IsDeleted, 0) = 0 and ISNULL(im.isAutoCancelled, 0) = 0  
      and CAST(im.confirmationStatus as varchar(20)) not in ('2', 'Cancelled')) fti  
outer apply (  
    select max(im.interviewDate) as LastInterviewConductedAt  
    from InterviewMeetings im  
    inner join InterviewComments ic on im.id = ic.InterviewId  
    where im.interviewer = ul.EmailId  
      and im.confirmationStatus = 1  
      and ic.MeetingStatus in (4, 6, 7, 8)  
) lic  
outer apply (  
    select count(im.id) as TotalCompletedMeetings  
    from InterviewMeetings im  
    left join InterviewComments ic on ic.InterviewId = im.id  
    where im.IRFId = @irfName and im.interviewer = ul.EmailId  
      and ic.MeetingStatus in (4, 6, 7, 8)  
) tcm  
outer apply (  
    select count(im.id) as TotalSelected  
    from InterviewMeetings im  
    left join InterviewComments ic on ic.InterviewId = im.id  
    where im.IRFId = @irfName and im.interviewer = ul.EmailId  
      and (ic.InterviewStatus = 2 or ic.InterviewStatus = 4)  
      and ic.MeetingStatus in (4, 6, 8)  
) tsel  
outer apply (  
    select count(im.id) as TotalRejected  
    from InterviewMeetings im  
    left join InterviewComments ic on ic.InterviewId = im.id  
    where im.IRFId = @irfName and im.interviewer = ul.EmailId  
      and (ic.InterviewStatus = 3 or ic.InterviewStatus = 5 or ic.InterviewStatus = 1)  
      and ic.MeetingStatus in (4, 6, 8)  
) trej  
where ti.IRFId=@irfId and ul.IsActive=1 and ul.IsInterviewerAccess=1  and ISNULL(ip.IsDeleted,0)=0 
    
--selectability details    
Select * from TransSelectability where IRFId = @irfId and @isJoinabilitySelectabilityRequired = 1    
    
--HR details    
select thr.*, HR.UserId from TransHR thr inner join HR  on thr.HRId = HR.id where IRFId=@irfId    
    
--client user details    
select a.*, ClientRole.[Name] as [Role] from (select TransClientUser.*,     
UserList.EmailId,UserList.uniqueid,IRF.IRFId as IRFName,UserList.FirstName from TransClientUser    
inner join UserList on TransClientUser.ClientUserId = UserList.id     
join IRF on IRF.id=TransClientUser.IRFId) as a     
inner join ClientRole on ClientRole.id =a.ClientRoleId where IRFId =  @irfId    
    
END    
GO

    
      