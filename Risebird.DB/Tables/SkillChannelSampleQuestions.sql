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

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsListeningComprehension')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IsListeningComprehension bit NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ListeningAudioUrl')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add ListeningAudioUrl varchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ListeningAudioTranscription')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add ListeningAudioTranscription nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsCodingQuestion')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IsCodingQuestion bit NULL DEFAULT 0
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CodingTimeLimitInMinutes')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add CodingTimeLimitInMinutes int NULL
END

-- Plain-language summary of a coding question. Code cannot be read aloud, so this is
-- what gets narrated by the Synthesia avatar instead of the Comment for coding questions.
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'QuestionGist')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add QuestionGist nvarchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IdealAnswer')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IdealAnswer varchar(max) NULL
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'IsAIGenerated')
      AND Object_ID = Object_ID(N'SkillChannelSampleQuestions'))
BEGIN
    ALTER TABLE SkillChannelSampleQuestions
    Add IsAIGenerated bit NULL
END

End
GO


