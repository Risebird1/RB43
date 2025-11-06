IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SkillChannelSampleQuestions')
BEGIN  
CREATE TABLE [dbo].[SkillChannelSampleQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NULL,
	[Skill] [int] NULL,
	[Comment] [varchar](500) NULL,
	[CreatedBy] [varchar](150) NULL,
	[SkillName] [varchar](500) NULL,
	[AIQuestionVideoLink] [varchar](max) NULL,
	[ExpectedAnswer] [varchar](5000) NULL,
	[IsFlaggedForSentimentAnalysis] [bit] NULL,
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

End
GO


