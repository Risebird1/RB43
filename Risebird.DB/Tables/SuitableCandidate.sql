/****** Object:  Table [dbo].[SuitableCandidate]    Script Date: 5/1/2020 11:01:16 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SuitableCandidate')
BEGIN  

CREATE TABLE [dbo].[SuitableCandidate](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[Userid] [int] NULL,
	[Status] [varchar](20) NULL,
	[Comment] [varchar](400) NULL,
	[InterstedOn] [datetime] NULL,
 CONSTRAINT [PK_SuitableCandidate] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


