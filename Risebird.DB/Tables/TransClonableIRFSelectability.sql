IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFSelectability')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFSelectability](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[LevelId] [int] NOT NULL,
	[Type] [varchar](200) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL,
	CONSTRAINT [FK_TransClonableIRFSelectability_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_TransClonableIRFSelectability_LevelId] FOREIGN KEY([LevelId]) REFERENCES [dbo].[Level] ([id]),
 CONSTRAINT [PK_TransClonableIRFSelectability] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END

GO





