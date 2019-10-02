/****** Object:  Table [dbo].[ProjectIRFMapping]    Script Date: 20-09-2019 11.47.30 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ProjectIRFMapping')
BEGIN 
CREATE TABLE [dbo].[ProjectIRFMapping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[ProjectId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL DEFAULT '0',
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([ProjectId]) REFERENCES [dbo].[Project] ([id]),
 CONSTRAINT [PK_ProjectIRFMapping] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
Go




