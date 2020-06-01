/****** Object:  Table [dbo].[VideoCountDetails]    Script Date: 6/1/2020 6:07:59 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VideoCountDetails')
BEGIN 


CREATE TABLE [dbo].[VideoCountDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Videoid] [int] NULL,
	[Userid] [int] NULL,
	[Count] [int] NULL,
	[OnDate] [datetime] NULL,
 CONSTRAINT [PK_VideoCountDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
End
GO


