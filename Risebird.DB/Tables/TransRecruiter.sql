/****** Object:  Table [dbo].[TransRecruiter]    Script Date: 10-01-2020 3.01.03 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransRecruiter')
BEGIN
CREATE TABLE [dbo].[TransRecruiter](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[RecruiterId] [int] NOT NULL,
	[UserId] [int] NULL,
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_TransRecruiter] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO



