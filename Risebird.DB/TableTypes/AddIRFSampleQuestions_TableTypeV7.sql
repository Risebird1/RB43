-- V7 adds IsMcqAssessmentSection.
--
-- A new version rather than an ALTER because a table type in use by a stored procedure cannot be
-- altered, which is why V2..V6 exist. usp_AddIRFSampleQuestions is repointed at this one; the
-- older types stay in place so a rollback of the procedure still has its type to bind to.
IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'AddIRFSampleQuestions_TableTypeV7')
BEGIN
CREATE TYPE [dbo].[AddIRFSampleQuestions_TableTypeV7] AS TABLE(
	[IRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Comment] [nvarchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [nvarchar](500) NULL,
	[ExpectedAnswer] [nvarchar](max) NULL,
	[IsCodingQuestion] [bit] NULL,
	[CodingTimeLimitInMinutes] [int] NULL,
	[QuestionGist] [nvarchar](max) NULL,
	-- Marks the row as the MCQ assessment section rather than a question the avatar asks. The
	-- questions themselves live on screen.risebird.io; this row only fixes where in the
	-- interview the section falls, and its Comment is the spoken introduction.
	[IsMcqAssessmentSection] [bit] NULL
)
END
GO
