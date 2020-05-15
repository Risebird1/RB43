IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_DeleteInterviewerSkills'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteInterviewerSkills
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_DeleteInterviewerSkills]

@InterviewerSkillId int

AS
Begin

Delete from InterviewerSkillsChannel where id = @InterviewerSkillId
End
Go