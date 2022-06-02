IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQquestions')
BEGIN 

CREATE TABLE [dbo].[MCQquestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[QuestionType] [varchar](20) NULL,
	[Question] [varchar](max) NULL,
	[Option1] [varchar](1000) NULL,
	[Option2] [varchar](1000) NULL,
	[Option3] [varchar](1000) NULL,
	[Option4] [varchar](1000) NULL,
	[Answer] [varchar](10) NULL,
	[Explanation] [varchar](100) NULL,
	[MCQId] [varchar](50) NULL,
	[QuestionNumber] [int] NULL,
 CONSTRAINT [PK_MCQquestions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
