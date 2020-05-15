/****** Object:  Table [dbo].[Subscribers]    Script Date: 29-04-2020 6.45.11 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Subscribers')
CREATE TABLE [dbo].[Subscribers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),

 CONSTRAINT [PK_Subscribers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO



