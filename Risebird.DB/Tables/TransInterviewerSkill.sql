IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkill')
BEGIN 


CREATE TABLE [dbo].[TransInterviewerSkill](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerProfileId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[Level] [int] NULL,
	[SkillName] [varchar](150) NULL,
	CONSTRAINT [FK__IProfileID] FOREIGN KEY (InterviewerProfileId) REFERENCES InterviewerProfile(id),
	CONSTRAINT [FK__Level] FOREIGN KEY (Level) REFERENCES LevelTech(id),
	CONSTRAINT [FK__SkillID] FOREIGN KEY (SkillId) REFERENCES Skill(id),
 CONSTRAINT [PK_TransInterviewerSkill] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)

END

GO
