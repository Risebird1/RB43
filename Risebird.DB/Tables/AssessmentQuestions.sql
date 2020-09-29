IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AssessmentQuestions')
BEGIN 


CREATE TABLE [dbo].[AssessmentQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Assessmentid] [int] NULL,
	[QuestionCovered] [varchar](max) NULL,
	[Skill] [varchar](200) NULL,
	[QuestionLevel] [int] NULL,
	[Time] [varchar](15) NULL,
 CONSTRAINT [PK_AssessmentQuestions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


