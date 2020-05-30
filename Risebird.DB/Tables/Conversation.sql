/****** Object:  Table [dbo].[Conversation]    Script Date: 5/29/2020 2:51:43 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'Conversation')
BEGIN  
CREATE TABLE [dbo].[Conversation](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Senderid] [varchar](100) NULL,
	[Reciverid] [varchar](100) NULL,
	[Status] [varchar](20) NULL,
	[message] [varchar](200) NULL,
	[Created_at] [datetime] NULL,
	[messageStatus] [varchar](20) NULL,
 CONSTRAINT [PK_Conversation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


