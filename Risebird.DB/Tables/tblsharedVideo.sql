/****** Object:  Table [dbo].[tblsharedVideo]    Script Date: 5/22/2020 6:05:45 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'tblsharedVideo')
BEGIN 

CREATE TABLE [dbo].[tblsharedVideo](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Userid] [int] NULL,
	[VideoId] [int] NULL,
	[WhoShare] [varchar](100) NULL,
	[SharedOn] [datetime] NULL,
 CONSTRAINT [PK_tblsharedVideo] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO

