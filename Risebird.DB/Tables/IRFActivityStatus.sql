IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'IRFActivityStatus')
BEGIN

CREATE TABLE [dbo].[IRFActivityStatus](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[idProgress] [int] NULL,
	[Userid] [int] NULL,
	[IRFId] [int] NULL,
	[Comment] [varchar](400) NULL,
	[Status] [varchar](50) NULL,
 CONSTRAINT [PK_IRFActivityStatus] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


