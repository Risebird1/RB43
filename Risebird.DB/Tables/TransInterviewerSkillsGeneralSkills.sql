/****** Object:  Table [dbo].[TransInterviewerSkillsGeneralSkills]    Script Date: 5/15/2020 9:42:40 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkillsGeneralSkills')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerSkillsGeneralSkills](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerSkillId] [int] NOT NULL,
	[Type] [varchar](50) NULL,
	[Value] [varchar](max) NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
 CONSTRAINT [PK_TransInterviewerSkillsGeneralSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 

END
GO




