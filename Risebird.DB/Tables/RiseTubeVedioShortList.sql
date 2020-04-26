/****** Object:  Table [dbo].[RiseTubeVedioShortList]    Script Date: 4/26/2020 2:49:09 PM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RiseTubeVedioShortList')
BEGIN 

CREATE TABLE [dbo].[RiseTubeVedioShortList](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[UserId] [int] NULL,
 CONSTRAINT [PK_RiseTubeVedioShortList] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


