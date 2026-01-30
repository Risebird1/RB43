IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelSampleQuestions')
BEGIN  
CREATE TABLE [dbo].[SkillChannelSampleQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[Skill] [int] NULL,
	[Comment] [nvarchar](max) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [nvarchar](500) NULL,
	[AIQuestionVideoLink] [varchar](max) NULL,
	[ExpectedAnswer] [nvarchar](max) NULL,
	[IsFlaggedForSentimentAnalysis] [bit] NULL,
	[IsTypingSpeedAssessment] [bit] NULL,
 CONSTRAINT [PK_SkillChannelSampleQuestions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
else
Begin

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillName')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add SkillName varchar(500)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AIQuestionVideoLink')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add AIQuestionVideoLink varchar(max)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExpectedAnswer')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add ExpectedAnswer varchar(5000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsFlaggedForSentimentAnalysis')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IsFlaggedForSentimentAnalysis bit
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Comment')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ALTER COLUMN Comment NVARCHAR(max)
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ExpectedAnswer')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ALTER COLUMN ExpectedAnswer NVARCHAR(max);
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'SkillName')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    ALTER COLUMN SkillName NVARCHAR(500);
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsTypingSpeedAssessment')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IsTypingSpeedAssessment bit NULL DEFAULT 0
END

End
GO


