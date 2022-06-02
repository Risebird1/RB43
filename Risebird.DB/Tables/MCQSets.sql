
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQSets')
BEGIN 

CREATE TABLE [dbo].[MCQSets](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[Title] [varchar](50) NULL,
	[CompentencyLevel] [int] NULL,
	[PrimarySkills] [varchar](100) NULL,
	[ProfileType] [int] NULL,
	[SecondarySkill] [varchar](100) NULL,
	[Timer] [varchar](5) NULL,
	[QuestionCount] [int] NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_MCQSets] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
