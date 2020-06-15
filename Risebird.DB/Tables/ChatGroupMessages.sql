/****** Object:  Table [dbo].[ChatGroupMessages]    Script Date: 6/12/2020 4:36:35 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ChatGroupMessages')
BEGIN  

CREATE TABLE [dbo].[ChatGroupMessages](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Groupid] [int] NULL,
	[Userid] [int] NULL,
	[GroupMessage] [varchar](400) NULL,
	[OnDate] [datetime] NULL,
	[Status] [varchar](10) NULL,
	[Senderid] [int] NULL,
 CONSTRAINT [PK_ChatGroupMessages] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO


