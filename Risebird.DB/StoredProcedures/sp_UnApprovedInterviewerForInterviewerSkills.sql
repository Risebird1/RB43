/****** Object:  StoredProcedure [dbo].[sp_UnApprovedInterviewerForInterviewerSkills]    Script Date: 5/15/2020 10:12:37 AM ******/


IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_UnApprovedInterviewerForInterviewerSkills'))
BEGIN
    DROP PROCEDURE dbo.sp_UnApprovedInterviewerForInterviewerSkills
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[sp_UnApprovedInterviewerForInterviewerSkills]
--@InterviewerSkillId int
AS
BEGIN
--Select Ul.Id, Ul.FirstName,Ul.EmailId,ISs.InterviewerSkillId from  InterviewerProfile IPS inner join InterviewerSkills ISs on ISs.UserId=IPS.UserId inner join UserList Ul on IPS.UserId=Ul.id where IPS.IsApproved=0 and ISs.id=@InterviewerSkillId
Select Ul.Id, Ul.FirstName,Ul.EmailId,ISs.PrimarySkill from  InterviewerProfile IPS inner join UserList Ul on IPS.UserId=Ul.id  inner join InterviewerSkillsChannel ISs on ISs.UserId=IPS.UserId where IPS.IsApproved=0
END
GO


