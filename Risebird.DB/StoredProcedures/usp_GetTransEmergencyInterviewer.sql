/****** Object:  StoredProcedure [dbo].[usp_GetTransInterviewer]    Script Date: 12-11-2019 1.41.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransEmergencyInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransEmergencyInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetTransEmergencyInterviewer]

@irfId int


AS
Begin

--select TransEmergencyInterviewer.*, Interviewer.PrimarySkill from TransEmergencyInterviewer 
--inner join 
--Interviewer on TransEmergencyInterviewer.InterviewerId = Interviewer.id where IRFId=@irfId

Declare @Assessmentid int
Select @Assessmentid=AssessmentGroupid From IRF where id=@irfId
Declare @temptable Table(Userid int)
Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid


select tei.*, c.PrimarySkill, ul.EmailId from  (SELECT  UserId, PrimarySkill = 
    STUFF((SELECT ', ' + SkillName
           FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b 
           WHERE b.Id = a.Id
           FOR XML PATH('')), 1, 2, '')
	FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a
	GROUP BY Id, UserId) c 
join  TransEmergencyInterviewer tei on tei.UserId=c.UserId
join UserList ul on ul.id = tei.UserId
join @temptable tt on tt.userid=tei.Userid
where IRFId=@irfId and ul.IsActive=1

End


GO


