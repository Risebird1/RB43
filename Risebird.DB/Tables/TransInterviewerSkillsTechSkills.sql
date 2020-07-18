/****** Object:  Table [dbo].[TransInterviewerSkillsTechSkills]    Script Date: 5/15/2020 9:53:39 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkillsTechSkills')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerSkillsTechSkills](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerSkillId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
 CONSTRAINT [PK_TransInterviewerSkillsTechSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO




