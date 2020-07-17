/****** Object:  Table [dbo].[CuratedGroupUsers]    Script Date: 13-07-2020 8.13.54 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CuratedGroupUsers')
BEGIN 

CREATE TABLE [dbo].[CuratedGroupUsers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[CuratedGroupId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL DEFAULT 0,
	FOREIGN KEY([CuratedGroupId]) REFERENCES [dbo].[CuratedGroup] ([id]),
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_CuratedGroupUsers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

end
GO



