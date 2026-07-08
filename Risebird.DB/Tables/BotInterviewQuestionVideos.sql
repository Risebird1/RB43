IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'BotInterviewQuestionVideos')
BEGIN 

CREATE TABLE [dbo].[BotInterviewQuestionVideos](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[VideoLink] [varchar](1000) NULL,
	[question] [nvarchar](max) NULL,
	[CreatedAt] [datetime] NULL,
	[BotSynthesizedVideoLink] [varchar](2000) NULL,
	[CandidateAnswer] [nvarchar](max) NULL,
	[BotAudioLink] [varchar](2000) NULL,
	[DeliveryHeadQuestionId] [int] NULL,
	[AnswerModifiedAt] [datetime] NULL,
	[AnswerModifiedBy] [varchar](100) NULL,
 CONSTRAINT [PK_BotInterviewQuestionVideos] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BotSynthesizedVideoLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add BotSynthesizedVideoLink varchar(2000)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswer')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add CandidateAnswer nvarchar(4000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BotAudioLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add BotAudioLink varchar(2000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DeliveryHeadQuestionId')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add DeliveryHeadQuestionId int null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnswerModifiedAt')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add AnswerModifiedAt datetime null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnswerModifiedBy')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add AnswerModifiedBy varchar(100) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswerAudioLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add CandidateAnswerAudioLink varchar(2000) null
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'question')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    ALTER COLUMN question NVARCHAR(max);
END
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswer')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    ALTER COLUMN CandidateAnswer NVARCHAR(max);
END

END
GO

-- =====================================================
-- Add unique index to prevent duplicate entries
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_BotInterviewQuestionVideos_SessionQuestion' 
    AND object_id = OBJECT_ID('BotInterviewQuestionVideos')
)
BEGIN
    -- Use filtered index to only apply uniqueness where DeliveryHeadQuestionId IS NOT NULL
    CREATE UNIQUE NONCLUSTERED INDEX UQ_BotInterviewQuestionVideos_SessionQuestion
    ON BotInterviewQuestionVideos (SessionId, DeliveryHeadQuestionId)
    WHERE DeliveryHeadQuestionId IS NOT NULL;
    PRINT '✓ Unique constraint added to BotInterviewQuestionVideos to prevent duplicates';
END
GO


