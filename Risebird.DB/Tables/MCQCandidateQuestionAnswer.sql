IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQCandidateQuestionAnswer')
BEGIN 
CREATE TABLE [dbo].[MCQCandidateQuestionAnswer](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[QuestionType] [varchar](20) NULL,
	[Question] [varchar](max) NULL,
	[Option1] [varchar](1000) NULL,
	[Option2] [varchar](1000) NULL,
	[Option3] [varchar](1000) NULL,
	[Option4] [varchar](1000) NULL,
	[Answer] [varchar](10) NULL,
	[Explanation] [varchar](500) NULL,
	[MCQId] [varchar](50) NULL,
	[QuestionNumber] [int] NULL,
	[CandidateID] [varchar](50) NULL,
	[MultiAnswerOption1] [bit] NULL,
	[MultiAnswerOption2] [bit] NULL,
	[MultiAnswerOption3] [bit] NULL,
	[MultiAnswerOption4] [bit] NULL,
	[QuestionId] [varchar](50) NULL,
	[Rating] [int] NULL,
	[QuestionAnswer] [varchar](10) NULL,
	[SingleAnswer] [varchar](10) NULL,
 CONSTRAINT [PK_MCQCandidateQuestionAnswer] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
