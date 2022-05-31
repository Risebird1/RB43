IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQCandidates')
BEGIN 
CREATE TABLE [dbo].[MCQCandidates](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[MCQId] [varchar](50) NULL,
	[Skills] [varchar](100) NULL,
	[TotalExprience] [int] NULL,
	[CandidateEmail] [varchar](200) NULL,
	[ApplyingCompany] [varchar](50) NULL,
	[ClientHR] [varchar](200) NULL,
	[CandidateScore] [int] NULL,
	[TotalQuestions] [int] NULL,
	[VideoLink] [varchar](1000) NULL,
	[FeedbackComment] [varchar](500) NULL,
	[Feedbackreating] [int] NULL,
	[AttemptedDate] [datetime] NULL,
 CONSTRAINT [PK_MCQCandidates] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
