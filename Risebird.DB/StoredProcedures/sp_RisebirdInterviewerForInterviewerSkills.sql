/****** Object:  StoredProcedure [dbo].[sp_RisebirdInterviewerForInterviewerSkills]    Script Date: 5/15/2020 10:05:03 AM ******/


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_RisebirdInterviewerForInterviewerSkills'))
BEGIN
    DROP PROCEDURE dbo.sp_RisebirdInterviewerForInterviewerSkills
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[sp_RisebirdInterviewerForInterviewerSkills] 
  @InterviewerSkillId int
AS
BEGIN
select inter.id,TISI.InterviewerSkillId,TISI.UserId, ul.FirstName,ul.EmailId,
ul.idProfile, inter.PrimarySkill,inter.ResumeLink,
TISI.Id,TISI.InterviewerId
from [Interviewer] inter
inner join [dbo].[UserList] ul on inter.UserId = ul.id
inner join [dbo].[TransInterviewerSkillsLeadInterviewer] TISI on inter.id=TISI.InterviewerId
inner join [dbo].[TransInterviewerListInterviewerSkills] TILIS on TILIS.UserId=TISI.UserId
where TISI.InterviewerSkillId=@InterviewerSkillId

END
GO



