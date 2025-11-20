    
    IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetChannelApprovedInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetChannelApprovedInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_GetChannelApprovedInterviewer]        
@Channelid int      
AS        
BEGIN      
 declare @PrimarySkill varchar(max)     
 set @PrimarySkill = (select PrimarySkill from ChannelManagement where id=@Channelid)      
 select * from (select d.*,tilis.id, ul.FirstName, ul.EmailId from  (select * from (SELECT Id as InterviewerProfileId, UserId,CurrentRole, PreferredDomain, ResumeLink, SkillName =       
    STUFF((SELECT ', ' + SkillName      
           FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id=tis.SkillId  where ipr.IsApproved=1) b       
           WHERE b.Id = a.Id      
           FOR XML PATH('')), 1, 2, '')      
 FROM (select ipr.*, sk.[Name] as SkillName     
 from InterviewerProfile ipr     
 join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id=tis.SkillId     
 where ipr.IsApproved=1 and ISNULL(ipr.IsDeleted,0)=0) a      
 GROUP BY Id, UserId,CurrentRole, PreferredDomain, ResumeLink) c       
 where CHARINDEX(@PrimarySkill,c.SkillName)>0 ) d      
 join UserList ul on d.UserId = ul.id      
 left join (select * from TransChannelInterviewerList where id=@Channelid) tilis       
 on tilis.InterviewerProfileId = d.InterviewerProfileId) e      
 where e.id is null    
      
        
End     
GO