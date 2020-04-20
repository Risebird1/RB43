/****** Object:  Table [dbo].[RisetubeVideoComment]    Script Date: 4/18/2020 11:03:02 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RisetubeVideoComment')

BEGIN

CREATE TABLE [dbo].[RisetubeVideoComment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NULL,
	[interviewId] [int] NULL,
	[Comment] [varchar](max) NULL,
	[Date] [datetime] NULL,
 CONSTRAINT [PK_RisetubeVideoComment] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
