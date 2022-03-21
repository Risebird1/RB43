IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFInterviewer')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFInterviewer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] DEFAULT 1,
	[ClientAction] [varchar](12) NULL,
	[IsAvailable] [bit] NULL,
	CONSTRAINT [FK_TransClonableIRFInterviewer_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
 CONSTRAINT [PK_TransClonableIRFInterviewer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO



