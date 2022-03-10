IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFEmergencyInterviewer')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFEmergencyInterviewer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[UserId] [int] NULL,
	[PerSuccessRate] [int] NULL,
	[IsActive] [bit] DEFAULT 1,
	[ClientAction] [varchar](12) NULL,
	CONSTRAINT [FK_TransClonableIRFEmergencyInterviewer_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
 CONSTRAINT [PK_TransClonableIRFEmergencyInterviewer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END

GO



