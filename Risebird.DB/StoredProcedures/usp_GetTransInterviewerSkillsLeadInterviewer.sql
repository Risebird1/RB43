IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetTransInterviewerSkillsLeadInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransInterviewerSkillsLeadInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_GetTransInterviewerSkillsLeadInterviewer]
@InterviewerSkillId int
AS
Begin
select TransInterviewerSkillsLeadInterviewer.*, Interviewer.PrimarySkill from TransInterviewerSkillsLeadInterviewer 
inner join 
Interviewer on TransInterviewerSkillsLeadInterviewer.InterviewerId = Interviewer.id where InterviewerSkillId=@InterviewerSkillId
End




