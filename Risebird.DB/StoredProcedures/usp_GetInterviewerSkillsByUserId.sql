IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerSkillsByUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerSkillsByUserId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_GetInterviewerSkillsByUserId]     
@userId int    
AS      
BEGIN      
    
    
 select c.PrimarySkill from (SELECT  id, UserId, IsApproved, PrimarySkill =     
    STUFF((SELECT ', ' + SkillName    
           FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b     
           WHERE b.Id = a.Id    
           FOR XML PATH('')), 1, 2, '')    
 FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a    
 GROUP BY Id, UserId, IsApproved) c join UserList ul on ul.id = c.UserId    
 where UserId=@userId and IsApproved=1 and IsActive=1    
End 

Go