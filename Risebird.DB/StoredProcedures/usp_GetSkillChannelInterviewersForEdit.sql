IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelInterviewersForEdit'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetSkillChannelInterviewersForEdit]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetSkillChannelInterviewersForEdit]                 
@irfId int      
AS                  
BEGIN   

          
Select IRF.IRFID, IRF.JDTiltle, irf.ClientName, irf.CreatedOn, cl.[Name] as ClientCompName, irf.AssessmentGroupId,
irf.CompanyType, irf.JobCountry, irf.Heuristicsscore, irf.IsNewSkillChannel,
asg.AssessmentGroup as AssessmentGroupName
from IRF inner join Client cl on IRF.ClientId = cl.id 
join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
where IRF.id = @irfId  
  
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
  

  
END  

GO

    
      