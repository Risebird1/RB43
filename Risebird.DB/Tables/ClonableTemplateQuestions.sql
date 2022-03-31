IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClonableTemplateQuestions')

Begin
	CREATE TABLE [dbo].[ClonableTemplateQuestions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[Skill] [int] NULL,
	[Question] [varchar](max) NULL,
	[UserId][int] NULL,
	CONSTRAINT [FK_ClonableTemplateQuestions_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_ClonableTemplateQuestions_UserId] FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
	CONSTRAINT [PK_ClonableTemplateQuestions] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

End