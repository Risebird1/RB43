-- Table to store listening comprehension assessment results
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ListeningComprehensionAssessment')
BEGIN  
CREATE TABLE [dbo].[ListeningComprehensionAssessment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[QuestionId] [int] NULL,
	[AudioUrl] [varchar](max) NULL,
	[AudioTranscription] [nvarchar](max) NULL,
	[CandidateSummary] [nvarchar](max) NULL,
	[TimeStarted] [datetime] NULL,
	[TimeCompleted] [datetime] NULL,
	[TimeTakenInSeconds] [decimal](10,2) NULL,
	[CreatedAt] [datetime] NULL,
	[CreatedBy] [varchar](150) NULL,
 CONSTRAINT [PK_ListeningComprehensionAssessment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
