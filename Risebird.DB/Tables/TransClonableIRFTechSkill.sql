IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFTechSkill')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFTechSkill](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
	[Concepts] [varchar](50) NULL,
	[Natureofquestion] [varchar](50) NULL,
	[Timelimit] [int] NULL,
	CONSTRAINT [FK_TransClonableIRFTechSkill_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_TransClonableIRFTechSkill_LevelId] FOREIGN KEY([LevelId]) REFERENCES [dbo].[LevelTech] ([id]),
	CONSTRAINT [FK_TransClonableIRFTechSkill_SkillId] FOREIGN KEY([SkillId]) REFERENCES [dbo].[Skill] ([id]),
 CONSTRAINT [PK_TransClonableIRFTechSkill] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

GO



