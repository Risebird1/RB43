/****** Object:  Table [dbo].[AccountProjectMapping]    Script Date: 04-12-2019 6.40.24 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'AccountProjectMapping')
Begin
CREATE TABLE [dbo].[AccountProjectMapping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL,
	[ProjectId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL default '0',
	FOREIGN KEY([AccountId]) REFERENCES [dbo].[Account] ([id]),
	FOREIGN KEY([ProjectId]) REFERENCES [dbo].[Project] ([id]),
 CONSTRAINT [PK_AccountProjectMapping] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End
GO






