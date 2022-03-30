IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableIRFTransEmergencyInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableIRFTransEmergencyInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetClonableIRFTransEmergencyInterviewer]

@id int


AS
Begin


select tei.*, c.PrimarySkill, ul.EmailId from  (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a
	GROUP BY Id, UserId) c 
join  TransClonableIRFEmergencyInterviewer tei on tei.UserId=c.UserId
join UserList ul on ul.id = tei.UserId

where ClonableIRFId=@id

End


GO


