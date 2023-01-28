/****** Object:  StoredProcedure [dbo].[usp_GetTransInterviewer]    Script Date: 02-02-2021 6.10.03 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetTransInterviewer]
@irfId int
AS
Begin


Declare @Assessmentid int
Select @Assessmentid=AssessmentGroupid From IRF where id=@irfId
Declare @temptable Table(Userid int)
Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid

select ti.*, c.PrimarySkill, ul.EmailId, c.NickName, c.ResumeLink, c.CodedResumeLink 
from  (SELECT  UserId, NickName, ResumeLink, CodedResumeLink, PrimarySkill = 
STUFF((SELECT ', ' + SkillName
FROM (select ipr.*,sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) b 
WHERE b.Id = a.Id
FOR XML PATH('')), 1, 2, '')
FROM (select ipr.*, sk.[Name] as SkillName from InterviewerProfile ipr join TransInterviewerSkill tis on tis.InterviewerProfileId = ipr.id left join Skill sk on sk.id = tis.SkillId) a
GROUP BY Id, UserId, NickName, ResumeLink, CodedResumeLink) c 
join  TransInterviewer ti on ti.UserId=c.UserId
join UserList ul on ul.id = ti.UserId
join @temptable tt on tt.userid=ti.Userid
where IRFId=@irfId and ul.IsActive=1

End

GO


