-- Table to store typing speed assessment results
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TypingSpeedAssessment')
BEGIN  
CREATE TABLE [dbo].[TypingSpeedAssessment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[QuestionId] [int] NULL,
	[SampleText] [nvarchar](max) NULL,
	[TypedText] [nvarchar](max) NULL,
	[TimeStarted] [datetime] NULL,
	[TimeCompleted] [datetime] NULL,
	[TimeTakenInSeconds] [decimal](10,2) NULL,
	[TotalCharacters] [int] NULL,
	[CorrectCharacters] [int] NULL,
	[AccuracyPercentage] [decimal](5,2) NULL,
	[WordsPerMinute] [decimal](10,2) NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_TypingSpeedAssessment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
