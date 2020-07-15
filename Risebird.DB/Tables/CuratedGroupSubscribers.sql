/****** Object:  Table [dbo].[CuratedGroupSubscribers]    Script Date: 15-07-2020 5.59.05 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CuratedGroupSubscribers')
BEGIN 

CREATE TABLE [dbo].[CuratedGroupSubscribers](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[CuratedGroupId] [int] NOT NULL,
	FOREIGN KEY([CuratedGroupId]) REFERENCES [dbo].[CuratedGroup] ([id]),
	FOREIGN KEY([UserId]) REFERENCES [dbo].[UserList] ([id]),
 CONSTRAINT [PK_CuratedGroupSubscribers] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO



