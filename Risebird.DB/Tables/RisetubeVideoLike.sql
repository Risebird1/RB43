/****** Object:  Table [dbo].[RisetubeVideoLike]    Script Date: 4/18/2020 11:03:16 AM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RisetubeVideoLike')

BEGIN
CREATE TABLE [dbo].[RisetubeVideoLike](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[userId] [int] NULL,
	[interviewId] [int] NULL,
	[isLike] [varchar](10) NULL,
 CONSTRAINT [PK_RisetubeVideoLike] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


