IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransTemplateTechSkill')

BEGIN

CREATE TABLE [dbo].[TransTemplateTechSkill](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[TemplateId] [int] NOT NULL,
	[SkillId] [int] NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
        [IsSelected] [bit] NOT NULL default 1,
	CONSTRAINT [FK_TransTemplateTechSkill_TemplateId] FOREIGN KEY (TemplateId) REFERENCES SkillChannelTemplate(id),
	CONSTRAINT [FK_TransTemplateTechSkill_SkillId] FOREIGN KEY (SkillId) REFERENCES Skill(id),
	CONSTRAINT [FK_TransTemplateTechSkill_LevelId] FOREIGN KEY (LevelId) REFERENCES LevelTech(id),
 CONSTRAINT [PK_TransTemplateTechSkill] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

GO



