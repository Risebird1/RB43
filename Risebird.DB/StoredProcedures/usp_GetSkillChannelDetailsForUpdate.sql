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
cl.IsMeetingAutoCancelled as IsClientMeetingAutoCancelled, cl.InterviewDuration as ClientInterviewDuration  
from IRF inner join Client cl on IRF.ClientId = cl.id where IRF.id = @irfId  
  
  
--tech skills details  
Select tts.* from TransTechSkill tts  where IRFId = @irfId  --join Skill on tts.SkillId=Skill.id
  
--general skills details  
Select * from TransSkillGeneral where IRFId = @irfId and @isGeneralSkillRequired = 1  
  
--joinability details  
Select * from TransJoinability where IRFId = @irfId and @isJoinabilitySelectabilityRequired = 1  
  
--interviewer details  
Declare @Assessmentid int  
set @Assessmentid=(select AssessmentGroupid From IRF where id=@irfId)  
Declare @temptable Table(Userid int)  
Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid  
  
select distinct ti.*, agc.CampaignName, c.PrimarySkill, ul.EmailId, c.NickName, c.ResumeLink, c.CodedResumeLink,c.CompanyName, c.IsResumeRedactAttempted  
from  (SELECT  UserId, NickName, ResumeLink, CodedResumeLink,CompanyName, IsResumeRedactAttempted, PrimarySkill =   
STUFF((SELECT ', ' + SkillName  
FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b   
WHERE b.Id = a.Id  
FOR XML PATH('')), 1, 2, '')  
FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr left join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a  
GROUP BY Id, UserId, NickName, ResumeLink, CodedResumeLink,CompanyName, IsResumeRedactAttempted) c   
join  TransInterviewer ti on ti.UserId=c.UserId  
join UserList ul on ul.id = ti.UserId  
join @temptable tt on tt.userid=ti.Userid  
left join AGCampaigns agc on agc.id = ti.CampaignId  
where IRFId=@irfId and ul.IsActive=1  
  
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

    
      