/****** Object:  Table [dbo].[TransInterviewerSkillsSelectablity]    Script Date: 5/15/2020 9:51:02 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkillsSelectablity')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerSkillsSelectablity](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerSkillId] [int] NOT NULL,
	[LevelId] [int] NOT NULL,
	[Type] [varchar](200) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL,
 CONSTRAINT [PK_TransInterviewerSkillsSelectablity] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO




