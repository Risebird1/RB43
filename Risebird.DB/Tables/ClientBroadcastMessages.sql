IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientBroadcastMessages')

Begin
	CREATE TABLE [dbo].[ClientBroadcastMessages](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[Subject] [varchar](500) NULL,
	[Message] [varchar](max) NULL,
	[SentAt][datetime] NULL,
	[SentBy][varchar](50) NULL,
	CONSTRAINT [FK_ClientBroadcastMessages_IRFId] FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	CONSTRAINT [PK_ClientBroadcastMessages] PRIMARY KEY CLUSTERED 
	(
		[id] ASC
	) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End

