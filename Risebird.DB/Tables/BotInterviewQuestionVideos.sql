IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'BotInterviewQuestionVideos')
BEGIN 

CREATE TABLE [dbo].[BotInterviewQuestionVideos](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[VideoLink] [varchar](1000) NULL,
	[question] [varchar](1000) NULL,
	[CreatedAt] [datetime] NULL,
	[BotSynthesizedVideoLink] [varchar](2000) NULL,
	[CandidateAnswer] [nvarchar](4000) NULL,
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

END
GO


