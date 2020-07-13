/****** Object:  Table [dbo].[CuratedIRFMapping]    Script Date: 13-07-2020 8.14.08 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CuratedIRFMapping')
BEGIN 

CREATE TABLE [dbo].[CuratedIRFMapping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[CuratedGroupId] [int] NOT NULL,
	[IRFId] [int] NOT NULL,
	[IsAdded] [bit] NOT NULL DEFAULT 0,
	FOREIGN KEY([IRFId]) REFERENCES [dbo].[IRF] ([id]),
	FOREIGN KEY([CuratedGroupId]) REFERENCES [dbo].[CuratedGroup] ([id]),
 CONSTRAINT [PK_CuratedIRFMapping] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO