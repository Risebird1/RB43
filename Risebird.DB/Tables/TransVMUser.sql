/****** Object:  Table [dbo].[TransVMUser]    Script Date: 25-09-2020 11.49.17 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Candidate')
BEGIN
CREATE TABLE [dbo].[TransVMUser](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[VmId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[ProfileId] [int] NULL,
	FOREIGN KEY([VmId]) REFERENCES [dbo].[VMProperties] ([id]),
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
	FOREIGN KEY([ProfileId]) REFERENCES [dbo].[Profile] ([id]),
 CONSTRAINT [PK_TransVMUser] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO



