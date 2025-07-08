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

END
GO


