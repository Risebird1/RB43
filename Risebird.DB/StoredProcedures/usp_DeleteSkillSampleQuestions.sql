IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteSkillSampleQuestions'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteSkillSampleQuestions
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_DeleteSkillSampleQuestions
@questionid int
AS
Begin
delete From SkillChannelSampleQuestions where id=@questionid
End
GO