IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFSkillGeneral')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFSkillGeneral](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[Type] [varchar](50) NULL,
	[Value] [varchar](max) NULL,
	[LevelId] [int] NULL,
	[Weightage] [int] NULL,
	CONSTRAINT [FK_TransClonableIRFSkillGeneral_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_TransClonableIRFSkillGeneral_LevelId] FOREIGN KEY([LevelId]) REFERENCES [dbo].[Level] ([id]),
 CONSTRAINT [PK_TransClonableIRFSkillGeneral] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END

GO
