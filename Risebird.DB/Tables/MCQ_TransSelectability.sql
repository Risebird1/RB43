IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQ_TransSelectability')
BEGIN

CREATE TABLE [dbo].[MCQ_TransSelectability](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[MCQId] [varchar](50) NULL,
	[CMCQId] [varchar](50) NULL,
	[CandidateEmail] [varchar](200) NULL,
	[ApplyingCompany] [varchar](50) NULL,
	[Type] [varchar](50) NULL,
	[Value] [varchar](200) NULL,
	[LevelId] [int] NULL,
	[RequiredLevelId] [int] NULL,
 CONSTRAINT [PK_MCQ_TransSelectability] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


