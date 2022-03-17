IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransClonableIRFClientUser')
BEGIN

CREATE TABLE [dbo].[TransClonableIRFClientUser](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ClonableIRFId] [int] NOT NULL,
	[ClientUserId] [int] NOT NULL,
	[ClientRoleId] [int] NOT NULL,
	CONSTRAINT [FK_TransClonableIRFClientUser_ClonableIRFId] FOREIGN KEY([ClonableIRFId]) REFERENCES [dbo].[ClonableSkillChannel] ([id]),
	CONSTRAINT [FK_TransClonableIRFClientUser_ClientUserId] FOREIGN KEY([ClientUserId]) REFERENCES [dbo].[UserList] ([id]),
	CONSTRAINT [FK_TransClonableIRFClientUser_ClientRoleId] FOREIGN KEY([ClientRoleId]) REFERENCES [dbo].[ClientRole] ([id]),
 CONSTRAINT [PK_TransClonableIRFClientUser] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO


