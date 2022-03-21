IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFJoinability')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFJoinability](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[LevelId] [int] NULL,
	[Type] [nvarchar](50) NULL,
	[Value] [varchar](max) NULL,
	[Weightage] [int] NULL,
	CONSTRAINT [FK_TransClonableIRFJoinability_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_TransClonableIRFJoinability_LevelId] FOREIGN KEY([LevelId]) REFERENCES [dbo].[Level] ([id]),
 CONSTRAINT [PK_TransClonableIRFJoinability] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

END
GO


