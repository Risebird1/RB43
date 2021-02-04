/****** Object:  StoredProcedure [dbo].[usp_GetTransQualityController]    Script Date: 12-11-2019 5.50.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransQualityController'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransQualityController
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetTransQualityController]

@irfId int

AS
Begin

--select TransQualityController.*, Interviewer.PrimarySkill, ul.EmailId from TransQualityController 
--inner join 
--Interviewer on TransQualityController.InterviewerId = Interviewer.id join UserList ul on ul.id= TransQualityController.UserId where IRFId=@irfId

select tqc.*, c.PrimarySkill, ul.EmailId from  (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a
	GROUP BY Id, UserId) c 
join  TransQualityController tqc on tqc.UserId=c.UserId
join UserList ul on ul.id=tqc.UserId

where IRFId=@irfId


End

GO


